using RequestsManagement.DTO.Enums;

namespace RequestsManagement.DTO.Requests
{
    /// <summary>
    /// קלט לעדכון סטטוס מרובה (Bulk) - עד 100 פניות בבקשה אחת.
    /// </summary>
    public class BulkUpdateStatusDTO
    {
        /// <summary>
        /// מזהי הפניות לעדכון (עד 100).
        /// </summary>
        public List<long> RequestIds { get; set; } = new();

        public RequestStatus NewStatus { get; set; }

        public string? ChangedBy { get; set; }
    }
}
