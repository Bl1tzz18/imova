using Imova.Application.Features.Listings.Visitors;
using Imova.Application.Common;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Agencies.Import;
using Imova.Application.Features.Auth.Sessions;
using Imova.Application.Features.Favorites.Alerts;
using Imova.Application.Features.Listings.Expiry;
using Imova.Application.Features.Listings.SearchListings;
using Imova.Application.Features.Media.Cleanup;
using Imova.Application.Features.Media.Sizes;
using Imova.Application.Features.SavedSearches;
using Imova.Application.Features.SavedSearches.Alerts;
using Imova.Infrastructure;
using Imova.Infrastructure.Email;
using Imova.Infrastructure.Geocoding;
using Imova.Infrastructure.Imports;
using Imova.Infrastructure.Pricing;
using Imova.Infrastructure.Listings;
using Imova.Infrastructure.Storage;
using Imova.Worker;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

// Background jobs, separate from the API: saved-search alert emails, saved-listing (favorite) alert emails, listing expiry (with the
// reminder emails), the display sizes of older photos (PhotoSizeBackfill), and the cleanup of photos from abandoned add-listing forms and of ended
// sessions. Uses the same database, email and data-protection setup as the API (the key ring is
// shared so unsubscribe links made here verify in the API). Never runs migrations — the API does that on startup.
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<ImovaDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Default"),
        npgsqlOptions => npgsqlOptions.UseNetTopologySuite()));
builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ImovaDbContext>());
builder.Services.AddScoped<IListingSearch, ListingSearch>();

// Alert emails show each listing's main photo (its public blob URL); the photo-sizes backfill
// reads originals and writes their display sizes.
builder.Services.AddSingleton(
    builder.Configuration.GetSection(BlobStorageOptions.SectionName).Get<BlobStorageOptions>()
    ?? throw new InvalidOperationException($"Configuration section \"{BlobStorageOptions.SectionName}\" is missing."));
builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();
builder.Services.AddSingleton<IPhotoResizer, MagickPhotoResizer>();
builder.Services.AddScoped<PhotoSizeGenerator>();

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

builder.Services.AddSingleton(builder.Configuration.GetSection(AppOptions.SectionName).Get<AppOptions>() ?? new AppOptions());
builder.Services.AddDataProtection()
    .SetApplicationName("Imova.Api")
    .PersistKeysToDbContext<ImovaDbContext>();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddScoped<SavedSearchUnsubscribeTokens>();
builder.Services.AddScoped<FavoriteAlertUnsubscribeTokens>();

// Agencies' CSV imports: listings are placed on the map like the form's (the geocoder, same
// "Geocoding" settings as the API) and priced in EUR (the "ExchangeRates" settings).
var geocodingOptions = builder.Configuration.GetSection(GeocodingOptions.SectionName).Get<GeocodingOptions>() ?? new GeocodingOptions();
builder.Services.AddSingleton(geocodingOptions);
builder.Services.AddSingleton(
    builder.Configuration.GetSection(ServiceIdentityOptions.SectionName).Get<ServiceIdentityOptions>() ?? new ServiceIdentityOptions());
builder.Services.AddHttpClient<IGeocoderClient, NominatimGeocodingService>((sp, client) =>
{
    var options = sp.GetRequiredService<GeocodingOptions>();
    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd(sp.GetRequiredService<ServiceIdentityOptions>().UserAgent);
    client.Timeout = TimeSpan.FromSeconds(10);
});
// The same shared cache and request slots as the API (GeocodedAddresses, GeocoderThrottle).
builder.Services.AddScoped<IGeocodingStore, GeocodingStore>();
builder.Services.AddScoped<IGeocodingService, SharedGeocodingService>();
builder.Services.AddSingleton(
    builder.Configuration.GetSection(ExchangeRateOptions.SectionName).Get<ExchangeRateOptions>() ?? new ExchangeRateOptions());
builder.Services.AddSingleton<IExchangeRateProvider, ConfiguredExchangeRateProvider>();
// Photos named in agencies' CSVs: public internet addresses only, checked on every connection
// (ImportImageFetcher); one instance, so its connections are pooled.
builder.Services.AddSingleton<IImportImageFetcher, ImportImageFetcher>();
builder.Services.AddScoped<ImportPhotoSync>();
builder.Services.AddScoped<ImportListingWriter>();

// Each job on its own timer; "<Section>:IntervalSeconds" overrides the default.
builder.Services.AddScheduledJob<SavedSearchAlerts>(builder.Configuration, "SavedSearchAlerts", defaultIntervalSeconds: 300);
builder.Services.AddScheduledJob<FavoriteAlerts>(builder.Configuration, "FavoriteAlerts", defaultIntervalSeconds: 3600);
builder.Services.AddScheduledJob<ListingExpiry>(builder.Configuration, "ListingExpiry", defaultIntervalSeconds: 3600);
builder.Services.AddScheduledJob<PhotoSizeBackfill>(builder.Configuration, "PhotoSizes", defaultIntervalSeconds: 300);
builder.Services.AddScheduledJob<AbandonedPhotoCleanup>(builder.Configuration, "PhotoCleanup", defaultIntervalSeconds: 6 * 3600);
builder.Services.AddScheduledJob<RefreshTokenCleanup>(builder.Configuration, "SessionCleanup", defaultIntervalSeconds: 6 * 3600);
builder.Services.AddScheduledJob<ListingVisitorMarkCleanup>(builder.Configuration, "ListingVisitorCleanup", defaultIntervalSeconds: 6 * 3600);
builder.Services.AddScheduledJob<ListingImports>(builder.Configuration, "ListingImports", defaultIntervalSeconds: 15);

builder.Build().Run();
