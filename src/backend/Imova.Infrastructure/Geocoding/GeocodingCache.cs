using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imova.Infrastructure.Geocoding;

// One looked-up address (table GeocodedAddresses), shared by the API and the Worker: Key is the
// normalised address (GeocodingKey); no coordinates = the geocoder found nothing (kept for a shorter
// time, GeocodingOptions.NotFoundCacheDays). Lookups that failed (network, 5xx) are never stored.
public sealed class GeocodedAddress
{
    public string Key { get; set; } = string.Empty;

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public string? FormattedAddress { get; set; }

    public DateTimeOffset LookedUpAt { get; set; }
}

// The geocoder's request slots, shared by every process (table GeocoderThrottle, one row): the time
// the next request may go out. A caller reserves the next slot atomically (GeocodingStore) and waits
// for it, so the API and the Worker together never exceed the geocoder's limit.
public sealed class GeocoderThrottle
{
    public const int SingleRowId = 1;

    public int Id { get; set; }

    public DateTimeOffset NextSlotAt { get; set; }
}

public class GeocodedAddressConfiguration : IEntityTypeConfiguration<GeocodedAddress>
{
    public void Configure(EntityTypeBuilder<GeocodedAddress> builder)
    {
        builder.ToTable("GeocodedAddresses");
        builder.HasKey(a => a.Key);
        builder.Property(a => a.Key).HasMaxLength(GeocodingKey.MaxLength);
        builder.Property(a => a.FormattedAddress).HasMaxLength(500);
    }
}

public class GeocoderThrottleConfiguration : IEntityTypeConfiguration<GeocoderThrottle>
{
    public void Configure(EntityTypeBuilder<GeocoderThrottle> builder)
    {
        builder.ToTable("GeocoderThrottle");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.HasData(new GeocoderThrottle { Id = GeocoderThrottle.SingleRowId, NextSlotAt = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero) });
    }
}

// The cache key of an address: the same place written slightly differently is one key — case,
// Romanian diacritics (both comma and cedilla forms), punctuation and spacing don't matter
// ("Str. Dacia, 27, Chișinău" = "str dacia 27 chisinau"). Very long addresses are cut and end in a
// hash so the key stays unique within the column.
public static class GeocodingKey
{
    public const int MaxLength = 400;

    public static string Normalize(string address)
    {
        var builder = new StringBuilder(address.Length);
        var lastWasSpace = true;
        foreach (var c in address.Trim().ToLowerInvariant())
        {
            var mapped = c switch
            {
                'ă' or 'â' => 'a',
                'î' => 'i',
                'ș' or 'ş' => 's',
                'ț' or 'ţ' => 't',
                _ when char.IsLetterOrDigit(c) => c,
                _ => ' ',
            };

            if (mapped == ' ')
            {
                if (!lastWasSpace)
                {
                    builder.Append(' ');
                }

                lastWasSpace = true;
            }
            else
            {
                builder.Append(mapped);
                lastWasSpace = false;
            }
        }

        var key = builder.ToString().TrimEnd();
        if (key.Length <= MaxLength)
        {
            return key;
        }

        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
        return $"{key[..(MaxLength - hash.Length - 1)]} {hash}";
    }
}
