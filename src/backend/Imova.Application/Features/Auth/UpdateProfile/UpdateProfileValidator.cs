using FluentValidation;
using Imova.Application.Common.Validation;

namespace Imova.Application.Features.Auth.UpdateProfile;

public class UpdateProfileValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileValidator()
    {
        RuleFor(c => c.DisplayName).MaximumLength(200);
        RuleFor(c => c.PhoneNumber).ValidPhoneNumber();
    }
}
