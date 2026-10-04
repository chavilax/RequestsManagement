namespace RequestsManagement.DTO.Requests
{
    /// <summary>
    /// תוצאת עדכון מרובה (Bulk) בגישת Partial Success.
    /// כל פנייה מטופלת בנפרד - מה שהצליח נשמר, מה שנכשל מדווח עם סיבה.
    /// </summary>
    public class BulkUpdateResultDTO
    {
        public int TotalRequested { get; set; }
        public int SucceededCount { get; set; }
        public int FailedCount { get; set; }

        public List<long> Succeeded { get; set; } = new();
        public List<BulkFailureDTO> Failed { get; set; } = new();
    }

    /// <summary>
    /// פרטי כישלון עדכון של פנייה בודדת ב-Bulk.
    /// </summary>
    public class BulkFailureDTO
    {
        public long RequestId { get; set; }

        /// <summary>
        /// סיבת הכישלון: NotFound / ConcurrencyConflict / InvalidTransition.
        /// </summary>
        public string Reason { get; set; } = string.Empty;
    }
}
