using Imova.Contracts.Properties;
using MediatR;

namespace Imova.Application.Features.Properties.CreateProperty;

public record CreatePropertyCommand(
    string Title,
    decimal Price,
    string Currency,
    string City,
    string District) : IRequest<PropertyDto>;
