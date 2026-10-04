using Microsoft.EntityFrameworkCore;
using RequestsManagement.DAL.Context;
using RequestsManagement.DAL.Entities;
using RequestsManagement.DTO.Enums;
using RequestsManagement.DTO.Requests;

namespace RequestsManagement.DAL.Repositories
{
    public class RequestDAL : IRequestDAL
    {
        private readonly AppDbContext _context;

        public RequestDAL(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(List<Request> Items, int TotalCount)> GetPagedAsync(
            RequestFilterDTO filter, CancellationToken cancellationToken)
        {
            // AsNoTracking - שליפה בלבד, אין צורך ב-change tracking (ביצועים)
            IQueryable<Request> query = _context.Requests.AsNoTracking();

            query = ApplyFilters(query, filter);

            // הספירה מתבצעת ב-DB על ה-query המסונן, לפני הדפדוף
            var totalCount = await query.CountAsync(cancellationToken);

            query = ApplySorting(query, filter);

            // דפדוף בצד השרת - רק העמוד המבוקש נמשך מה-DB
            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<(int Total, List<(RequestStatus Status, int Count)> ByStatus, List<(RequestPriority Priority, int Count)> ByPriority)>
            GetStatsAsync(CancellationToken cancellationToken)
        {
            // GROUP BY מתבצע ב-DB - לא נמשכות כל הרשומות לזיכרון
            var byStatusRaw = await _context.Requests.AsNoTracking()
                .GroupBy(r => r.Status)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var byPriorityRaw = await _context.Requests.AsNoTracking()
                .GroupBy(r => r.Priority)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var byStatus = byStatusRaw.Select(x => (x.Key, x.Count)).ToList();
            var byPriority = byPriorityRaw.Select(x => (x.Key, x.Count)).ToList();
            var total = byStatusRaw.Sum(x => x.Count);

            return (total, byStatus, byPriority);
        }

        public async Task<Request?> GetByIdAsync(long id, CancellationToken cancellationToken)
        {
            // ללא AsNoTracking - אנחנו עומדים לעדכן את הישות, אז EF צריך לעקוב אחריה
            return await _context.Requests
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        public void SetOriginalRowVersion(Request request, byte[] rowVersion)
        {
            // מגדירים ל-EF מהי הגרסה שהלקוח "ראה". בעת ה-UPDATE, EF יוסיף תנאי
            // WHERE RowVersion = <הערך הזה>. אם מישהו שינה בינתיים - 0 שורות יושפעו
            // ו-EF יזרוק DbUpdateConcurrencyException.
            _context.Entry(request).Property(r => r.RowVersion).OriginalValue = rowVersion;
        }

        public void AddStatusHistory(RequestStatusHistory history)
        {
            _context.RequestStatusHistory.Add(history);
        }

        public async Task<bool> ExistsAsync(long id, CancellationToken cancellationToken)
        {
            return await _context.Requests
                .AsNoTracking()
                .AnyAsync(r => r.Id == id, cancellationToken);
        }

        public async Task<List<RequestStatusHistory>> GetHistoryAsync(long requestId, CancellationToken cancellationToken)
        {
            // משתמש באינדקס IX_RequestStatusHistory_RequestId_ChangedAt
            return await _context.RequestStatusHistory
                .AsNoTracking()
                .Where(h => h.RequestId == requestId)
                .OrderByDescending(h => h.ChangedAt)
                .ToListAsync(cancellationToken);
        }

        public void ClearChangeTracker()
        {
            _context.ChangeTracker.Clear();
        }

        /// <summary>
        /// הרכבת תנאי הסינון על ה-IQueryable. התנאים מתווספים רק אם הפרמטר סופק,
        /// כך שה-SQL הסופי כולל רק את הסינונים הרלוונטיים.
        /// </summary>
        private static IQueryable<Request> ApplyFilters(IQueryable<Request> query, RequestFilterDTO filter)
        {
            if (filter.Status.HasValue)
                query = query.Where(r => r.Status == filter.Status.Value);

            if (filter.Priority.HasValue)
                query = query.Where(r => r.Priority == filter.Priority.Value);

            if (!string.IsNullOrWhiteSpace(filter.OrganizationName))
                query = query.Where(r => r.OrganizationName.Contains(filter.OrganizationName));

            if (!string.IsNullOrWhiteSpace(filter.AssignedTo))
                query = query.Where(r => r.AssignedTo != null && r.AssignedTo.Contains(filter.AssignedTo));

            if (filter.CreatedFrom.HasValue)
                query = query.Where(r => r.CreatedAt >= filter.CreatedFrom.Value);

            if (filter.CreatedTo.HasValue)
                query = query.Where(r => r.CreatedAt <= filter.CreatedTo.Value);

            // חיפוש טקסטואלי ב-Title וב-OrganizationName
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search;
                query = query.Where(r =>
                    r.Title.Contains(term) || r.OrganizationName.Contains(term));
            }

            return query;
        }

        /// <summary>
        /// מיון דינמי לפי שדה נבחר (ברירת מחדל: CreatedAt). תומך Asc/Desc.
        /// </summary>
        private static IQueryable<Request> ApplySorting(IQueryable<Request> query, RequestFilterDTO filter)
        {
            var desc = filter.SortDescending;

            return filter.SortBy?.ToLowerInvariant() switch
            {
                "title" => desc ? query.OrderByDescending(r => r.Title) : query.OrderBy(r => r.Title),
                "organizationname" => desc ? query.OrderByDescending(r => r.OrganizationName) : query.OrderBy(r => r.OrganizationName),
                "status" => desc ? query.OrderByDescending(r => r.Status) : query.OrderBy(r => r.Status),
                "priority" => desc ? query.OrderByDescending(r => r.Priority) : query.OrderBy(r => r.Priority),
                "updatedat" => desc ? query.OrderByDescending(r => r.UpdatedAt) : query.OrderBy(r => r.UpdatedAt),
                "createdat" => desc ? query.OrderByDescending(r => r.CreatedAt) : query.OrderBy(r => r.CreatedAt),
                // ברירת מחדל - מיון יציב לפי Id כדי לשמור על עקביות בדפדוף
                _ => desc ? query.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
                          : query.OrderByDescending(r => r.CreatedAt).ThenBy(r => r.Id)
            };
        }
    }
}
