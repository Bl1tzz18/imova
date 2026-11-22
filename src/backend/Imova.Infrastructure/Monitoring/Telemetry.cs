using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Imova.Infrastructure.Monitoring;

// Errors, logs, requests, outgoing calls and database timings go to Azure Application Insights
// (OpenTelemetry + the Azure Monitor exporter) — only when APPLICATIONINSIGHTS_CONNECTION_STRING is
// set, so local runs, compose and CI send nothing. Server-side only: the web app has no browser SDK,
// which would set cookies (see the privacy policy's cookie table). Application Insights drops the
// client IP on ingestion by default — keep it that way. Each app shows up as its own "cloud role"
// (ServiceName), tagged with the release (App:Version, the image's git commit).
public static class Telemetry
{
    public const string ConnectionStringKey = "APPLICATIONINSIGHTS_CONNECTION_STRING";
    public const string VersionKey = "App:Version";

    public static bool IsEnabled(IConfiguration configuration) =>
        !string.IsNullOrWhiteSpace(configuration[ConnectionStringKey]);

    // Health probes (uptime tests, the container platform) every few seconds would be most of the
    // traffic and cost; their failures still show up in the uptime tests themselves.
    public static bool ShouldRecordRequest(HttpContext context) =>
        !context.Request.Path.StartsWithSegments("/health");

    public static void AddImovaTelemetry(this IHostApplicationBuilder builder, string serviceName, bool isWebApp)
    {
        if (!IsEnabled(builder.Configuration))
        {
            return;
        }

        var connectionString = builder.Configuration[ConnectionStringKey]!;
        var version = builder.Configuration[VersionKey] ?? "dev";

        // The exporter's own usage reports ("Statsbeat") go to Microsoft's endpoint whatever our
        // connection string says — off, so telemetry only goes where we send it. Live Metrics (a
        // stream to another endpoint) isn't needed at this size.
        if (Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_STATSBEAT_DISABLED") is null)
        {
            Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_STATSBEAT_DISABLED", "true");
        }

        void Configure(AzureMonitorExporterOptions o)
        {
            o.ConnectionString = connectionString;
            o.EnableLiveMetrics = false;
        }

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            // The ids of a business event (AppEvents) travel as a scope — they become its custom dimensions.
            logging.IncludeScopes = true;
            logging.AddAzureMonitorLogExporter(o =>
            {
                Configure(o);
                // Every log line goes, whatever happens to its request's trace — a business event
                // (AppEvents) or an error must never be sampled away.
                o.EnableTraceBasedLogsSampler = false;
            });
        });
        // Every EF query is already a database span with its timing; its Information log line would
        // only double the volume (and the bill). Warnings and errors still go.
        builder.Logging.AddFilter<OpenTelemetryLoggerProvider>("Microsoft.EntityFrameworkCore", LogLevel.Warning);

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName, serviceVersion: version))
            .WithTracing(tracing =>
            {
                if (isWebApp)
                {
                    tracing.AddAspNetCoreInstrumentation(o => o.Filter = ShouldRecordRequest);
                }

                tracing
                    .AddHttpClientInstrumentation()
                    .AddNpgsql()
                    .AddProcessor(new TelemetryRedactionProcessor())
                    .AddAzureMonitorTraceExporter(Configure);
            })
            .WithMetrics(metrics =>
            {
                if (isWebApp)
                {
                    metrics.AddAspNetCoreInstrumentation();
                }

                metrics
                    .AddHttpClientInstrumentation()
                    .AddAzureMonitorMetricExporter(Configure);
            });
    }
}
