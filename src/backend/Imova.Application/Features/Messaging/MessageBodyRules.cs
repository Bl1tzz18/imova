using FluentValidation;
using Imova.Domain.Messaging;

namespace Imova.Application.Features.Messaging;

// Shared by StartConversation and SendMessage: text up to 2000 characters and/or up to 5 images,
// never neither.
public static class MessageBodyRules
{
    public static void Apply<T>(AbstractValidator<T> validator, Func<T, string?> body, Func<T, IReadOnlyList<string>?> attachments)
    {
        validator.RuleFor(c => body(c))
            .Must(b => b is null || b.Trim().Length <= Message.MaxBodyLength)
            .WithMessage($"A message can be at most {Message.MaxBodyLength} characters.")
            .OverridePropertyName("Body");
        validator.RuleFor(c => c)
            .Must(c => !string.IsNullOrWhiteSpace(body(c)) || (attachments(c)?.Count ?? 0) > 0)
            .WithMessage("Write a message or attach at least one image.")
            .OverridePropertyName("Body");
        validator.RuleFor(c => attachments(c))
            .Must(a => a is null || a.Count <= Message.MaxAttachments)
            .WithMessage($"A message can have at most {Message.MaxAttachments} images.")
            .OverridePropertyName("AttachmentBlobNames");
    }
}
