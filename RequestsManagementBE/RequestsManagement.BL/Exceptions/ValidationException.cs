namespace RequestsManagement.BL.Exceptions
{
    /// <summary>
    /// נזרקת על קלט לא חוקי - למשל מעבר סטטוס אסור. ממופה ל-HTTP 400.
    /// </summary>
    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
    }
}
