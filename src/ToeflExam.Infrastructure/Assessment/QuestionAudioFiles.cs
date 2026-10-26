using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ToeflExam.Infrastructure.Assessment;

/// <summary>Resolves pre-generated Google voices without exposing cloud credentials or synthesizing per student.</summary>
public sealed class QuestionAudioFiles(IHostEnvironment environment, IConfiguration configuration)
{
    /// <summary>Prefers reviewed neural recordings; production can disable the legacy audio fallback.</summary>
    public string Resolve(string collection, string name)
    {
        if (collection is not ("Bank" or "Speaking") ||
            !System.Text.RegularExpressions.Regex.IsMatch(name, @"^(?:[12]-[1-7]-v[1-9][0-9]*|[a-f0-9]{32})\.wav$"))
            throw new InvalidOperationException("Invalid question audio path.");
        var google = Path.Combine(environment.ContentRootPath, "Content", "Google", collection, name);
        if (File.Exists(google) || configuration.GetValue<bool>("QuestionAudio:RequireGoogle")) return google;
        return Path.Combine(environment.ContentRootPath, "Content", collection, name);
    }
}
