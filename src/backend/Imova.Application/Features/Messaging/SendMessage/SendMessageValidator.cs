using FluentValidation;

namespace Imova.Application.Features.Messaging.SendMessage;

public class SendMessageValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageValidator()
    {
        MessageBodyRules.Apply(this, c => c.Body, c => c.AttachmentBlobNames);
    }
}
