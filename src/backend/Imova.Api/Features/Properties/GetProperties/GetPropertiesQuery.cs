using MediatR;

namespace Imova.Api.Features.Properties.GetProperties;

public record GetPropertiesQuery : IRequest<List<PropertyDto>>;
