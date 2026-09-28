namespace Imova.Application.Common.Interfaces;

// A job the Worker runs on a timer (see Imova.Worker's ScheduledJobWorker). Each run must be safe
// to repeat or interrupt: whatever it didn't finish, the next run picks up. Returns how many
// things it did (emails sent, listings expired, …) — only used for logging.
public interface IScheduledJob
{
    Task<int> RunAsync(CancellationToken cancellationToken);
}
