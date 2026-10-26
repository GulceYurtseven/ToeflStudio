using System.Text.RegularExpressions;
using ToeflExam.Infrastructure.ContentBank;

namespace ToeflExam.IntegrationTests;

public sealed class ContentBankTests
{
    [Fact]
    public void Practice_has_fifty_new_questions_per_skill_and_no_exam_content()
    {
        var bank = new EmbeddedContentBank();
        foreach (var section in new[] { "reading", "listening", "writing", "speaking" }) Assert.Equal(50, bank.Practice.Where(x => x.Section == section).Sum(x => x.QuestionCount));
        Assert.All(bank.Practice, e => { Assert.Equal("practice", e.Pool); Assert.Empty(e.ExamId); });
        var all = bank.Practice.Concat(bank.Exams).ToArray();
        Assert.Equal(all.Length, all.Select(x => x.Id).Distinct().Count());
        // Ignore short generic directions; compare the actual authored stimulus across every pool/form.
        var bodies = all.Select(x => Regex.Replace(x.Text + x.Context + x.Prompt + x.Prefix + string.Join(" ", x.Order.Select(n => x.Tiles.Single(t => t.Id == n).Text)) + x.Suffix, @"\s+", " ").Trim().ToLowerInvariant()).ToArray();
        Assert.Equal(bodies.Length, bodies.Distinct().Count());
    }
    [Theory]
    [InlineData("mock-1")]
    [InlineData("mock-2")]
    [InlineData("mock-3")]
    public void Every_form_has_both_complete_adaptive_branches_and_linear_productive_sections(string form)
    {
        var entries = new EmbeddedContentBank().Exams.Where(x => x.ExamId == form).ToArray();
        int Count(string section, string stage, string kind) => entries.Where(x => x.Section == section && x.Stage == stage && x.Kind == kind).Sum(x => x.QuestionCount);
        foreach (var (stage, complete, daily, academic) in new[] { ("router", 10, 5, 5), ("lower", 10, 5, 0), ("upper", 10, 0, 5) })
        { Assert.Equal(complete, Count("reading", stage, "complete-words")); Assert.Equal(daily, Count("reading", stage, "daily-life")); Assert.Equal(academic, Count("reading", stage, "academic-passage")); }
        foreach (var (stage, response, conversation, announcement, talk) in new[] { ("router", 8, 4, 4, 4), ("lower", 7, 4, 4, 0), ("upper", 3, 4, 0, 8) })
        { Assert.Equal(response, Count("listening", stage, "choose-response")); Assert.Equal(conversation, Count("listening", stage, "conversation")); Assert.Equal(announcement, Count("listening", stage, "announcement")); Assert.Equal(talk, Count("listening", stage, "academic-talk")); }
        Assert.Equal(10, Count("writing", "linear", "build-sentence")); Assert.Equal(1, Count("writing", "linear", "email")); Assert.Equal(1, Count("writing", "linear", "academic-discussion"));
        Assert.Equal(7, Count("speaking", "linear", "listen-repeat")); Assert.Equal(4, Count("speaking", "linear", "interview"));
        foreach (var branch in new[] { "lower", "upper" }) Assert.Equal(93, entries.Where(x => x.Stage is "router" or "linear" || x.Stage == branch).Sum(x => x.QuestionCount));
    }
    [Fact]
    public void All_materials_have_valid_keys_lengths_and_original_audio_files()
    {
        var bank = new EmbeddedContentBank();
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/ToeflExam.Api/Content"));
        foreach (var e in bank.Practice.Concat(bank.Exams))
        {
            var words = Regex.Matches(e.Text, @"\b[\p{L}\p{N}]+(?:['’-][\p{L}\p{N}]+)*\b").Count;
            if (e.Kind == "academic-talk") Assert.True(words is >= 175 and <= 250, $"{e.Title}: {words} spoken words");
            if (e.Kind == "academic-passage") Assert.True(words is >= 180 and <= 230, $"{e.Title}: {words} words");
            if (e.Kind == "daily-life") Assert.InRange(words, 15, 150);
            if (e.Kind == "complete-words")
            {
                Assert.Equal(10, e.Questions.Length); Assert.Equal(10, Regex.Matches(e.Text, @"\{\{\d+\}\}").Count);
                Assert.All(e.Questions, x => { Assert.Equal(x.MissingLetters, x.Answer.Length); Assert.Equal((x.Prefix.Length + x.Answer.Length) / 2, x.Prefix.Length); });
            }
            Assert.All(e.Questions.Where(x => x.Options.Length > 0), x => { Assert.Equal(4, x.Options.Length); Assert.Equal(4, x.Options.Select(o => o.Text).Distinct().Count()); Assert.Single(x.Options, o => o.Id == x.Answer); Assert.NotEmpty(x.Explanation); });
            if (e.Kind == "build-sentence") { Assert.NotEmpty(e.Order); Assert.Equal(e.Order.Length, e.Order.Distinct().Count()); Assert.All(e.Order, n => Assert.Single(e.Tiles, t => t.Id == n)); Assert.NotEmpty(e.Explanation); }
            if (e.Kind == "email") Assert.Equal(3, e.Requirements.Length);
            if (e.Kind == "academic-discussion") Assert.Equal(2, e.Posts.Length);
            if (e.Section is "listening" or "speaking")
            {
                Assert.Matches("^[a-f0-9]{32}\\.wav$", e.AudioFile);
                var file = Path.Combine(root, e.Section == "speaking" && e.Pool == "practice" ? "Speaking" : "Bank", e.AudioFile);
                Assert.True(File.Exists(file), file); Assert.True(new FileInfo(file).Length > 1000);
                Assert.DoesNotMatch("[çğıöşüÇĞİÖŞÜ]", e.Context + e.Instructions + e.Prompt + e.Text);
            }
        }
    }
}
