using RequestsManagement.DTO.Enums;

namespace RequestsManagement.DTO.Requests
{
    /// <summary>
    /// פרמטרי סינון, חיפוש, מיון ודפדוף לשליפת פניות.
    /// כל הפרמטרים אופציונליים.
    /// </summary>
    public class RequestFilterDTO
    {
        // סינון
        public RequestStatus? Status { get; set; }
        public RequestPriority? Priority { get; set; }
        public string? OrganizationName { get; set; }
        public string? AssignedTo { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }

        // חיפוש טקסטואלי (Title + OrganizationName)
        public string? Search { get; set; }

        // מיון
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; }

        // דפדוף
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
