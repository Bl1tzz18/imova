namespace Imova.Infrastructure;

internal static class SeedData
{
    public static readonly DateTimeOffset SeedDate = new(2026, 8, 20, 0, 0, 0, TimeSpan.Zero);

    public static readonly Guid DemoOwnerId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static readonly Guid Property1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static readonly Guid Property2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
}
