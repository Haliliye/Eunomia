using Microsoft.Extensions.Options;
using Testcontainers.MongoDb;
using TodoApp.Infrastructure;
using TodoApp.Infrastructure.Persistence;
using Xunit;

namespace TodoApp.IntegrationTests.Fixtures;

/// <summary>
/// Spins up one real MongoDB container for the whole test run (not per
/// test/class — that would be needlessly slow) via Testcontainers. Requires
/// Docker to be available wherever these tests run; GitHub Actions'
/// ubuntu-latest runner has it preinstalled (see .github/workflows/ci.yml).
/// </summary>
public class MongoFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder()
        .WithImage("mongo:7")
        .Build();

    public MongoDbContext Context { get; private set; } = null!;
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        var settings = Options.Create(new MongoDbSettings
        {
            ConnectionString = ConnectionString,
            DatabaseName = "todoapp_integration_tests",
        });

        Context = new MongoDbContext(settings);

        // Exercises the real index creation logic against a real server —
        // this alone would have caught the original ExpressionNotSupportedException
        // class of bug (a mocked repository never touches real Mongo behavior).
        await MongoIndexInitializer.EnsureIndexesAsync(Context);
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}

[CollectionDefinition("Mongo collection")]
public class MongoCollectionFixture : ICollectionFixture<MongoFixture> { }
