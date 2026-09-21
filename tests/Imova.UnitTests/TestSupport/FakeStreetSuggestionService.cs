using Imova.Application.Common.Interfaces;

namespace Imova.UnitTests.TestSupport;

internal sealed class FakeStreetSuggestionService : IStreetSuggestionService
{
    public List<StreetSuggestion> ResultToReturn { get; set; } = [];

    public string? LastQueryRequested { get; private set; }

    public string? LastLocalityRequested { get; private set; }

    public Task<List<StreetSuggestion>> SuggestStreetsAsync(string query, string? locality, CancellationToken cancellationToken)
    {
        LastQueryRequested = query;
        LastLocalityRequested = locality;
        return Task.FromResult(ResultToReturn);
    }
}
