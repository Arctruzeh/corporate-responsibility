using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;

namespace CorporateResponsibility.Api.Services;

public sealed record ImageLookupResult(string ImageUrl, string SearchQuery);

public interface IImageService
{
    Task<ImageLookupResult> GetBackgroundImageAsync(string? query, CancellationToken cancellationToken = default);
}

public sealed class PixabayImageService : IImageService
{
    private const string PoolCacheKey = "pixabay:pool";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(6);
    private static readonly object PoolLock = new();

    private static readonly string[] FallbackSearchTerms =
    [
        "work", "job", "corporate", "hr", "human resources",
        "office", "hire", "meeting", "board room", "contract"
    ];

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PixabayImageService> _logger;

    public PixabayImageService(
        HttpClient httpClient,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<PixabayImageService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ImageLookupResult> GetBackgroundImageAsync(string? query, CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["PIXABAY_API_KEY"] ?? _configuration["Pixabay:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("PIXABAY_API_KEY is not configured; returning empty image URL.");
            return new ImageLookupResult(string.Empty, query ?? string.Empty);
        }

        var usedTerm = string.IsNullOrWhiteSpace(query)
            ? FallbackSearchTerms[Random.Shared.Next(FallbackSearchTerms.Length)]
            : query.Trim();

        try
        {
            var hits = await QueryPixabayWithCacheAsync(apiKey, usedTerm, cancellationToken);

            if (hits.Count == 0 && !string.IsNullOrWhiteSpace(query))
            {
                usedTerm = FallbackSearchTerms[Random.Shared.Next(FallbackSearchTerms.Length)];
                hits = await QueryPixabayWithCacheAsync(apiKey, usedTerm, cancellationToken);
            }

            if (hits.Count == 0)
            {
                return new ImageLookupResult(TryGetPoolImage(), usedTerm);
            }

            var selected = hits[Random.Shared.Next(hits.Count)];
            return new ImageLookupResult(selected.LargeImageUrl ?? string.Empty, usedTerm);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Pixabay background image for query '{Query}'.", usedTerm);
            return new ImageLookupResult(TryGetPoolImage(), usedTerm);
        }
    }

    private async Task<IReadOnlyList<PixabayHit>> QueryPixabayWithCacheAsync(
        string apiKey,
        string term,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"pixabay:hits:{term.ToLowerInvariant()}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<PixabayHit>? cachedHits) && cachedHits is { Count: > 0 })
        {
            return cachedHits;
        }

        var requestUri =
            $"api/?key={Uri.EscapeDataString(apiKey)}&q={Uri.EscapeDataString(term)}&image_type=photo&category=business&per_page=40&pretty=false";

        using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Pixabay API failed with status {(int)response.StatusCode}.");
        }

        var payload = await response.Content.ReadFromJsonAsync<PixabayResponse>(cancellationToken: cancellationToken);
        var hits = payload?.Hits?
            .Where(h => !string.IsNullOrWhiteSpace(h.LargeImageUrl))
            .ToArray() ?? [];

        if (hits.Length > 0)
        {
            _cache.Set(cacheKey, (IReadOnlyList<PixabayHit>)hits, CacheDuration);
            AddUrlsToPool(hits.Select(h => h.LargeImageUrl!));
        }

        return hits;
    }

    private void AddUrlsToPool(IEnumerable<string> urls)
    {
        lock (PoolLock)
        {
            var existing = _cache.Get<string[]>(PoolCacheKey) ?? [];
            var updated = existing
                .Concat(urls)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            _cache.Set(PoolCacheKey, updated, CacheDuration);
        }
    }

    private string TryGetPoolImage()
    {
        var pool = _cache.Get<string[]>(PoolCacheKey);
        if (pool is not { Length: > 0 })
        {
            return string.Empty;
        }

        return pool[Random.Shared.Next(pool.Length)];
    }

    private sealed record PixabayResponse(
        [property: JsonPropertyName("hits")] IReadOnlyList<PixabayHit>? Hits);

    private sealed record PixabayHit(
        [property: JsonPropertyName("largeImageURL")] string? LargeImageUrl);
}
