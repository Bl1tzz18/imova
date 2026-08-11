var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("Frontend");

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/v1/properties", () =>
{
    var properties = new[]
    {
        new
        {
            Id = Guid.NewGuid(),
            Title = "Apartament cu 2 camere, Botanica",
            Price = 550,
            Currency = "EUR",
            City = "Chisinau",
            District = "Botanica"
        },
        new
        {
            Id = Guid.NewGuid(),
            Title = "Casa cu curte, Durlesti",
            Price = 89000,
            Currency = "EUR",
            City = "Chisinau",
            District = "Durlesti"
        }
    };

    return Results.Ok(properties);
});

app.Run();
