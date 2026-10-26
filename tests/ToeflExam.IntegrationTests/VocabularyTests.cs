using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ToeflExam.Application.Vocabulary;
using ToeflExam.Infrastructure.Vocabulary;

namespace ToeflExam.IntegrationTests;

/// <summary>Protects source completeness, usable learning content and the authenticated catalog boundary.</summary>
public sealed class VocabularyTests
{
    [Fact]
    public void Every_source_target_has_a_complete_entry_without_duplicate_headwords()
    {
        var catalog = new EmbeddedVocabularyCatalog().Catalog;
        using var inventory = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "VocabularySourceInventory.json")));
        var sources = inventory.RootElement.GetProperty("sources").EnumerateObject().ToArray();
        Assert.Equal(8, sources.Length);
        Assert.Equal(100, sources.Single(x => x.Name == "pdf-academic-100").Value.GetArrayLength());
        Assert.Equal(58, sources.Single(x => x.Name == "image-toefl-writing-word-set").Value.GetArrayLength());
        Assert.Equal(411, sources.Sum(x => x.Value.GetArrayLength()));
        Assert.Equal(378, catalog.Words.Count);
        Assert.Equal(378, catalog.Words.Select(x => x.Word).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(catalog.Words.Select(x => x.Word).Order(StringComparer.OrdinalIgnoreCase), catalog.Words.Select(x => x.Word));

        foreach (var source in sources)
        {
            var expectedWords = source.Value.EnumerateArray().Select(x => x.GetString()!).Order().ToArray();
            var actualWords = catalog.Words.Where(x => x.SourceIds.Contains(source.Name)).Select(x => x.Word).Order().ToArray();
            Assert.Equal(expectedWords, actualWords);
            var provenance = Assert.Single(catalog.Sources, x => x.Id == source.Name);
            Assert.Equal("complete", provenance.Coverage);
            Assert.Equal(expectedWords.Length, provenance.WordIds.Count);
        }
        Assert.All(catalog.Words, word =>
        {
            Assert.NotEmpty(word.PartOfSpeech);
            Assert.InRange(word.Meaning.Length, 20, 500);
            Assert.InRange(word.ExampleSentence.Length, 30, 500);
            Assert.DoesNotMatch("[çğıöşüÇĞİÖŞÜ<>]", word.Word + word.Meaning + word.ExampleSentence);
            Assert.Matches("[.!?]$", word.ExampleSentence);
        });
    }

    [Fact]
    public void Shared_words_keep_all_sources_and_inflections_remain_searchable()
    {
        var catalog = new EmbeddedVocabularyCatalog().Catalog;
        var adapt = Assert.Single(catalog.Words, x => x.Word == "adapt");
        Assert.Contains("pdf-academic-100", adapt.SourceIds);
        Assert.Contains("video-eITSW_5pYm8", adapt.SourceIds);
        Assert.Contains("criteria", catalog.Words.Select(x => x.Word));
        Assert.Contains("criterion", catalog.Words.Single(x => x.Word == "criteria").Aliases);
        Assert.Contains("served", catalog.Words.Single(x => x.Word == "serve").Aliases);
        Assert.Equal("verb / noun", catalog.Words.Single(x => x.Word == "claim").PartOfSpeech);
        Assert.Contains("image-toefl-writing-word-set", catalog.Words.Single(x => x.Word == "however").SourceIds);
        // Runtime collections must not let one request change another learner's catalog.
        Assert.Throws<NotSupportedException>(() => ((IList<VocabularyEntry>)catalog.Words).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<string>)adapt.SourceIds).Clear());
    }

    [Fact]
    public async Task Endpoint_requires_authentication_and_serves_complete_catalog_without_database_access()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        using var anonymous = await client.GetAsync("/api/vocabulary");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        // Issue a real protected Identity token without creating a SQL user: this read path needs no DB.
        var options = factory.Services.GetRequiredService<IOptionsMonitor<BearerTokenOptions>>().Get(IdentityConstants.BearerScheme);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "vocabulary-test")], IdentityConstants.BearerScheme));
        var ticket = new AuthenticationTicket(principal,
            new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(5) }, IdentityConstants.BearerScheme);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.BearerTokenProtector.Protect(ticket));
        using var response = await client.GetAsync("/api/vocabulary");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        var catalog = await response.Content.ReadFromJsonAsync<VocabularyCatalog>();
        Assert.NotNull(catalog);
        Assert.Equal(378, catalog.Words.Count);
        Assert.Equal(8, catalog.Sources.Count);
        using var mutation = await client.PostAsJsonAsync("/api/vocabulary", new { word = "injected" });
        Assert.Equal(HttpStatusCode.MethodNotAllowed, mutation.StatusCode);
    }
}
