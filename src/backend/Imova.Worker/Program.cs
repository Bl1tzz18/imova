using Imova.Application.Common;
using Imova.Application.Common.Interfaces;
using Imova.Application.Features.Listings.SearchListings;
using Imova.Application.Features.SavedSearches;
using Imova.Application.Features.SavedSearches.Alerts;
using Imova.Infrastructure;
using Imova.Infrastructure.Email;
using Imova.Infrastructure.Listings;
using Imova.Infrastructure.Storage;
using Imova.Worker;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

// Background jobs, separate from the API: the saved-search alert emails for now. Uses the same
// database, email and data-protection setup as the API (the key ring is shared so unsubscribe
// links made here verify in the API). Never runs migrations — the API does that on startup.
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<ImovaDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Default"),
        npgsqlOptions => npgsqlOptions.UseNetTopologySuite()));
builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ImovaDbContext>());
builder.Services.AddScoped<IListingSearch, ListingSearch>();

// Alert emails show each listing's main photo — its public blob URL.
builder.Services.AddSingleton(
    builder.Configuration.GetSection(BlobStorageOptions.SectionName).Get<BlobStorageOptions>()
    ?? throw new InvalidOperationException($"Configuration section \"{BlobStorageOptions.SectionName}\" is missing."));
builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();

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
builder.Services.AddScoped<SavedSearchAlerts>();
builder.Services.AddSingleton(
    builder.Configuration.GetSection(SavedSearchAlertsWorkerOptions.SectionName).Get<SavedSearchAlertsWorkerOptions>()
    ?? new SavedSearchAlertsWorkerOptions());
builder.Services.AddHostedService<SavedSearchAlertsWorker>();

builder.Build().Run();
