using Imova.Contracts.Properties;
using Imova.Domain.Properties;
using MediatR;

namespace Imova.Application.Features.Properties.GetProperties;

public record GetPropertiesQuery(PropertyType? PropertyType = null, Guid? CurrentUserId = null) : IRequest<List<PropertyDto>>;
