using System.Net;
using System.Net.Http.Json;
using System.Text;
using CorporateResponsibility.Api.Models;
using CorporateResponsibility.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CorporateResponsibility.Api.Tests;

public class PhraseGeneratorTests
{
    [Fact]
    public void GeneratePhrase_ReturnsNonEmptyFourPartStatement()
    {
        var generator = new PhraseGenerator();

        var phrase = generator.GeneratePhrase();

        Assert.False(string.IsNullOrWhiteSpace(phrase));
        Assert.True(phrase.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 4);
    }

    [Fact]
    public void GeneratePhrase_WithSeededRandom_IsDeterministic()
    {
        var first = new PhraseGenerator(new Random(42)).GeneratePhrase();
        var second = new PhraseGenerator(new Random(42)).GeneratePhrase();

        Assert.Equal(first, second);
    }

    [Fact]
    public void GeneratePhrases_ReturnsRequestedCount()
    {
        var generator = new PhraseGenerator();

        var phrases = generator.GeneratePhrases(4);

        Assert.Equal(4, phrases.Count);
        Assert.All(phrases, p => Assert.False(string.IsNullOrWhiteSpace(p)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(21)]
    public void GeneratePhrases_ThrowsForOutOfRangeCount(int invalidCount)
    {
        var generator = new PhraseGenerator();

        Assert.Throws<ArgumentOutOfRangeException>(() => generator.GeneratePhrases(invalidCount));
    }
}

public class SearchQueryExtractorTests
{
    private readonly SearchQueryExtractor _extractor = new();

    [Theory]
    [InlineData("Thoughtfully Modernize Cloud-Native Commerce Platforms", "ecommerce business")]
    [InlineData("Strategically Align Executive Leadership Alignment", "business team meeting")]
    [InlineData("Reliably Harden Auditable Security Posture", "corporate contract")]
    [InlineData("Methodically Foster Resilient Business Continuity", "business strategy")]
    [InlineData("Collectively Calibrate Balanced Resource Allocation", "business analytics")]
    [InlineData("Disciplinedly Standardize Modular Workflows", "technology office")]
    public void ExtractSearchQuery_MapsCorporateThemes(string phrase, string expectedQuery)
    {
        Assert.Equal(expectedQuery, _extractor.ExtractSearchQuery(phrase));
    }

    [Fact]
    public void ExtractSearchQuery_ExtractsTrailingConcreteNounWhenNoThemeMatches()
    {
        Assert.Equal("architecture", _extractor.ExtractSearchQuery("Dramatically streamline bespoke architecture"));
    }

    [Fact]
    public void ExtractSearchQuery_ReturnsNullForPurelyAbstractBuzzwords()
    {
        Assert.Null(_extractor.ExtractSearchQuery("Synergistically leverage holistic paradigms"));
    }
}

public class PixabayImageServiceTests
{
    private static IConfiguration BuildConfig(string? apiKey) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(apiKey is null
                ? []
                : [new KeyValuePair<string, string?>("PIXABAY_API_KEY", apiKey)])
            .Build();

    [Fact]
    public async Task GetBackgroundImageAsync_FetchesFromPixabayAndCachesHits()
    {
        var callCount = 0;
        Uri? capturedUri = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            callCount++;
            capturedUri = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"hits":[{"largeImageURL":"https://example.com/tech.jpg"}]}""",
                    Encoding.UTF8,
                    "application/json")
            };
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://pixabay.com/") };
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new PixabayImageService(
            httpClient,
            cache,
            BuildConfig("test-key"),
            NullLogger<PixabayImageService>.Instance);

        var first = await service.GetBackgroundImageAsync("technology office");
        var second = await service.GetBackgroundImageAsync("technology office");

        Assert.Equal("https://example.com/tech.jpg", first.ImageUrl);
        Assert.Equal("technology office", first.SearchQuery);
        Assert.Equal("https://example.com/tech.jpg", second.ImageUrl);
        Assert.Equal(1, callCount);
        Assert.Contains("category=business", capturedUri?.Query);
        Assert.Contains("key=test-key", capturedUri?.Query);
    }

    [Fact]
    public async Task GetBackgroundImageAsync_FallsBackWhenExtractedQueryHasNoHits()
    {
        var requestedTerms = new List<string>();
        var handler = new StubHttpMessageHandler(request =>
        {
            var query = request.RequestUri?.Query ?? string.Empty;
            requestedTerms.Add(query);

            var body = requestedTerms.Count == 1
                ? """{"hits":[]}"""
                : """{"hits":[{"largeImageURL":"https://example.com/fallback.jpg"}]}""";

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://pixabay.com/") };
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new PixabayImageService(
            httpClient,
            cache,
            BuildConfig("test-key"),
            NullLogger<PixabayImageService>.Instance);

        var result = await service.GetBackgroundImageAsync("architecture");

        Assert.Equal("https://example.com/fallback.jpg", result.ImageUrl);
        Assert.Equal(2, requestedTerms.Count);
        Assert.Contains("q=architecture", requestedTerms[0]);
        Assert.DoesNotContain("q=architecture", requestedTerms[1]);
    }

    [Fact]
    public async Task GetBackgroundImageAsync_ReturnsEmptyUrlWhenHttpFailsOrKeyMissing()
    {
        var failingHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        using var httpClient = new HttpClient(failingHandler) { BaseAddress = new Uri("https://pixabay.com/") };
        using var cache = new MemoryCache(new MemoryCacheOptions());

        var serviceWithHttpError = new PixabayImageService(
            httpClient,
            cache,
            BuildConfig("test-key"),
            NullLogger<PixabayImageService>.Instance);
        var errorResult = await serviceWithHttpError.GetBackgroundImageAsync("office");
        Assert.Equal(string.Empty, errorResult.ImageUrl);

        var serviceWithoutKey = new PixabayImageService(
            httpClient,
            cache,
            BuildConfig(null),
            NullLogger<PixabayImageService>.Instance);
        var missingKeyResult = await serviceWithoutKey.GetBackgroundImageAsync("office");
        Assert.Equal(string.Empty, missingKeyResult.ImageUrl);
    }

    [Fact]
    public async Task GetBackgroundImageAsync_FallsBackToCachedPoolWhenRateLimited()
    {
        var callCount = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            callCount++;
            if (callCount == 1)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"hits":[{"largeImageURL":"https://example.com/cached-pool.jpg"}]}""",
                        Encoding.UTF8,
                        "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://pixabay.com/") };
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new PixabayImageService(
            httpClient,
            cache,
            BuildConfig("test-key"),
            NullLogger<PixabayImageService>.Instance);

        var warmed = await service.GetBackgroundImageAsync("office");
        Assert.Equal("https://example.com/cached-pool.jpg", warmed.ImageUrl);

        var rateLimited = await service.GetBackgroundImageAsync("business analytics");
        Assert.Equal("https://example.com/cached-pool.jpg", rateLimited.ImageUrl);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(responder(request));
    }
}

public class ApiEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/api/phrase")]
    public async Task PhraseEndpoints_ReturnOkWithPhrase(string endpoint)
    {
        var response = await _client.GetAsync(endpoint);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PhraseResponse>();
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload.Phrase));
    }

    [Fact]
    public async Task PhrasesBatchEndpoint_ReturnsRequestedCount()
    {
        var response = await _client.GetAsync("/api/phrases?count=3");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PhrasesBatchResponse>();
        Assert.NotNull(payload);
        Assert.Equal(3, payload.Count);
        Assert.Equal(3, payload.Phrases.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    public async Task PhrasesBatchEndpoint_ReturnsBadRequestForInvalidCount(int count)
    {
        var response = await _client.GetAsync($"/api/phrases?count={count}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResponsibilityEndpoint_ReturnsOkWithPhraseAndImage()
    {
        using var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IImageService>(new FakeImageService());
            });
        });

        var client = customFactory.CreateClient();
        var response = await client.GetAsync("/api/responsibility");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ResponsibilityResponse>();
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload.Phrase));
        Assert.Equal("https://example.com/mock-bg.jpg", payload.ImageUrl);
        Assert.False(string.IsNullOrWhiteSpace(payload.SearchQuery));
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.Equal("ok", payload?.Status);
    }

    private sealed class FakeImageService : IImageService
    {
        public Task<ImageLookupResult> GetBackgroundImageAsync(string? query, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ImageLookupResult("https://example.com/mock-bg.jpg", query ?? "office"));
    }
}
