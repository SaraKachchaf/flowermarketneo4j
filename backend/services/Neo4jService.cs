using Neo4j.Driver;

public class Neo4jService : IAsyncDisposable
{
    private readonly IDriver _driver;
    private readonly string _database;

    public Neo4jService(IConfiguration configuration)
    {
        _driver = GraphDatabase.Driver(
            configuration["Neo4j:Uri"],
            AuthTokens.Basic(
                configuration["Neo4j:User"],
                configuration["Neo4j:Password"]
            )
        );
        _database = configuration["Neo4j:Database"] ?? "neo4j";
    }

    public async Task<List<Dictionary<string, object>>> RunQueryAsync(string cypher, object? parameters = null)
    {
        var session = _driver.AsyncSession(o => o.WithDatabase(_database));
        var results = new List<Dictionary<string, object>>();

        try
        {
            var cursor = await session.RunAsync(cypher, parameters);

            await foreach (var record in cursor)
            {
                results.Add(record.Values.ToDictionary(
                    key => key.Key,
                    value => value.Value
                ));
            }

            return results;
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _driver.DisposeAsync();
    }
}
