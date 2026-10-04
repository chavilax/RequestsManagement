namespace RequestsManagement.BL.Exceptions
{
    /// <summary>
    /// נזרקת כאשר הפנייה שונתה על ידי משתמש אחר מאז שנקראה (RowVersion לא תואם).
    /// ממופה ל-HTTP 409 Conflict.
    /// </summary>
    public class ConcurrencyConflictException : Exception
    {
        public ConcurrencyConflictException(string message) : base(message) { }
    }
}
