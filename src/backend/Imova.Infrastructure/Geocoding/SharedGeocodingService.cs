using System.Data;
using Imova.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace Imova.Infrastructure.Geocoding;

// What one lookup at the geocoder came back with: a place, "no such place", or a failure (network,
// 5xx, bad JSON). Only the first two are worth remembering.
public enum GeocodeOutcome
{
    Found = 1,
    NotFound = 2,
    Failed = 3,
}

public sealed record GeocodeLookup(GeocodeOutcome Outcome, GeocodingResult? Result)
{
    public static readonly GeocodeLookup NotFound = new(GeocodeOutcome.NotFound, null);
    public static readonly GeocodeLookup Failed = new(GeocodeOutcome.Failed, null);
}

// The geocoder itself (Nominatim), without any throttling of its own.
public interface IGeocoderClient
{
    Task<GeocodeLookup> LookupAsync(string address, CancellationToken cancellationToken);
}

// The cache and the shared request slots (Postgres in production — GeocodingStore).
public interface IGeocodingStore
{
    Task<GeocodedAddress?> FindAsync(string key, CancellationToken cancellationToken);

    Task SaveAsync(GeocodedAddress address, CancellationToken cancellationToken);

    // Reserves the next request slot `interval` after the last one and answers how long to wait for it.
    Task<TimeSpan> ReserveSlotAsync(TimeSpan interval, CancellationToken cancellationToken);
}

// IGeocodingService for every process (API and Worker): a cached answer when this address (normalised,
// GeocodingKey) was looked up recently; otherwise one request to the geocoder in a slot reserved in
// the database, so all processes together keep to its limit (Nominatim: 1 request a second). Never
// throws (IGeocodingService's contract): a failure answers null, like a place that wasn't found.
public sealed class SharedGeocodingService(
    IGeocoderClient client,
    IGeocodingStore store,
    GeocodingOptions options,
    TimeProvider timeProvider,
    ILogger<SharedGeocodingService> logger) : IGeocodingService
{
    public async Task<GeocodingResult?> GeocodeAsync(string address, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        var key = GeocodingKey.Normalize(address);
        try
        {
            var cached = await store.FindAsync(key, cancellationToken);
            if (cached is not null && IsFresh(cached))
            {
                return cached.Latitude is { } lat && cached.Longitude is { } lon
                    ? new GeocodingResult(lat, lon, cached.FormattedAddress ?? address)
                    : null;
            }

            var wait = await store.ReserveSlotAsync(TimeSpan.FromMilliseconds(options.MinRequestIntervalMilliseconds), cancellationToken);
            if (wait > TimeSpan.Zero)
            {
                await Task.Delay(wait, timeProvider, cancellationToken);
            }

            var lookup = await client.LookupAsync(address, cancellationToken);
            if (lookup.Outcome != GeocodeOutcome.Failed)
            {
                await store.SaveAsync(
                    new GeocodedAddress
                    {
                        Key = key,
                        Latitude = lookup.Result?.Latitude,
                        Longitude = lookup.Result?.Longitude,
                        FormattedAddress = Shorten(lookup.Result?.FormattedAddress),
                        LookedUpAt = timeProvider.GetUtcNow(),
                    },
                    cancellationToken);
            }

            return lookup.Result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Geocoding failed for address \"{Address}\".", address);
            return null;
        }
    }

    private bool IsFresh(GeocodedAddress cached)
    {
        var days = cached.Latitude is null ? options.NotFoundCacheDays : options.CacheDays;
        return timeProvider.GetUtcNow() - cached.LookedUpAt < TimeSpan.FromDays(days);
    }

    private static string? Shorten(string? value) => value is { Length: > 500 } ? value[..500] : value;
}

// The Postgres side: a cache row per normalised address, and the single throttle row. Raw SQL on the
// caller's context, never its change tracker — so a long job's unit of work (the CSV import) isn't
// disturbed by a lookup in the middle of it.
public sealed class GeocodingStore(ImovaDbContext dbContext) : IGeocodingStore
{
    public Task<GeocodedAddress?> FindAsync(string key, CancellationToken cancellationToken) =>
        dbContext.Set<GeocodedAddress>().AsNoTracking().FirstOrDefaultAsync(a => a.Key == key, cancellationToken);

    // Typed parameters: "no such place" stores NULL coordinates, which untyped raw-SQL values can't carry.
    public async Task SaveAsync(GeocodedAddress address, CancellationToken cancellationToken) =>
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO "GeocodedAddresses" ("Key", "Latitude", "Longitude", "FormattedAddress", "LookedUpAt")
            VALUES (@key, @latitude, @longitude, @formatted, @lookedUpAt)
            ON CONFLICT ("Key") DO UPDATE SET
                "Latitude" = EXCLUDED."Latitude",
                "Longitude" = EXCLUDED."Longitude",
                "FormattedAddress" = EXCLUDED."FormattedAddress",
                "LookedUpAt" = EXCLUDED."LookedUpAt"
            """,
            [
                new NpgsqlParameter("key", NpgsqlDbType.Varchar) { Value = address.Key },
                new NpgsqlParameter("latitude", NpgsqlDbType.Double) { Value = (object?)address.Latitude ?? DBNull.Value },
                new NpgsqlParameter("longitude", NpgsqlDbType.Double) { Value = (object?)address.Longitude ?? DBNull.Value },
                new NpgsqlParameter("formatted", NpgsqlDbType.Varchar) { Value = (object?)address.FormattedAddress ?? DBNull.Value },
                new NpgsqlParameter("lookedUpAt", NpgsqlDbType.TimestampTz) { Value = address.LookedUpAt },
            ],
            cancellationToken);

    // One atomic statement: move the next free slot `interval` past max(now, the last reserved slot)
    // and return how far away the reserved slot is. No lock is held while the caller waits.
    public async Task<TimeSpan> ReserveSlotAsync(TimeSpan interval, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var opened = connection.State != ConnectionState.Open;
        if (opened)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                WITH slot AS (
                    UPDATE "GeocoderThrottle"
                    SET "NextSlotAt" = GREATEST("NextSlotAt", clock_timestamp()) + make_interval(secs => @seconds)
                    WHERE "Id" = @id
                    RETURNING "NextSlotAt" - make_interval(secs => @seconds) AS "Slot")
                SELECT GREATEST(0, EXTRACT(EPOCH FROM ("Slot" - clock_timestamp())))::float8 FROM slot
                """;
            var seconds = command.CreateParameter();
            seconds.ParameterName = "seconds";
            seconds.Value = interval.TotalSeconds;
            command.Parameters.Add(seconds);
            var id = command.CreateParameter();
            id.ParameterName = "id";
            id.Value = GeocoderThrottle.SingleRowId;
            command.Parameters.Add(id);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is double wait ? TimeSpan.FromSeconds(wait) : TimeSpan.Zero;
        }
        finally
        {
            if (opened)
            {
                await connection.CloseAsync();
            }
        }
    }
}
