using FluentValidation;

namespace Imova.Application.Features.Messaging.StartConversation;

public class StartConversationValidator : AbstractValidator<StartConversationCommand>
{
    public StartConversationValidator()
    {
        RuleFor(c => c.ListingId).NotEmpty();
        MessageBodyRules.Apply(this, c => c.Body, c => c.AttachmentBlobNames);
    }
}
