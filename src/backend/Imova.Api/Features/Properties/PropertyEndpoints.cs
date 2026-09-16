using Imova.Api.Features.Properties.CreateProperty;
using Imova.Api.Features.Properties.GetProperties;
using Imova.Api.Features.Properties.GetPropertyById;

namespace Imova.Api.Features.Properties;

public static class PropertyEndpoints
{
    public static void MapPropertiesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGetProperties();
        app.MapGetPropertyById();
        app.MapCreateProperty();
    }
}
