namespace RequestsManagement.DTO.Common
{
    /// <summary>
    /// תוצאת שליפה מדופדפת - הנתונים לעמוד הנוכחי + metadata.
    /// </summary>
    public class PagedResultDTO<T>
    {
        public IReadOnlyList<T> Items { get; set; } = new List<T>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;
    }
}
