using FluentValidation;

namespace Imova.Application.Features.Account.DeleteAccount;

public class DeleteAccountValidator : AbstractValidator<DeleteAccountCommand>
{
    public DeleteAccountValidator()
    {
        RuleFor(c => c.Password).MaximumLength(200);
    }
}
