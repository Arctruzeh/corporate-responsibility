namespace CorporateResponsibility.Api.Models;

public sealed record PhraseResponse(string Phrase);

public sealed record PhrasesBatchResponse(IReadOnlyList<string> Phrases, int Count);

public sealed record ResponsibilityResponse(string Phrase, string ImageUrl, string SearchQuery);

public sealed record HealthResponse(string Status);
