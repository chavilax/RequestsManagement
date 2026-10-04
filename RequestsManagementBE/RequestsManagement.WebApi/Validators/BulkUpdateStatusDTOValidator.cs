using FluentValidation;
using RequestsManagement.DTO.Requests;

namespace RequestsManagement.WebApi.Validators
{
    /// <summary>
    /// ולידציה לקלט עדכון מרובה (Bulk).
    /// </summary>
    public class BulkUpdateStatusDTOValidator : AbstractValidator<BulkUpdateStatusDTO>
    {
        private const int MaxBulkSize = 100;

        public BulkUpdateStatusDTOValidator()
        {
            RuleFor(x => x.NewStatus)
                .IsInEnum()
                .WithMessage("סטטוס לא חוקי.");

            // חייבת להיות לפחות פנייה אחת
            RuleFor(x => x.RequestIds)
                .NotEmpty()
                .WithMessage("יש לספק לפחות מזהה פנייה אחד.");

            // לכל היותר 100 (תואם למגבלת ה-BL)
            RuleFor(x => x.RequestIds)
                .Must(ids => ids == null || ids.Count <= MaxBulkSize)
                .WithMessage($"ניתן לעדכן עד {MaxBulkSize} פניות בבקשה אחת.");

            RuleFor(x => x.ChangedBy)
                .MaximumLength(100)
                .When(x => !string.IsNullOrEmpty(x.ChangedBy));
        }
    }
}
