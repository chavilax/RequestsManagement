using RequestsManagement.DTO.Common;
using RequestsManagement.DTO.Requests;

namespace RequestsManagement.BL.Interfaces
{
    public interface IRequestBL
    {
        /// <summary>
        /// שליפת פניות מסוננות, ממוינות ומדופדפות.
        /// </summary>
        Task<PagedResultDTO<RequestListItemDTO>> GetRequestsAsync(
            RequestFilterDTO filter, CancellationToken cancellationToken);

        /// <summary>
        /// נתונים מסכמים (Aggregations) על הפניות.
        /// </summary>
        Task<RequestStatsDTO> GetStatsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// עדכון סטטוס של פנייה בודדת עם אכיפת Optimistic Concurrency.
        /// זורק NotFoundException / ValidationException / ConcurrencyConflictException.
        /// </summary>
        Task<RequestDTO> UpdateStatusAsync(long id, UpdateStatusDTO dto, CancellationToken cancellationToken);

        /// <summary>
        /// שליפת היסטוריית שינויי הסטטוס של פנייה. זורק NotFoundException אם לא קיימת.
        /// </summary>
        Task<List<StatusHistoryDTO>> GetHistoryAsync(long requestId, CancellationToken cancellationToken);

        /// <summary>
        /// עדכון סטטוס מרובה (Bulk) בגישת Partial Success - עד 100 פניות.
        /// זורק ValidationException אם הקלט חורג מהמגבלות.
        /// </summary>
        Task<BulkUpdateResultDTO> BulkUpdateStatusAsync(BulkUpdateStatusDTO dto, CancellationToken cancellationToken);
    }
}
