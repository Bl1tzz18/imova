using Imova.Contracts.Properties;
using MediatR;

namespace Imova.Application.Features.Properties.GetPropertyById;

public record GetPropertyByIdQuery(Guid Id) : IRequest<PropertyDto?>;
