using CorporateResponsibility.Api.Models;
using CorporateResponsibility.Api.Services;

var builder = WebApplication.CreateBuilder(args);

if (Environment.GetEnvironmentVariable("PORT") is { Length: > 0 } port)
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IPhraseGenerator, PhraseGenerator>();
builder.Services.AddSingleton<ISearchQueryExtractor, SearchQueryExtractor>();
builder.Services.AddHttpClient<IImageService, PixabayImageService>(client =>
{
    client.BaseAddress = new Uri("https://pixabay.com/");
    client.Timeout = TimeSpan.FromSeconds(5);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("CorporateResponsibilityApi/1.0");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});
builder.Services.AddOpenApi();

var allowedOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS")
    ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins is { Length: > 0 })
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader();
        }
        else
        {
            policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        }
    });
});

var app = builder.Build();

app.UseCors();
app.MapOpenApi();

app.MapGet("/", (IPhraseGenerator generator) =>
    Results.Ok(new PhraseResponse(generator.GeneratePhrase())))
    .WithName("GetRootPhrase");

app.MapGet("/api/phrase", (IPhraseGenerator generator) =>
    Results.Ok(new PhraseResponse(generator.GeneratePhrase())))
    .WithName("GetPhrase");

app.MapGet("/api/phrases", (IPhraseGenerator generator, int count = 5) =>
{
    if (count < 1 || count > 20)
    {
        return Results.BadRequest(new { error = "count must be between 1 and 20." });
    }

    return Results.Ok(new PhrasesBatchResponse(generator.GeneratePhrases(count), count));
})
    .WithName("GetPhrasesBatch");

app.MapGet("/api/responsibility", async (
    IPhraseGenerator generator,
    ISearchQueryExtractor queryExtractor,
    IImageService imageService,
    CancellationToken cancellationToken) =>
{
    var phrase = generator.GeneratePhrase();
    var extractedQuery = queryExtractor.ExtractSearchQuery(phrase);
    var image = await imageService.GetBackgroundImageAsync(extractedQuery, cancellationToken);

    return Results.Ok(new ResponsibilityResponse(phrase, image.ImageUrl, image.SearchQuery));
})
    .WithName("GetResponsibility");

app.MapGet("/health", () =>
    Results.Ok(new HealthResponse("ok")))
    .WithName("GetHealth");

app.Run();

public partial class Program;
