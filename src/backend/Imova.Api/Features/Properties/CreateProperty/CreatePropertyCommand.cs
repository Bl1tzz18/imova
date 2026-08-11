using MediatR;

namespace Imova.Api.Features.Properties.CreateProperty;

public record CreatePropertyCommand(
    string Title,
    decimal Price,
    string Currency,
    string City,
    string District) : IRequest<PropertyDto>;
