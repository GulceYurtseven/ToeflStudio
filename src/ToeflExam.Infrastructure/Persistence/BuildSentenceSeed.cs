using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ToeflExam.Domain.Content;
using ToeflExam.Infrastructure.Persistence.Entities;

namespace ToeflExam.Infrastructure.Persistence;

// Original starter exercises, not ETS questions. Version 1 is frozen; expand through a new path version.
internal static class BuildSentenceSeed
{
    internal static readonly Guid PathId = Guid.Parse("b5100000-0000-4000-8000-000000000001");

    /// <summary>Defines the model mapping, constraints or deterministic seed data for this content.</summary>
    public static void Configure(ModelBuilder builder)
    {
        builder.Entity<BuildSentencePathVersion>().HasData(new
        { Id = PathId, Version = 1, Title = "Build a Sentence — Foundation", IsPublished = true });

        Add(1, "What does Maya do before class?", "Before class, Maya ", ".",
            ["visits", "the", "library"], "visit",
            "Maya is third-person singular, so the present-simple verb is 'visits'. The object 'the library' follows the verb; the time phrase comes first.");
        Add(2, "Ask about the location of the science lab.", "Where ", "?",
            ["is", "the science lab"], "does",
            "Use be for a location question: Where + is + subject. Does alone cannot express location.");
        Add(3, "What happened before the lecture?", "Before the lecture began, the students ", ".",
            ["had", "finished", "their homework"], "has",
            "Past perfect describes an action completed before another past event: had + past participle. Has does not agree with the plural subject or express this past relationship.");
        Add(4, "Ask politely about the deadline.", "Could you ", "?",
            ["tell me", "when", "the assignment is due"], "is the assignment due",
            "Embedded questions keep statement order: 'when the assignment is due'. 'Could you tell me' makes the question polite.");
        Add(5, "Describe the book you borrowed.", "The book ", " from the library was very useful.",
            ["that", "I", "borrowed"], "what",
            "'That I borrowed from the library' is a relative clause describing the book. Use that, not what, after this noun.");
        Add(6, "What will you do if the weather improves?", "If ", ".",
            ["the weather improves,", "we will have", "the meeting outside"], "the weather will improve,",
            "A first conditional uses present simple in the if-clause and will + base verb in the result.");
        Add(7, "Compare the two courses.", "This course ", ".",
            ["is", "more challenging than", "the introductory course"], "are",
            "The singular subject 'this course' takes is. The comparative 'more challenging' is followed by than.");
        Add(8, "Explain the student's decision.", "She decided ", ".",
            ["to", "apply for", "the scholarship"], "applying",
            "Decide takes a to-infinitive. The correct phrase is 'apply for a scholarship'.");
        Add(9, "Describe the new schedule.", "Yesterday, the schedule ", ".",
            ["was", "revised by", "the coordinator"], "were",
            "The singular past passive is 'was revised'. By identifies the person responsible; yesterday gives the time.");
        Add(10, "Say how long the researchers have worked here.", "Since 2021, the researchers ", ".",
            ["have been", "working at", "this university"], "has been",
            "Present perfect continuous describes an action continuing from the past. The plural subject researchers takes have been; since introduces the starting year.");

        void Add(int number, string prompt, string prefix, string suffix, string[] words, string distractor, string explanation)
        {
            var id = Guid.Parse($"b5200000-0000-4000-8000-{number:D12}");
            // Opaque local IDs and mixed display order do not encode the answer sequence.
            int[][] tileIds = [[42, 87, 16, 95], [68, 14, 52, 39], [93, 27, 61, 45],
                [31, 82, 47, 19], [76, 24, 59, 83], [18, 64, 91, 37],
                [54, 29, 73, 11], [86, 41, 22, 67], [35, 92, 48, 17], [63, 26, 81, 44]];
            var ids = tileIds[number - 1];
            var tiles = words.Select((text, index) => new BuildSentenceQuestionMapper.Tile(ids[index], text)).ToList();
            tiles.Add(new(ids[3], distractor));
            tiles = tiles.OrderBy(tile => (tile.Id * 13 + number * tile.Id) % 101).ToList();
            var orders = new[] { ids.Take(words.Length).ToArray() };
            var parts = new[] { new BuildSentenceQuestionMapper.Part(false, prefix) }
                .Concat(words.Select(_ => new BuildSentenceQuestionMapper.Part(true, null)))
                .Append(new(false, suffix)).ToArray();
            var row = new BuildSentenceQuestionVersion
            {
                Id = id, QuestionId = id, VersionNumber = 1, Prompt = prompt,
                PublicationStatus = PublicationStatus.Published, Explanation = explanation,
                TilesJson = JsonSerializer.Serialize(tiles), ResponsePartsJson = JsonSerializer.Serialize(parts),
                AcceptedOrdersJson = JsonSerializer.Serialize(orders)
            };
            _ = BuildSentenceQuestionMapper.Read(row); // Validate content before producing a migration.
            builder.Entity<BuildSentenceQuestionVersion>().HasData(new
            { row.Id, row.QuestionId, row.VersionNumber, row.Prompt, row.PublicationStatus, row.Explanation,
                row.TilesJson, row.ResponsePartsJson, row.AcceptedOrdersJson });
            builder.Entity<BuildSentencePathStep>().HasData(new { PathVersionId = PathId, Position = number, QuestionVersionId = id });
        }
    }
}
