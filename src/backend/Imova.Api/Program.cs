using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json.Serialization;
using FluentValidation;
using Imova.Api.Features.Auth;
using Imova.Api.Features.Media;
using Imova.Api.Features.Properties;
using Imova.Api.Features.Users;
using Imova.Application.Common.Behaviors;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Properties.GetProperties;
using Imova.Infrastructure;
using Imova.Infrastructure.Identity;
using Imova.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddDbContext<ImovaDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Default"),
        npgsqlOptions => npgsqlOptions.UseNetTopologySuite()));

builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ImovaDbContext>());

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<ImovaDbContext>()
    .AddDefaultTokenProviders();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException($"Configuration section \"{JwtOptions.SectionName}\" is missing.");
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

// Absent in appsettings.json/appsettings.Development.json this falls back to an empty ClientId —
// Google sign-in just won't verify until real credentials are configured (see GoogleAuthOptions).
var googleAuthOptions = builder.Configuration.GetSection(GoogleAuthOptions.SectionName).Get<GoogleAuthOptions>()
    ?? new GoogleAuthOptions();
builder.Services.AddSingleton(googleAuthOptions);
builder.Services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keeps the short claim names JwtTokenGenerator writes ("sub", "email", "role", …) as-is
        // instead of the default inbound remapping to long-form ClaimTypes.* URIs.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = "role",
            NameClaimType = JwtRegisteredClaimNames.Email,
        };
    });

builder.Services.AddAuthorization();

var blobStorageOptions = builder.Configuration.GetSection(BlobStorageOptions.SectionName).Get<BlobStorageOptions>()
    ?? throw new InvalidOperationException($"Configuration section \"{BlobStorageOptions.SectionName}\" is missing.");
builder.Services.AddSingleton(blobStorageOptions);
builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();

// Typed HttpClient for downloading external images (currently just Google profile pictures on
// new-account sign-in — see GoogleLoginHandler). A short timeout since this is a synchronous
// part of the sign-in request path and must not hang it.
builder.Services.AddHttpClient<IExternalImageFetcher, ExternalImageFetcher>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<GetPropertiesQuery>();
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

builder.Services.AddValidatorsFromAssemblyContaining<GetPropertiesQuery>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ImovaDbContext>();
    dbContext.Database.Migrate();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    foreach (var role in new[] { Roles.User, Roles.Admin })
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }
    }
}

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.UseExceptionHandler(handler =>
{
    handler.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        if (exception is ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await Results.ValidationProblem(errors).ExecuteAsync(context);
            return;
        }

        if (exception is AuthenticationFailedException authException)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await Results.Problem(authException.Message, statusCode: StatusCodes.Status401Unauthorized).ExecuteAsync(context);
            return;
        }

        if (exception is ForbiddenAccessException forbiddenException)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await Results.Problem(forbiddenException.Message, statusCode: StatusCodes.Status403Forbidden).ExecuteAsync(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    });
});

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPropertiesEndpoints();
app.MapMediaEndpoints();
app.MapAuthEndpoints();
app.MapUserEndpoints();

app.Run();

public partial class Program;
