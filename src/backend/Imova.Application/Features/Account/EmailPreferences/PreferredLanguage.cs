using FluentValidation;
using Imova.Application.Common;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imova.Application.Features.Account.EmailPreferences;

// The account's language (ApplicationUser.PreferredLanguage) follows the site: the web app sends
// it whenever a signed-in user switches language. There's no separate setting for it.
public record UpdatePreferredLanguageCommand(Guid UserId, string Language) : IRequest;

public class UpdatePreferredLanguageValidator : AbstractValidator<UpdatePreferredLanguageCommand>
{
    public UpdatePreferredLanguageValidator()
    {
        RuleFor(c => c.Language)
            .Must(Languages.IsSupported)
            .WithMessage($"Language must be one of: {string.Join(", ", Languages.Supported)}.");
    }
}

public class UpdatePreferredLanguageHandler(IApplicationDbContext dbContext) : IRequestHandler<UpdatePreferredLanguageCommand>
{
    public async Task Handle(UpdatePreferredLanguageCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
                   ?? throw new AuthenticationFailedException("The account no longer exists.");
        if (user.PreferredLanguage == request.Language)
        {
            return;
        }

        // A plain column write, like the profile edits — never through UserManager, so the security
        // stamp (and with it every session) is left alone.
        user.PreferredLanguage = request.Language;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
