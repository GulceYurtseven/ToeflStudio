using System.Text.Json;
using ToeflExam.Application.Listening;
using ToeflExam.Domain.Listening;

namespace ToeflExam.Application.Tests;

public sealed class ListeningContentTests
{
    private static ListeningQuestion Question => new(1, "Private stimulus wording", [new(1, "Monday"), new(2, "Tuesday"), new(3, "Wednesday"), new(4, "Thursday")], 2, "Private explanation.");
    private static ListeningActivity Activity(ListeningTaskKind kind = ListeningTaskKind.ChooseResponse, int words = 12)
        => new(Guid.NewGuid(), 1, kind, "A campus exchange", string.Join(" ", Enumerable.Repeat("word", words)),
            [new("Speaker", ListeningAccent.British)], [Question]);

    [Fact]
    public void Draft_cannot_leak_and_published_projection_hides_stimulus_key_and_explanation()
    {
        var activity = Activity();
        Assert.Throws<InvalidOperationException>(() => ListeningActivityDto.FromPublished(activity));
        Assert.Throws<InvalidOperationException>(() => activity.Publish(new("/media/listening/demo.mp3", 1000), true, false));
        Assert.False(activity.IsPublished);
        activity.Publish(new("/media/listening/demo.mp3", 1000), true, true);
        var dto = ListeningActivityDto.FromPublished(activity);
        var json = JsonSerializer.Serialize(dto);
        Assert.DoesNotContain("Transcript", json); Assert.DoesNotContain("CorrectOption", json);
        Assert.DoesNotContain("Private stimulus", json); Assert.DoesNotContain("Private explanation", json);
        Assert.Equal("Choose the best response.", dto.Questions[0].Prompt);
        Assert.Equal(4, dto.Questions[0].Options.Length);
        Assert.Throws<InvalidOperationException>(() => activity.Publish(new("/media/listening/other.mp3", 2000), true, true));
    }

    [Theory]
    [InlineData(174, false)] [InlineData(175, true)] [InlineData(250, true)] [InlineData(251, false)]
    public void Academic_talk_enforces_spoken_word_range(int words, bool valid)
    {
        if (valid) Assert.NotNull(Activity(ListeningTaskKind.AcademicTalk, words));
        else Assert.Throws<ArgumentException>(() => Activity(ListeningTaskKind.AcademicTalk, words));
    }

    [Theory]
    [InlineData("https://untrusted.example/audio.mp3")]
    [InlineData("/media/listening/../private.wav")]
    [InlineData("/media/listening/test.html")]
    public void Publication_rejects_remote_or_unsafe_audio_paths(string path)
        => Assert.Throws<ArgumentException>(() => Activity().Publish(new(path, 1000), true, true));

    [Fact]
    public void Questions_have_four_options_binary_scoring_and_no_mutable_external_collections()
    {
        Assert.Equal(1, Question.Score(2)); Assert.Equal(0, Question.Score(1)); Assert.Equal(0, Question.Score(null));
        Assert.Throws<ArgumentException>(() => Question.Score(99));
        Assert.Throws<ArgumentException>(() => new ListeningQuestion(1, "Prompt", [new(1, "A")], 1, "Why"));
        Assert.Throws<ArgumentException>(() => Activity(ListeningTaskKind.Conversation));
        var speakers = new[] { new ListeningSpeaker("A", ListeningAccent.Australian), new ListeningSpeaker("B", ListeningAccent.NorthAmerican) };
        var questions = new[] { Question };
        var activity = new ListeningActivity(Guid.NewGuid(), 1, ListeningTaskKind.Conversation, "Conversation", "Spoken words", speakers, questions);
        speakers[0] = new("Changed", ListeningAccent.British); questions[0] = new(2, "Other", Question.Options, 1, "Why");
        Assert.Equal("A", activity.Speakers[0].Name); Assert.Equal(1, activity.Questions[0].Id);
    }
}
