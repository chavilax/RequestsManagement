using RequestsManagement.DTO.Enums;

namespace RequestsManagement.DAL.Entities;

/// <summary>
/// רשומת Audit לשינוי סטטוס של פנייה.
/// נוצרת אוטומטית בכל עדכון סטטוס (יחיד או Bulk).
/// </summary>
public class RequestStatusHistory
{
    public long Id { get; set; }

    /// <summary>
    /// מזהה הפנייה שעבורה נרשם השינוי.
    /// </summary>
    public long RequestId { get; set; }

    /// <summary>
    /// הסטטוס לפני השינוי. null עבור הרשומה הראשונה (יצירה).
    /// </summary>
    public RequestStatus? PreviousStatus { get; set; }

    public RequestStatus NewStatus { get; set; }

    public DateTime ChangedAt { get; set; }

    /// <summary>
    /// מזהה/שם המשתמש שביצע את השינוי.
    /// </summary>
    public string ChangedBy { get; set; } = string.Empty;

    public Request Request { get; set; } = null!;
}
