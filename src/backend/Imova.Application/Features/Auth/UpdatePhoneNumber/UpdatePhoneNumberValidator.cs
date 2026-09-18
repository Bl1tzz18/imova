using FluentValidation;
using Imova.Application.Common.Validation;

namespace Imova.Application.Features.Auth.UpdatePhoneNumber;

public class UpdatePhoneNumberValidator : AbstractValidator<UpdatePhoneNumberCommand>
{
    public UpdatePhoneNumberValidator()
    {
        RuleFor(c => c.PhoneNumber).ValidPhoneNumber();
    }
}
