using RequestsManagement.DTO.Enums;

namespace RequestsManagement.BL.Services
{
    /// <summary>
    /// חוקי מעברי סטטוס מותרים. מרוכז במקום אחד כדי שיהיה קל לתחזק ולבדוק.
    /// </summary>
    public static class StatusTransitionRules
    {
        /// <summary>
        /// מפה של סטטוס נוכחי -> הסטטוסים שאליהם מותר לעבור.
        /// New        -> InProgress, Waiting
        /// InProgress -> Waiting, Completed
        /// Waiting    -> InProgress, Completed
        /// Completed  -> (סופי, אין מעברים)
        /// </summary>
        private static readonly Dictionary<RequestStatus, RequestStatus[]> AllowedTransitions = new()
        {
            [RequestStatus.New] = new[] { RequestStatus.InProgress, RequestStatus.Waiting },
            [RequestStatus.InProgress] = new[] { RequestStatus.Waiting, RequestStatus.Completed },
            [RequestStatus.Waiting] = new[] { RequestStatus.InProgress, RequestStatus.Completed },
            [RequestStatus.Completed] = Array.Empty<RequestStatus>()
        };

        /// <summary>
        /// בודק אם מעבר מ-current ל-next מותר.
        /// עדכון לאותו סטטוס (ללא שינוי) נחשב לא חוקי - אין מה לעדכן.
        /// </summary>
        public static bool IsAllowed(RequestStatus current, RequestStatus next)
        {
            return AllowedTransitions.TryGetValue(current, out var allowed)
                   && allowed.Contains(next);
        }
    }
}
