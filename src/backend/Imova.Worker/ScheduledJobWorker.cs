using Imova.Application.Common.Interfaces;

namespace Imova.Worker;

// How often one job runs: "<section>:IntervalSeconds" in configuration (e.g.
// SavedSearchAlerts:IntervalSeconds), else the job's default. At least 10 seconds.
public sealed record JobSchedule<TJob>(string Name, TimeSpan Interval)
    where TJob : IScheduledJob;

// Runs one IScheduledJob on a timer, starting right away, in a fresh DI scope each time (its own
// DbContext). A failed run (e.g. the database not migrated yet while the API is still starting)
// is logged and simply retried on the next tick — the jobs are written to be safely repeatable.
public sealed class ScheduledJobWorker<TJob>(
    IServiceScopeFactory scopeFactory,
    JobSchedule<TJob> schedule,
    ILogger<ScheduledJobWorker<TJob>> logger) : BackgroundService
    where TJob : IScheduledJob
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(schedule.Interval);
        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var done = await scope.ServiceProvider.GetRequiredService<TJob>().RunAsync(stoppingToken);
                if (done > 0)
                {
                    logger.LogInformation("{Job}: {Count} done.", schedule.Name, done);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "{Job} run failed; retrying on the next tick.", schedule.Name);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}

public static class ScheduledJobRegistration
{
    public static IServiceCollection AddScheduledJob<TJob>(
        this IServiceCollection services, IConfiguration configuration, string section, int defaultIntervalSeconds)
        where TJob : class, IScheduledJob
    {
        var seconds = configuration.GetValue($"{section}:IntervalSeconds", defaultIntervalSeconds);
        services.AddScoped<TJob>();
        services.AddSingleton(new JobSchedule<TJob>(section, TimeSpan.FromSeconds(Math.Max(10, seconds))));
        return services.AddHostedService<ScheduledJobWorker<TJob>>();
    }
}
