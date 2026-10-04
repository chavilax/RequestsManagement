using RequestsManagement.DTO.Requests;

namespace RequestsManagement.BL.Interfaces
{
    /// <summary>
    /// Cache עבור הנתונים המסכמים (Aggregations).
    /// מופשט מאחורי interface כדי שניתן יהיה להחליף את המימוש (IMemoryCache -> Redis)
    /// ללא שינוי בשכבת ה-BL.
    /// </summary>
    public interface IStatsCache
    {
        /// <summary>
        /// מחזיר את ה-stats מה-cache אם קיים, אחרת null.
        /// </summary>
        RequestStatsDTO? Get();

        /// <summary>
        /// שומר את ה-stats ב-cache עם זמן תפוגה.
        /// </summary>
        void Set(RequestStatsDTO stats);

        /// <summary>
        /// ביטול ה-cache (Invalidation) - נקרא בכל שינוי סטטוס.
        /// </summary>
        void Invalidate();
    }
}
