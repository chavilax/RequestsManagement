using RequestsManagement.DAL.Entities;
using RequestsManagement.DTO.Enums;
using RequestsManagement.DTO.Requests;

namespace RequestsManagement.DAL.Repositories
{
    public interface IRequestDAL
    {
        /// <summary>
        /// שליפת פניות מסוננות, ממוינות ומדופדפות. מחזירה את הפריטים לעמוד + הספירה הכוללת.
        /// הסינון, המיון והדפדוף מתבצעים כולם בצד השרת (DB).
        /// </summary>
        Task<(List<Request> Items, int TotalCount)> GetPagedAsync(
            RequestFilterDTO filter, CancellationToken cancellationToken);

        /// <summary>
        /// נתונים מסכמים: ספירה כוללת, ספירה לפי סטטוס וספירה לפי עדיפות.
        /// מחושב ב-DB (GROUP BY) ללא טעינת כל הרשומות.
        /// </summary>
        Task<(int Total, List<(RequestStatus Status, int Count)> ByStatus, List<(RequestPriority Priority, int Count)> ByPriority)>
            GetStatsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// שליפת פנייה בודדת לצורך עדכון (עם tracking). מחזיר null אם לא קיימת.
        /// </summary>
        Task<Request?> GetByIdAsync(long id, CancellationToken cancellationToken);

        /// <summary>
        /// שמירת השינויים. זורק DbUpdateConcurrencyException אם RowVersion לא תואם.
        /// </summary>
        Task SaveChangesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// מגדיר ידנית את ערך ה-RowVersion המקורי של הישות, כך ש-EF ישווה אותו
        /// מול ה-DB בעת העדכון (בסיס ה-Optimistic Concurrency).
        /// </summary>
        void SetOriginalRowVersion(Request request, byte[] rowVersion);

        /// <summary>
        /// הוספת רשומת היסטוריה (Audit) לשינוי סטטוס.
        /// </summary>
        void AddStatusHistory(RequestStatusHistory history);

        /// <summary>
        /// בדיקה אם פנייה קיימת (ללא שליפת כל הישות).
        /// </summary>
        Task<bool> ExistsAsync(long id, CancellationToken cancellationToken);

        /// <summary>
        /// שליפת היסטוריית שינויי הסטטוס של פנייה, ממוינת מהחדש לישן.
        /// </summary>
        Task<List<RequestStatusHistory>> GetHistoryAsync(long requestId, CancellationToken cancellationToken);

        /// <summary>
        /// ניקוי ה-ChangeTracker. נדרש בין פניות ב-Bulk כדי שכישלון של אחת
        /// לא ישאיר ישויות "תקועות" שישפיעו על הפניות הבאות.
        /// </summary>
        void ClearChangeTracker();
    }
}
