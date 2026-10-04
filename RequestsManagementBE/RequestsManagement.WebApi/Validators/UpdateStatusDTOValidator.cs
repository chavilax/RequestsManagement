using FluentValidation;
using RequestsManagement.DTO.Requests;

namespace RequestsManagement.WebApi.Validators
{
    /// <summary>
    /// ולידציה לקלט עדכון סטטוס יחיד. רצה אוטומטית לפני שה-Controller מקבל את הבקשה.
    /// </summary>
    public class UpdateStatusDTOValidator : AbstractValidator<UpdateStatusDTO>
    {
        public UpdateStatusDTOValidator()
        {
            // הסטטוס חייב להיות ערך חוקי מתוך ה-enum
            RuleFor(x => x.NewStatus)
                .IsInEnum()
                .WithMessage("סטטוס לא חוקי.");

            // RowVersion נדרש - בלעדיו אי אפשר לאכוף Concurrency
            RuleFor(x => x.RowVersion)
                .NotEmpty()
                .WithMessage("RowVersion נדרש לעדכון.");

            // אם סופק ChangedBy - אורך סביר
            RuleFor(x => x.ChangedBy)
                .MaximumLength(100)
                .When(x => !string.IsNullOrEmpty(x.ChangedBy));
        }
    }
}
