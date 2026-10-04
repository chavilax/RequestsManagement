using RequestsManagement.DTO.Enums;

namespace RequestsManagement.DTO.Requests
{
    /// <summary>
    /// נתונים מסכמים (Aggregations) על הפניות.
    /// </summary>
    public class RequestStatsDTO
    {
        public int TotalCount { get; set; }

        /// <summary>
        /// מספר פניות לפי סטטוס.
        /// </summary>
        public List<StatusCountDTO> CountByStatus { get; set; } = new();

        /// <summary>
        /// מספר פניות לפי עדיפות.
        /// </summary>
        public List<PriorityCountDTO> CountByPriority { get; set; } = new();
    }

    public class StatusCountDTO
    {
        public RequestStatus Status { get; set; }
        public int Count { get; set; }
    }

    public class PriorityCountDTO
    {
        public RequestPriority Priority { get; set; }
        public int Count { get; set; }
    }
}
