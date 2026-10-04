using RequestsManagement.DTO.Enums;

namespace RequestsManagement.DTO.Requests
{
    /// <summary>
    /// שורת פנייה ברשימה - מוחזרת בשליפה עם Pagination.
    /// </summary>
    public class RequestListItemDTO
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public RequestStatus Status { get; set; }
        public RequestPriority Priority { get; set; }
        public string? AssignedTo { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Concurrency token מקודד ב-Base64 - מאפשר עדכון סטטוס ישירות מהרשימה.
        /// </summary>
        public string RowVersion { get; set; } = string.Empty;
    }
}
