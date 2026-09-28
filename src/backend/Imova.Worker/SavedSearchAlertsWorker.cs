using Imova.Application.Features.SavedSearches.Alerts;

namespace Imova.Worker;

// Bound from "SavedSearchAlerts".
public class SavedSearchAlertsWorkerOptions
{
    public const string SectionName = "SavedSearchAlerts";

    // How often the alert job runs — "instant" alerts arrive within this long.
    public int IntervalSeconds { get; set; } = 300;
}

// Runs SavedSearchAlerts on a timer, starting right away. A failed run (e.g. the database not
// migrated yet while the API is still starting) is logged and simply retried on the next tick.
public sealed class SavedSearchAlertsWorker(
    IServiceScopeFactory scopeFactory,
    SavedSearchAlertsWorkerOptions options,
    ILogger<SavedSearchAlertsWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(10, options.IntervalSeconds)));
        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var sent = await scope.ServiceProvider.GetRequiredService<SavedSearchAlerts>().RunAsync(stoppingToken);
                if (sent > 0)
                {
                    logger.LogInformation("Sent {Count} saved-search alert email(s).", sent);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Saved-search alert run failed; retrying on the next tick.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
