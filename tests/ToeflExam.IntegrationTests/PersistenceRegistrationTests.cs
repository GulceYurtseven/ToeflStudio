using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ToeflExam.Infrastructure;
using ToeflExam.Infrastructure.Persistence;

namespace ToeflExam.IntegrationTests;

// Registration tests intentionally do not open a SQL connection.
public sealed class PersistenceRegistrationTests
{
    [Fact]
    public void Context_is_scoped_and_uses_sql_server()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:ToeflExam"] = "Server=localhost;Database=RegistrationTest;Integrated Security=True"
        }).Build();
        var services = new ServiceCollection();
        services.AddPersistence(configuration);
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        var first = firstScope.ServiceProvider.GetRequiredService<ToeflExamDbContext>();
        Assert.Same(first, firstScope.ServiceProvider.GetRequiredService<ToeflExamDbContext>());
        Assert.NotSame(first, secondScope.ServiceProvider.GetRequiredService<ToeflExamDbContext>());
        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", first.Database.ProviderName);
        Assert.Equal("RegistrationTest", first.Database.GetDbConnection().Database);
    }

    [Fact]
    public void Missing_connection_string_fails_when_context_is_requested()
    {
        var services = new ServiceCollection();
        services.AddPersistence(new ConfigurationBuilder().Build());
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var error = Assert.Throws<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<ToeflExamDbContext>());
        Assert.Contains("ConnectionStrings:ToeflExam", error.Message);
    }
}
