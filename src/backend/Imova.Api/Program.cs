using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using FluentValidation;
using Imova.Api.Common;
using Imova.Api.Features.Account;
using Imova.Api.Features.Admins;
using Imova.Api.Features.Amenities;
using Imova.Api.Features.Auth;
using Imova.Api.Features.Favorites;
using Imova.Api.Features.ListingReports;
using Imova.Api.Features.Listings;
using Imova.Api.Features.Locations;
using Imova.Api.Features.Media;
using Imova.Api.Features.Messaging;
using Imova.Api.Features.Proximities;
using Imova.Api.Features.ExchangeRates;
using Imova.Api.Features.Publishers;
using Imova.Api.Features.SavedSearches;
using Imova.Api.Features.Users;
using Imova.Application.Features.Listings;
using Imova.Application.Common;
using Imova.Application.Common.Behaviors;
using Imova.Application.Common.Exceptions;
using Imova.Application.Common.Identity;
using Imova.Application.Common.Interfaces;
using Imova.Application.Common.Validation;
using Imova.Application.Features.Account;
using Imova.Application.Features.Admins;
using Imova.Application.Features.Auth;
using Imova.Application.Features.Auth.Sessions;
using Imova.Application.Features.Listings.GetListings;
using Imova.Application.Features.Listings.SearchListings;
using Imova.Application.Features.Listings.GetSimilarListings;
using Imova.Application.Features.Listings.Visitors;
using Imova.Application.Features.Media.Sizes;
using Imova.Application.Features.Messaging;
using Imova.Application.Features.SavedSearches;
using Imova.Infrastructure;
using Imova.Infrastructure.Email;
using Imova.Infrastructure.Geocoding;
using Imova.Infrastructure.Identity;
using Imova.Infrastructure.Listings;
using Imova.Infrastructure.Locations;
using Imova.Infrastructure.Pricing;
using Imova.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Origins whose browser JS may call the API directly (photo uploads, location typeahead, the
// messaging hub) — the web app's own origin(s). Most calls go through its server instead and
// don't need CORS. No credentials: the browser never sends cookies to the API (the hub takes
// its token as a query parameter).
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() is { Length: > 0 } configured
    ? configured
    : ["http://localhost:3000"];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(corsOrigins)
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
        // Same policy as PasswordRules (the validators) and the web app's live checklist: 8+
        // characters, a number, a special character — nothing else.
        options.Password.RequiredLength = PasswordRules.MinLength;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;

        // See LoginHandler: 5 wrong passwords in a row lock sign-in for 15 minutes.
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<ImovaDbContext>()
    .AddDefaultTokenProviders();

// Password-reset and email-confirmation links (AccountEmails) are data-protection tokens: the
// keys live in Postgres so links survive a redeploy/container restart (and would work across
// several API instances) — with the default per-container key ring every restart voided them.
builder.Services.AddDataProtection()
    .SetApplicationName("Imova.Api")
    .PersistKeysToDbContext<ImovaDbContext>();
builder.Services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = AccountEmails.LinkLifetime);
builder.Services.AddSingleton(
    builder.Configuration.GetSection(AppOptions.SectionName).Get<AppOptions>() ?? new AppOptions());
builder.Services.AddScoped<AccountEmails>();
builder.Services.AddScoped<AccountDeletion>();
builder.Services.AddScoped<AccountDeletionEmails>();
builder.Services.AddScoped<AdminEmails>();
builder.Services.AddSingleton(
    builder.Configuration.GetSection(AuthSessionOptions.SectionName).Get<AuthSessionOptions>() ?? new AuthSessionOptions());
builder.Services.AddScoped<AuthSessions>();
builder.Services.AddScoped<SavedSearchUnsubscribeTokens>();
builder.Services.AddSingleton<AuthEmailThrottle>();
builder.Services.AddAuthRateLimiting(builder.Configuration);

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException($"Configuration section \"{JwtOptions.SectionName}\" is missing.");
// Without these the API would still start, sign tokens with no issuer/audience and then refuse
// every one of them (401 on every signed-in call) — fail at startup instead. Issuer/Audience are
// in appsettings.json; the key is a secret (user secrets locally, Jwt__Key elsewhere).
foreach (var (name, value) in new[] { ("Key", jwtOptions.Key), ("Issuer", jwtOptions.Issuer), ("Audience", jwtOptions.Audience) })
{
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException($"Configuration value \"{JwtOptions.SectionName}:{name}\" is missing.");
    }
}
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

        // A signature-valid, unexpired token is still refused once the account's security stamp
        // has moved on (password changed or reset, "sign out other sessions") — see SessionStamp.
        // One primary-key lookup per authenticated request.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var principal = context.Principal!;
                var current = Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId)
                    && await SessionStamp.IsCurrentAsync(
                        context.HttpContext.RequestServices.GetRequiredService<IApplicationDbContext>(),
                        userId,
                        principal.FindFirstValue(SessionStamp.ClaimType),
                        context.HttpContext.RequestAborted);
                if (!current)
                {
                    context.Fail("This session has been signed out.");
                }
            },
        };
    })
    // Only for the SignalR hub: short-lived tokens with their own audience (see
    // GenerateRealtimeToken). Browsers can't set headers on a WebSocket, so the token arrives as
    // the access_token query parameter.
    .AddJwtBearer(RealtimeAuth.Scheme, options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.RealtimeAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments(MessagingHub.Path))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization();

// Messaging: realtime push over SignalR, presence, email notifications.
builder.Services.AddSignalR();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<PresenceTracker>();
builder.Services.AddSingleton<IPresenceTracker>(sp => sp.GetRequiredService<PresenceTracker>());
builder.Services.AddSingleton<IRealtimeNotifier, RealtimeNotifier>();
builder.Services.AddSingleton(
    builder.Configuration.GetSection(MessagingOptions.SectionName).Get<MessagingOptions>() ?? new MessagingOptions());
builder.Services.AddSingleton(
    builder.Configuration.GetSection(ListingReportOptions.SectionName).Get<ListingReportOptions>() ?? new ListingReportOptions());
builder.Services.AddScoped<MessageDelivery>();
builder.Services.AddScoped<IListingSearch, ListingSearch>();
builder.Services.AddScoped<ISimilarListingsFinder, SimilarListingsFinder>();
builder.Services.AddScoped<IListingCounters, ListingCounters>();
var emailOptions = builder.Configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>() ?? new EmailOptions();
builder.Services.AddSingleton(emailOptions);
if (string.IsNullOrWhiteSpace(emailOptions.Host))
{
    builder.Services.AddSingleton<IEmailSender, LoggingEmailSender>();
}
else
{
    builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
}

var blobStorageOptions = builder.Configuration.GetSection(BlobStorageOptions.SectionName).Get<BlobStorageOptions>()
    ?? throw new InvalidOperationException($"Configuration section \"{BlobStorageOptions.SectionName}\" is missing.");
builder.Services.AddSingleton(blobStorageOptions);
builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();

// Display sizes of listing photos, made when an upload is confirmed (see PhotoSizes).
builder.Services.AddSingleton<IPhotoResizer, MagickPhotoResizer>();
builder.Services.AddScoped<PhotoSizeGenerator>();

// Typed HttpClient for downloading external images (currently just Google profile pictures on
// new-account sign-in — see GoogleLoginHandler). A short timeout since this is a synchronous
// part of the sign-in request path and must not hang it.
builder.Services.AddHttpClient<IExternalImageFetcher, ExternalImageFetcher>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Absent in configuration this falls back to GeocodingOptions' own defaults — geocoding degrades
// gracefully (see IGeocodingService), so a missing "Geocoding" section shouldn't crash startup the
// way a missing "Storage" section does.
var geocodingOptions = builder.Configuration.GetSection(GeocodingOptions.SectionName).Get<GeocodingOptions>()
    ?? new GeocodingOptions();
builder.Services.AddSingleton(geocodingOptions);
builder.Services.AddHttpClient<IGeocodingService, NominatimGeocodingService>((sp, client) =>
{
    var options = sp.GetRequiredService<GeocodingOptions>();
    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Same missing-section-degrades-gracefully treatment as Geocoding above — street suggestions are
// a best-effort typeahead aid, not something startup should fail without.
var photonOptions = builder.Configuration.GetSection(PhotonOptions.SectionName).Get<PhotonOptions>()
    ?? new PhotonOptions();
builder.Services.AddSingleton(photonOptions);
builder.Services.AddHttpClient<IStreetSuggestionService, PhotonStreetSuggestionService>((sp, client) =>
{
    var options = sp.GetRequiredService<PhotonOptions>();
    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
    client.Timeout = TimeSpan.FromSeconds(5);
});

// Backs the in-process cache for Raioane/Localitati/ChisinauSectors (see GetRaioaneHandler etc.)
// — static reference data seeded once at startup, so caching it indefinitely (no expiration,
// cleared only on restart) avoids re-querying Postgres for data that can't change without a
// reseed, which already implies a restart.
builder.Services.AddMemoryCache();

// Fixed, configurable EUR rates for Price.PriceEur (see ExchangeRateOptions) — defaults apply when
// the "ExchangeRates" section is absent.
var exchangeRateOptions = builder.Configuration.GetSection(ExchangeRateOptions.SectionName).Get<ExchangeRateOptions>()
    ?? new ExchangeRateOptions();
builder.Services.AddSingleton(exchangeRateOptions);
builder.Services.AddSingleton<IExchangeRateProvider, ConfiguredExchangeRateProvider>();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<GetListingsQuery>();
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

builder.Services.AddValidatorsFromAssemblyContaining<GetListingsQuery>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ImovaDbContext>();
    dbContext.Database.Migrate();

    await CuatmLocationSeeder.SeedAsync(dbContext, CancellationToken.None);
    await ChisinauSectorSeeder.SeedAsync(dbContext, CancellationToken.None);

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    foreach (var role in new[] { Roles.User, Roles.Admin })
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }
    }
}

// First, so everything after (the auth rate limiter in particular) sees the real client IP — see AuthRateLimiting.
app.UseForwardedHeaders();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Every error response carries a language-neutral code next to its English text (ProblemCodes /
// ErrorCodes) — the web app shows the translated version of the code.
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
            await Results.ValidationProblem(errors, extensions: ProblemCodes.ForValidation(validationException.Errors))
                .ExecuteAsync(context);
            return;
        }

        var (status, detail, code, parameters) = exception switch
        {
            AuthenticationFailedException e => (StatusCodes.Status401Unauthorized, e.Message, e.Code, null),
            ForbiddenAccessException e => (StatusCodes.Status403Forbidden, e.Message, e.Code, null),
            TooManyRequestsException e => (StatusCodes.Status429TooManyRequests, e.Message, e.Code, e.Params),
            // A query string that doesn't bind (e.g. an unknown enum value) is the caller's mistake:
            // 400, not the 500 an unhandled exception would otherwise become.
            BadHttpRequestException e => (e.StatusCode, e.Message, "badRequest", null),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", "unexpected",
                (IReadOnlyDictionary<string, object>?)null),
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("UnhandledException")
                .LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
        }

        context.Response.StatusCode = status;
        await Results.Problem(detail, statusCode: status, extensions: ProblemCodes.For(code, parameters)).ExecuteAsync(context);
    });
});

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapListingEndpoints();
app.MapListingReportEndpoints();
app.MapPublisherEndpoints();
app.MapAmenityEndpoints();
app.MapProximityEndpoints();
app.MapExchangeRateEndpoints();
app.MapMediaEndpoints();
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapAccountEndpoints();
app.MapAdminEndpoints();
app.MapFavoriteEndpoints();
app.MapLocationsEndpoints();
app.MapMessagingEndpoints();
app.MapSavedSearchEndpoints();
app.MapHub<MessagingHub>(MessagingHub.Path);

app.Run();

public partial class Program;
