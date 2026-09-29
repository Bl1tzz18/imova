using Imova.Contracts.Account;
using MediatR;

namespace Imova.Application.Features.Account.GetAccountDataSummary;

public record GetAccountDataSummaryQuery(Guid UserId) : IRequest<AccountDataSummaryDto>;
