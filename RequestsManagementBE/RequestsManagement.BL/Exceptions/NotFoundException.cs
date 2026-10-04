namespace RequestsManagement.BL.Exceptions
{
    /// <summary>
    /// נזרקת כאשר הפנייה המבוקשת אינה קיימת. ממופה ל-HTTP 404.
    /// </summary>
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }
}
