using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ToeflExam.Domain.Content;
using ToeflExam.Domain.Speaking;
using ToeflExam.Domain.Writing;
using ToeflExam.Infrastructure.ContentBank;
using ToeflExam.Infrastructure.Persistence.Entities;

namespace ToeflExam.Infrastructure.Persistence;

internal static class PracticeExpansionSeed
{
    /// <summary>Defines the model mapping, constraints or deterministic seed data for this content.</summary>
    public static void Configure(ModelBuilder b)
    {
        var bank = EmbeddedContentBank.PracticeEntries;
        foreach (var module in new[] { "complete-words", "daily-life", "academic-passage" })
        {
            var oldVersion = module == "academic-passage" ? 2 : 1;
            var existing = b.Entity<ReadingSetVersion>().Metadata.GetSeedData().Where(x => (string)x["Module"]! == module && (int)x["Version"]! == oldVersion).OrderBy(x => x["Position"]).ToArray();
            // Reading paths are whole editions. Clone the old material into v3; pinned v1/v2 IDs remain intact.
            foreach (var old in existing)
            {
                var row = JsonSerializer.Deserialize<ReadingSetVersion>(JsonSerializer.Serialize(old))!;
                row.Id = StableId("reading-v3:" + row.Id); row.Version = 3;
                b.Entity<ReadingSetVersion>().HasData(row);
            }
            var position = existing.Length;
            foreach (var entry in bank.Where(x => x.Section == "reading" && x.Kind == module))
                b.Entity<ReadingSetVersion>().HasData(new ReadingSetVersion
                {
                    Id = entry.Id,
                    Module = module,
                    Version = 3,
                    Position = ++position,
                    Title = entry.Title,
                    Format = module == "complete-words" ? "paragraph" : module == "daily-life" ? "notice" : "article",
                    Text = entry.Text,
                    ItemsJson = JsonSerializer.Serialize(entry.Questions),
                    PublicationStatus = PublicationStatus.Published
                });
        }
        var buildPath = Guid.Parse("b5100000-0000-4000-8000-000000000002");
        b.Entity<BuildSentencePathVersion>().HasData(new { Id = buildPath, Version = 2, Title = "Build a Sentence — Expanded practice", IsPublished = true });
        var oldBuild = b.Entity<BuildSentencePathStep>().Metadata.GetSeedData().OrderBy(x => x["Position"]).ToArray();
        foreach (var step in oldBuild) b.Entity<BuildSentencePathStep>().HasData(new { PathVersionId = buildPath, Position = (int)step["Position"]!, QuestionVersionId = (Guid)step["QuestionVersionId"]! });
        var number = oldBuild.Length;
        foreach (var entry in bank.Where(x => x.Kind == "build-sentence"))
        {
            var parts = new[] { new BuildSentenceQuestionMapper.Part(false, entry.Prefix) }.Concat(entry.Order.Select(_ => new BuildSentenceQuestionMapper.Part(true, null))).Append(new(false, entry.Suffix)).ToArray();
            var row = new BuildSentenceQuestionVersion
            {
                Id = entry.Id,
                QuestionId = entry.Id,
                VersionNumber = 1,
                Prompt = entry.Prompt,
                PublicationStatus = PublicationStatus.Published,
                Explanation = entry.Explanation,
                TilesJson = JsonSerializer.Serialize(entry.Tiles),
                ResponsePartsJson = JsonSerializer.Serialize(parts),
                AcceptedOrdersJson = JsonSerializer.Serialize(new[] { entry.Order })
            };
            _ = BuildSentenceQuestionMapper.Read(row);
            b.Entity<BuildSentenceQuestionVersion>().HasData(row);
            b.Entity<BuildSentencePathStep>().HasData(new { PathVersionId = buildPath, Position = ++number, QuestionVersionId = entry.Id });
        }
        foreach (var kind in new[] { WritingTaskKind.Email, WritingTaskKind.AcademicDiscussion })
        {
            var path = Guid.Parse($"c{(int)kind}100000-0000-4000-8000-000000000002");
            var oldPath = Guid.Parse($"c{(int)kind}100000-0000-4000-8000-000000000001");
            b.Entity<WritingPathVersion>().HasData(new { Id = path, Kind = kind, Version = 2, Title = "Expanded writing practice", IsPublished = true });
            var oldSteps = b.Entity<WritingPathStep>().Metadata.GetSeedData().Where(x => (Guid)x["PathId"]! == oldPath).OrderBy(x => x["Position"]).ToArray();
            foreach (var step in oldSteps) b.Entity<WritingPathStep>().HasData(new { PathId = path, Position = (int)step["Position"]!, QuestionId = (Guid)step["QuestionId"]! });
            var position = oldSteps.Length;
            foreach (var e in bank.Where(x => x.Kind == (kind == WritingTaskKind.Email ? "email" : "academic-discussion")))
            {
                b.Entity<WritingQuestionVersion>().HasData(new WritingQuestionVersion
                {
                    Id = e.Id,
                    QuestionId = e.Id,
                    Version = 1,
                    Kind = kind,
                    PublicationStatus = PublicationStatus.Published,
                    Title = e.Title,
                    Instructions = e.Instructions,
                    Context = e.Context,
                    RequirementsJson = JsonSerializer.Serialize(e.Requirements),
                    PostsJson = JsonSerializer.Serialize(e.Posts)
                });
                b.Entity<WritingPathStep>().HasData(new { PathId = path, Position = ++position, QuestionId = e.Id });
            }
        }
        foreach (var kind in new[] { SpeakingTaskKind.ListenRepeat, SpeakingTaskKind.Interview })
        {
            var path = Guid.Parse($"e{(int)kind}100000-0000-4000-8000-000000000002");
            var oldPath = Guid.Parse($"e{(int)kind}100000-0000-4000-8000-000000000001");
            b.Entity<SpeakingPathVersion>().HasData(new { Id = path, Kind = kind, Version = 2, Title = "Expanded speaking practice", IsPublished = true });
            var oldSteps = b.Entity<SpeakingPathStep>().Metadata.GetSeedData().Where(x => (Guid)x["PathId"]! == oldPath).OrderBy(x => x["Position"]).ToArray();
            foreach (var step in oldSteps) b.Entity<SpeakingPathStep>().HasData(new { PathId = path, Position = (int)step["Position"]!, QuestionId = (Guid)step["QuestionId"]! });
            var position = oldSteps.Length;
            foreach (var e in bank.Where(x => x.Kind == (kind == SpeakingTaskKind.ListenRepeat ? "listen-repeat" : "interview")))
            {
                b.Entity<SpeakingQuestionVersion>().HasData(new SpeakingQuestionVersion
                {
                    Id = e.Id,
                    QuestionId = e.Id,
                    Version = 1,
                    Kind = kind,
                    PublicationStatus = PublicationStatus.Published,
                    Title = e.Title,
                    Instructions = e.Instructions,
                    Context = e.Context,
                    Prompt = e.Prompt,
                    AudioFile = e.AudioFile,
                    ResponseSeconds = e.ResponseSeconds,
                    ScenePosition = e.ScenePosition
                });
                b.Entity<SpeakingPathStep>().HasData(new { PathId = path, Position = ++position, QuestionId = e.Id });
            }
        }
    }
    /// <summary>Derives a repeatable identifier so authored content retains stable references.</summary>
    private static Guid StableId(string value) => new(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)).AsSpan(0, 16));
}
