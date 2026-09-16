using Imova.Contracts.Properties;
using Imova.Domain.Properties;
using MediatR;

namespace Imova.Application.Features.Properties.GetProperties;

public record GetPropertiesQuery(PropertyType? PropertyType = null) : IRequest<List<PropertyDto>>;
