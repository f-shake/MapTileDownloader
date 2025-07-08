using System.Data;
using Microsoft.Data.Sqlite;

namespace MapTileDownloader.Services;

public class MbtilesService : IAsyncDisposable, IDisposable
{
    private readonly SqliteConnection mbtilesConnection;

    private readonly SemaphoreSlim connectionLock = new(1, 1);


    public MbtilesService(string mbtilesPath, bool readOnly)
    {
        SqlitePath = mbtilesPath;
        var connectionString = $"Data Source={mbtilesPath}";
        if (readOnly)
        {
            connectionString += ";Mode=ReadOnly;Cache=Shared";
        }
        mbtilesConnection = new SqliteConnection(connectionString);
    }

    public string SqlitePath { get; }

    public void Dispose()
    {
        mbtilesConnection.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await mbtilesConnection.DisposeAsync().ConfigureAwait(false);
    }

    public async Task<int> ExecuteAsync(string sql, params (string parameterName, object value)[] parameters)
    {
        CheckConnectionOpen();
        await connectionLock.WaitAsync();
        try
        {
            await using var cmd = mbtilesConnection.CreateCommand();
            cmd.CommandText = sql;
            if (parameters != null)
            {
                foreach (var (parameterName, value) in parameters)
                {
                    cmd.Parameters.AddWithValue(parameterName, value);
                }
            }
            return await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
        finally
        {
            connectionLock.Release();
        }
    }

    public async Task<ISet<string>> GetExistingTilesAsync()
    {
        var results = await QueryAsync(
            "SELECT zoom_level, tile_column, tile_row FROM tiles",
            r => $"{r.GetInt32(0)}/{r.GetInt32(1)}/{r.GetInt32(2)}")
            .ConfigureAwait(false);

        var existingTiles = new HashSet<string>(capacity: results.Count); // 预分配容量

        foreach (var row in results)
        {
            existingTiles.Add(row);
        }

        return existingTiles;
    }

    public async Task<byte[]> GetTileAsync(int x, int y, int z)
    {
        var results = await QueryAsync(
            "SELECT tile_data FROM tiles WHERE zoom_level = @z AND tile_column = @x AND tile_row = @y LIMIT 1",
            r => r.GetFieldValue<byte[]>(0),
            ("@z", z),
            ("@x", x),
            ("@y", y)
        ).ConfigureAwait(false);

        return results.Count > 0 ? results[0] : null;
    }

    public async ValueTask InitializeAsync()
    {
        await mbtilesConnection.OpenAsync().ConfigureAwait(false);
    }

    public async Task InitializeMBTilesAsync(string name, string format, string url, int minLevel = 0, int maxLevel = 19)
    {
        await using var cmd = mbtilesConnection.CreateCommand();
        cmd.CommandText = """
                          CREATE TABLE tiles (zoom_level INTEGER, tile_column INTEGER, tile_row INTEGER, tile_data BLOB);
                          CREATE UNIQUE INDEX tile_index ON tiles (zoom_level, tile_column, tile_row);
                          CREATE TABLE metadata (name TEXT, value TEXT);
                          CREATE UNIQUE INDEX name ON metadata (name);
                          """;
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

        await InsertOrUpdateMetadataAsync("name", name ?? "Unnamed Layer");
        await InsertOrUpdateMetadataAsync("type", "baselayer");
        await InsertOrUpdateMetadataAsync("version", "1.0");
        await InsertOrUpdateMetadataAsync("format", format?.ToLowerInvariant() ?? "unknown");
        await InsertOrUpdateMetadataAsync("description", $"Tiles downloaded from {url ?? "unknown"}");
        await InsertOrUpdateMetadataAsync("minzoom", minLevel.ToString());
        await InsertOrUpdateMetadataAsync("maxzoom", maxLevel.ToString());
        await InsertOrUpdateMetadataAsync("bounds", "-180.0,-85.0511,180.0,85.0511");

    }

    public async Task InsertOrUpdateMetadataAsync(string name, string value)
    {
        await ExecuteAsync(
            "INSERT OR REPLACE INTO metadata (name, value) VALUES (@name, @value)",
            ("@name", name),
            ("@value", value)
        ).ConfigureAwait(false);
    }

    public async Task<List<T>> QueryAsync<T>(string sql, Func<SqliteDataReader, T> mapper, params (string, object)[] parameters)
    {
        CheckConnectionOpen();
        using var cmd = mbtilesConnection.CreateCommand();
        cmd.CommandText = sql;

        if (parameters != null)
        {
            foreach (var (parameterName, value) in parameters)
            {
                cmd.Parameters.AddWithValue(parameterName, value);
            }
        }

        var results = new List<T>();
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            results.Add(mapper(reader));
        }

        return results;
    }

    public async Task WriteTileAsync(int x, int y, int z, byte[] data)
    {
        await ExecuteAsync(
            "INSERT OR REPLACE INTO tiles (zoom_level, tile_column, tile_row, tile_data) VALUES (@z, @x, @y, @data)",
            ("@z", z),
            ("@x", x),
            ("@y", y),
            ("@data", data)
        ).ConfigureAwait(false);
    }

    private void CheckConnectionOpen()
    {
        if (mbtilesConnection.State != ConnectionState.Open)
        {
            throw new InvalidOperationException("MBTiles连接未打开，请先调用InitializeAsync方法。");
        }
    }
}
