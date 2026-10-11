using System.Net;
using System.Net.Sockets;

namespace Imova.Infrastructure.Imports;

// Whether an IP address is on the public internet — the only places an import may fetch photos from.
// Everything private, local, link-local (the cloud metadata service at 169.254.169.254), shared
// (carrier-grade NAT), reserved, documentation, benchmarking or multicast is refused, for IPv4 and
// IPv6 alike, including IPv4 hidden inside IPv6 (mapped, 6to4, NAT64).
public static class PublicAddress
{
    private static readonly (byte[] Prefix, int Bits)[] BlockedV4 =
    [
        ([0, 0, 0, 0], 8),          // "this" network
        ([10, 0, 0, 0], 8),         // private
        ([100, 64, 0, 0], 10),      // shared address space (carrier-grade NAT)
        ([127, 0, 0, 0], 8),        // loopback
        ([169, 254, 0, 0], 16),     // link-local (cloud metadata)
        ([172, 16, 0, 0], 12),      // private
        ([192, 0, 0, 0], 24),       // IETF protocol assignments
        ([192, 0, 2, 0], 24),       // documentation
        ([192, 88, 99, 0], 24),     // 6to4 relay anycast
        ([192, 168, 0, 0], 16),     // private
        ([198, 18, 0, 0], 15),      // benchmarking
        ([198, 51, 100, 0], 24),    // documentation
        ([203, 0, 113, 0], 24),     // documentation
        ([224, 0, 0, 0], 4),        // multicast
        ([240, 0, 0, 0], 4),        // reserved, broadcast
    ];

    private static readonly (byte[] Prefix, int Bits)[] BlockedV6 =
    [
        (new byte[16], 96),                                                  // ::/96 — unspecified, loopback, IPv4-compatible
        ([0x01, 0x00, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0], 64),        // 100::/64 discard
        ([0x20, 0x01, 0x00, 0x00, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0], 32),  // 2001::/32 Teredo
        ([0x20, 0x01, 0x0d, 0xb8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0], 32),  // 2001:db8::/32 documentation
        ([0xfc, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0], 7),            // fc00::/7 unique local
        ([0xfe, 0x80, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0], 10),        // fe80::/10 link-local
        ([0xfe, 0xc0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0], 10),        // fec0::/10 site-local (deprecated)
        ([0xff, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0], 8),            // ff00::/8 multicast
    ];

    public static bool IsPublic(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            return IsPublic(address.MapToIPv4());
        }

        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            return !BlockedV4.Any(range => Matches(bytes, range.Prefix, range.Bits));
        }

        if (address.AddressFamily != AddressFamily.InterNetworkV6)
        {
            return false;
        }

        // IPv4 carried inside IPv6: NAT64 (64:ff9b::/96) and 6to4 (2002::/16) — judged by that IPv4.
        if (Matches(bytes, [0x00, 0x64, 0xff, 0x9b, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0], 96))
        {
            return IsPublic(new IPAddress(bytes[12..16]));
        }

        if (bytes[0] == 0x20 && bytes[1] == 0x02)
        {
            return IsPublic(new IPAddress(bytes[2..6]));
        }

        return !BlockedV6.Any(range => Matches(bytes, range.Prefix, range.Bits));
    }

    private static bool Matches(byte[] address, byte[] prefix, int bits)
    {
        if (address.Length != prefix.Length)
        {
            return false;
        }

        var whole = bits / 8;
        for (var i = 0; i < whole; i++)
        {
            if (address[i] != prefix[i])
            {
                return false;
            }
        }

        var rest = bits % 8;
        if (rest == 0)
        {
            return true;
        }

        var mask = (byte)(0xFF << (8 - rest));
        return (address[whole] & mask) == (prefix[whole] & mask);
    }
}
