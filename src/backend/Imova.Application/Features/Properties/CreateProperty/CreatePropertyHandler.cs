using Imova.Contracts.Properties;
using Imova.Domain.Properties;
using Imova.Infrastructure;
using MediatR;

namespace Imova.Application.Features.Properties.CreateProperty;

public class CreatePropertyHandler(ImovaDbContext dbContext) : IRequestHandler<CreatePropertyCommand, PropertyDto>
{
    public async Task<PropertyDto> Handle(CreatePropertyCommand request, CancellationToken cancellationToken)
    {
        var property = Property.Create(request.Title, request.Price, request.Currency, request.City, request.District);

        dbContext.Properties.Add(property);
        await dbContext.SaveChangesAsync(cancellationToken);

        return property.ToDto();
    }
}
