using RequestsManagement.DTO.Enums;

namespace RequestsManagement.DAL.Entities;

/// <summary>
/// ישות פנייה - הטבלה הראשית במערכת.
/// </summary>
public class Request
{
    public long Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string OrganizationName { get; set; } = string.Empty;

    public RequestStatus Status { get; set; }

    public RequestPriority Priority { get; set; }

    /// <summary>
    /// מזהה/שם המטפל בפנייה. יכול להיות ריק כאשר הפנייה טרם שויכה.
    /// </summary>
    public string? AssignedTo { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Concurrency token. SQL Server מעדכן את השדה הזה אוטומטית בכל UPDATE,
    /// וכך EF Core מזהה עדכונים מתחרים (Optimistic Concurrency).
    /// </summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// היסטוריית שינויי הסטטוס של הפנייה (Audit).
    /// </summary>
    public ICollection<RequestStatusHistory> StatusHistory { get; set; } = new List<RequestStatusHistory>();
}
