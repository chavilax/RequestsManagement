using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequestsManagement.BL.Exceptions;
using RequestsManagement.BL.Interfaces;
using RequestsManagement.BL.Mapping;
using RequestsManagement.DAL.Entities;
using RequestsManagement.DAL.Repositories;
using RequestsManagement.DTO.Common;
using RequestsManagement.DTO.Requests;

namespace RequestsManagement.BL.Services
{
    public class RequestBL : IRequestBL
    {
        private readonly IRequestDAL _requestDAL;
        private readonly IStatsCache _statsCache;
        private readonly ILogger<RequestBL> _logger;

        // הגבלות דפדוף - מונעות משיכת כמות גדולה מדי בבקשה אחת
        public const int MaxPageSize = 100;
        public const int DefaultPageSize = 20;

        // מגבלת כמות פניות בעדכון Bulk יחיד
        public const int MaxBulkSize = 100;

        public RequestBL(IRequestDAL requestDAL, IStatsCache statsCache, ILogger<RequestBL> logger)
        {
            _requestDAL = requestDAL;
            _statsCache = statsCache;
            _logger = logger;
        }

        public async Task<PagedResultDTO<RequestListItemDTO>> GetRequestsAsync(
            RequestFilterDTO filter, CancellationToken cancellationToken)
        {
            NormalizePaging(filter);

            var (items, totalCount) = await _requestDAL.GetPagedAsync(filter, cancellationToken);

            return new PagedResultDTO<RequestListItemDTO>
            {
                Items = items.Select(RequestMapper.ToListItemDTO).ToList(),
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<RequestStatsDTO> GetStatsAsync(CancellationToken cancellationToken)
        {
            // 1. ניסיון מה-cache. אם קיים - חוסכים את ה-GROUP BY על 100K שורות.
            var cached = _statsCache.Get();
            if (cached is not null)
                return cached;

            // 2. Cache miss - שליפה מה-DB
            var (total, byStatus, byPriority) = await _requestDAL.GetStatsAsync(cancellationToken);

            var stats = new RequestStatsDTO
            {
                TotalCount = total,
                CountByStatus = byStatus
                    .Select(x => new StatusCountDTO { Status = x.Status, Count = x.Count })
                    .OrderBy(x => x.Status)
                    .ToList(),
                CountByPriority = byPriority
                    .Select(x => new PriorityCountDTO { Priority = x.Priority, Count = x.Count })
                    .OrderBy(x => x.Priority)
                    .ToList()
            };

            // 3. שמירה ב-cache לבקשות הבאות
            _statsCache.Set(stats);
            return stats;
        }

        public async Task<RequestDTO> UpdateStatusAsync(long id, UpdateStatusDTO dto, CancellationToken cancellationToken)
        {
            // 1. שליפת הפנייה. לא קיימת -> 404
            var request = await _requestDAL.GetByIdAsync(id, cancellationToken);
            if (request is null)
                throw new NotFoundException($"פנייה עם מזהה {id} לא נמצאה.");

            // 2. ולידציה של מעבר הסטטוס. מעבר אסור -> 400
            if (!StatusTransitionRules.IsAllowed(request.Status, dto.NewStatus))
                throw new ValidationException(
                    $"מעבר סטטוס מ-{request.Status} ל-{dto.NewStatus} אינו חוקי.");

            var previousStatus = request.Status;

            // 3. עדכון השדות
            request.Status = dto.NewStatus;
            request.UpdatedAt = DateTime.UtcNow;

            // 4. הגדרת ה-RowVersion שהלקוח ראה - בסיס ה-Optimistic Concurrency.
            //    אם מישהו שינה בינתיים, השמירה תיכשל ב-409.
            _requestDAL.SetOriginalRowVersion(request, RequestMapper.DecodeRowVersion(dto.RowVersion));

            // 5. רישום Audit לשינוי הסטטוס
            _requestDAL.AddStatusHistory(new RequestStatusHistory
            {
                RequestId = request.Id,
                PreviousStatus = previousStatus,
                NewStatus = dto.NewStatus,
                ChangedAt = request.UpdatedAt,
                ChangedBy = string.IsNullOrWhiteSpace(dto.ChangedBy) ? "System" : dto.ChangedBy
            });

            // 6. שמירה. עדכון מתחרה -> DbUpdateConcurrencyException -> 409
            try
            {
                await _requestDAL.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogWarning("Concurrency conflict updating request {RequestId}", id);
                throw new ConcurrencyConflictException(
                    $"הפנייה {id} עודכנה על ידי משתמש אחר. יש לרענן ולנסות שוב.");
            }

            // Invalidation - הספירות השתנו, ה-cache כבר לא תקף
            _statsCache.Invalidate();

            _logger.LogInformation("Request {RequestId} status updated: {From} -> {To}",
                id, previousStatus, dto.NewStatus);

            return RequestMapper.ToDTO(request);
        }

        public async Task<List<StatusHistoryDTO>> GetHistoryAsync(long requestId, CancellationToken cancellationToken)
        {
            // אימות שהפנייה קיימת - אחרת מחזירים 404 ולא רשימה ריקה מטעה
            var exists = await _requestDAL.ExistsAsync(requestId, cancellationToken);
            if (!exists)
                throw new NotFoundException($"פנייה עם מזהה {requestId} לא נמצאה.");

            var history = await _requestDAL.GetHistoryAsync(requestId, cancellationToken);
            return history.Select(RequestMapper.ToHistoryDTO).ToList();
        }

        public async Task<BulkUpdateResultDTO> BulkUpdateStatusAsync(
            BulkUpdateStatusDTO dto, CancellationToken cancellationToken)
        {
            // --- Validation של הקלט (לפני כל טיפול) ---
            if (dto.RequestIds is null || dto.RequestIds.Count == 0)
                throw new ValidationException("יש לספק לפחות מזהה פנייה אחד.");

            if (dto.RequestIds.Count > MaxBulkSize)
                throw new ValidationException($"ניתן לעדכן עד {MaxBulkSize} פניות בבקשה אחת. התקבלו {dto.RequestIds.Count}.");

            var changedBy = string.IsNullOrWhiteSpace(dto.ChangedBy) ? "System" : dto.ChangedBy;

            var result = new BulkUpdateResultDTO
            {
                TotalRequested = dto.RequestIds.Count
            };

            // --- Partial Success: כל פנייה מטופלת ונשמרת בנפרד ---
            // כך כישלון של פנייה אחת (NotFound/InvalidTransition/Conflict)
            // אינו משפיע על עדכון הפניות האחרות.
            foreach (var id in dto.RequestIds.Distinct())
            {
                try
                {
                    await UpdateSingleInBulkAsync(id, dto.NewStatus, changedBy, cancellationToken);
                    result.Succeeded.Add(id);
                }
                catch (NotFoundException)
                {
                    result.Failed.Add(new BulkFailureDTO { RequestId = id, Reason = "NotFound" });
                }
                catch (ValidationException)
                {
                    result.Failed.Add(new BulkFailureDTO { RequestId = id, Reason = "InvalidTransition" });
                }
                catch (ConcurrencyConflictException)
                {
                    result.Failed.Add(new BulkFailureDTO { RequestId = id, Reason = "ConcurrencyConflict" });
                }
                finally
                {
                    // ניקוי ה-tracker בין פנייה לפנייה - מבטיח שכישלון של אחת
                    // לא ישאיר ישות "תקועה" שתשפיע על הבאות.
                    _requestDAL.ClearChangeTracker();
                }
            }

            result.SucceededCount = result.Succeeded.Count;
            result.FailedCount = result.Failed.Count;

            // Invalidation - רק אם משהו באמת השתנה (אחרת הספירות זהות)
            if (result.SucceededCount > 0)
                _statsCache.Invalidate();

            _logger.LogInformation(
                "Bulk status update to {Status}: {Succeeded} succeeded, {Failed} failed (of {Total})",
                dto.NewStatus, result.SucceededCount, result.FailedCount, result.TotalRequested);

            return result;
        }

        /// <summary>
        /// עדכון פנייה בודדת בהקשר של Bulk. בניגוד לעדכון היחיד, כאן לא נדרש RowVersion
        /// מהלקוח - משתמשים בגרסה הנוכחית של ה-DB (עדכון יזום של מנהל על קבוצת פניות).
        /// עדיין נאכף Concurrency ברמת ה-DB: אם מתבצע עדכון במקביל, ה-SaveChanges ייכשל.
        /// </summary>
        private async Task UpdateSingleInBulkAsync(
            long id, DTO.Enums.RequestStatus newStatus, string changedBy, CancellationToken cancellationToken)
        {
            var request = await _requestDAL.GetByIdAsync(id, cancellationToken);
            if (request is null)
                throw new NotFoundException($"פנייה {id} לא נמצאה.");

            if (!StatusTransitionRules.IsAllowed(request.Status, newStatus))
                throw new ValidationException($"מעבר מ-{request.Status} ל-{newStatus} אינו מותר.");

            var previousStatus = request.Status;
            request.Status = newStatus;
            request.UpdatedAt = DateTime.UtcNow;

            _requestDAL.AddStatusHistory(new RequestStatusHistory
            {
                RequestId = request.Id,
                PreviousStatus = previousStatus,
                NewStatus = newStatus,
                ChangedAt = request.UpdatedAt,
                ChangedBy = changedBy
            });

            try
            {
                await _requestDAL.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyConflictException($"הפנייה {id} עודכנה על ידי משתמש אחר.");
            }
        }

        /// <summary>
        /// אכיפת גבולות דפדוף: Page לפחות 1, PageSize בטווח חוקי.
        /// </summary>
        private static void NormalizePaging(RequestFilterDTO filter)
        {
            if (filter.Page < 1)
                filter.Page = 1;

            if (filter.PageSize < 1)
                filter.PageSize = DefaultPageSize;

            if (filter.PageSize > MaxPageSize)
                filter.PageSize = MaxPageSize;
        }
    }
}
