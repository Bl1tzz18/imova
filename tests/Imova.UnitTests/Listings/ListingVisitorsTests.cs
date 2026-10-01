using Imova.Application.Features.Listings.Visitors;

namespace Imova.UnitTests.Listings;

public class ListingVisitorsTests
{
    [Fact]
    public void ASignedInVisitor_IsTheirAccount_WhateverCookieTheyHave()
    {
        var user = Guid.NewGuid();

        Assert.Equal(ListingVisitors.Hash(user, "cookie-aaaaaaaa"), ListingVisitors.Hash(user, "cookie-bbbbbbbb"));
        Assert.Equal(64, ListingVisitors.Hash(user, null)!.Length);
    }

    [Fact]
    public void AnAnonymousVisitor_IsTheirCookie()
    {
        var id = Guid.NewGuid().ToString();

        Assert.Equal(ListingVisitors.Hash(null, id), ListingVisitors.Hash(null, id));
        Assert.NotEqual(ListingVisitors.Hash(null, id), ListingVisitors.Hash(null, Guid.NewGuid().ToString()));
        // Never the raw id.
        Assert.DoesNotContain(id, ListingVisitors.Hash(null, id)!);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("short")]
    [InlineData("has spaces in it!!")]
    public void AnAnonymousVisitorWithoutAUsableId_IsNotCounted(string? visitorId)
    {
        Assert.Null(ListingVisitors.Hash(null, visitorId));
    }
}
