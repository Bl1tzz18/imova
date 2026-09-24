using FluentValidation;
using Imova.Application.Common.Validation;

namespace Imova.Application.Features.Publishers.CreateAgencyPublisher;

public class CreateAgencyPublisherValidator : AbstractValidator<CreateAgencyPublisherCommand>
{
    public CreateAgencyPublisherValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Phone).Cascade(CascadeMode.Stop).ValidPhoneNumber();
        RuleFor(c => c.Email).EmailAddress().MaximumLength(256);
        RuleFor(c => c.LogoUrl)
            .MaximumLength(500)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
            .WithMessage("LogoUrl must be an absolute http(s) URL.")
            .When(c => !string.IsNullOrWhiteSpace(c.LogoUrl));
        RuleFor(c => c.Bio).MaximumLength(2000);
    }
}
