using RequestsManagement.DTO.Enums;

namespace RequestsManagement.DTO.Requests
{
    /// <summary>
    /// קלט לעדכון סטטוס של פנייה בודדת.
    /// RowVersion נדרש לאכיפת Optimistic Concurrency.
    /// </summary>
    public class UpdateStatusDTO
    {
        public RequestStatus NewStatus { get; set; }

        /// <summary>
        /// ה-RowVersion שהתקבל בשליפה (Base64). משמש לזיהוי עדכון מתחרה.
        /// </summary>
        public string RowVersion { get; set; } = string.Empty;

        /// <summary>
        /// מזהה/שם המשתמש המבצע את העדכון (נרשם ב-Audit).
        /// </summary>
        public string? ChangedBy { get; set; }
    }
}
