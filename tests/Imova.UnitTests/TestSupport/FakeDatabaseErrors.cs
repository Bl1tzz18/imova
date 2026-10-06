using Imova.Application.Common.Interfaces;
using Imova.Domain.Agencies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Imova.UnitTests.TestSupport;

// The in-memory database has no unique indexes: a test that needs "the save broke index X" throws
// a SimulatedUniqueViolation, and this recognises it.
internal sealed class FakeDatabaseErrors : IDatabaseErrors
{
    public bool IsUniqueViolation(DbUpdateException exception, string indexName) =>
        exception is SimulatedUniqueViolation violation && violation.IndexName == indexName;
}

internal sealed class SimulatedUniqueViolation(string indexName) : DbUpdateException($"Duplicate key on {indexName}.")
{
    public string IndexName { get; } = indexName;
}

// Two agencies created with the same name at the same moment: just before our first save, another
// agency (saved through a second context on the same store) takes the slug we picked, and our save
// fails the way Postgres's unique index would make it fail. Later saves go through.
internal sealed class SlugRaceInterceptor(string databaseName) : SaveChangesInterceptor
{
    public int Failures { get; private set; }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        var pending = eventData.Context!.ChangeTracker.Entries<Agency>().Where(e => e.State == EntityState.Added).ToList();
        if (Failures == 0 && pending.Count > 0)
        {
            Failures++;
            await using var competitor = TestDbContextFactory.Create(databaseName);
            competitor.Agencies.Add(Agency.Create(
                new AgencyProfile("Competitor", "+373 22 000 000", "c@example.com"), pending[0].Entity.Slug, Guid.NewGuid(), DateTimeOffset.UtcNow));
            await competitor.SaveChangesAsync(cancellationToken);
            throw new SimulatedUniqueViolation("IX_Agencies_Slug");
        }

        return result;
    }
}
