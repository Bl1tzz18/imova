using FluentValidation;

namespace Imova.Application.Features.Account.ConfirmAccountDeletion;

public class ConfirmAccountDeletionValidator : AbstractValidator<ConfirmAccountDeletionCommand>
{
    public ConfirmAccountDeletionValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.Token).NotEmpty().MaximumLength(2000);
    }
}
