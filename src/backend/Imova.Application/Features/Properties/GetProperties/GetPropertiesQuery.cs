using Imova.Contracts.Properties;
using MediatR;

namespace Imova.Application.Features.Properties.GetProperties;

public record GetPropertiesQuery : IRequest<List<PropertyDto>>;
