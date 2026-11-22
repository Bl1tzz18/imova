using Imova.Contracts.Admins;
using MediatR;

namespace Imova.Application.Features.Admins.GetAdminSummary;

public record GetAdminSummaryQuery(bool IsAdmin) : IRequest<AdminSummaryDto>;
