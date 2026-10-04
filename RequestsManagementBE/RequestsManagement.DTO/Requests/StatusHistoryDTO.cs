using RequestsManagement.DTO.Enums;

namespace RequestsManagement.DTO.Requests
{
    /// <summary>
    /// רשומת היסטוריית שינוי סטטוס (Audit) - מוחזרת בצפייה בהיסטוריה.
    /// </summary>
    public class StatusHistoryDTO
    {
        public long Id { get; set; }
        public long RequestId { get; set; }
        public RequestStatus? PreviousStatus { get; set; }
        public RequestStatus NewStatus { get; set; }
        public DateTime ChangedAt { get; set; }
        public string ChangedBy { get; set; } = string.Empty;
    }
}
