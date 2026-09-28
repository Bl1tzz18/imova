using FluentValidation;
using Imova.Application.Common;
using Imova.Domain.Messaging;

namespace Imova.Application.Features.Messaging.ReportConversation;

public class ReportConversationValidator : AbstractValidator<ReportConversationCommand>
{
    public ReportConversationValidator()
    {
        RuleFor(c => c.Reason).IsInEnum();
        RuleFor(c => c.Details).MaximumLength(ConversationReport.MaxDetailsLength);
        RuleFor(c => c.Details)
            .NotEmpty().WithMessage("Describe the problem when the reason is Other.").WithErrorCode(ErrorCodes.ReportDetailsRequired)
            .When(c => c.Reason == ReportReason.Other);
    }
}
