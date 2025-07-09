using System.Data;
using Microsoft.Data.Sqlite;

namespace MapTileDownloader.Services;

public class MbtilesService : IAsyncDisposable, IDisposable
{
    private readonly SemaphoreSlim connectionLock = new(1, 1);

    private readonly SqliteConnection mbtilesConnection;

    public MbtilesService(string mbtilesPath, bool readOnly)
    {
        SqlitePath = mbtilesPath;
        ReadOnly = readOnly;
        var connectionString = $"Data Source={mbtilesPath}";
        if (readOnly)
        {
            connectionString += ";Mode=ReadOnly;Cache=Shared";
        }

        mbtilesConnection = new SqliteConnection(connectionString);
    }

    public bool ReadOnly { get; }

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
        var result = await QuerySingleValueAsync(
            "SELECT tile_data FROM tiles WHERE zoom_level = @z AND tile_column = @x AND tile_row = @y LIMIT 1",
            r => r.GetFieldValue<byte[]>(0),
            ("@z", z),
            ("@x", x),
            ("@y", y)
        ).ConfigureAwait(false);

        return result;
    }

    public async ValueTask InitializeAsync()
    {
        if(mbtilesConnection.State==ConnectionState.Open)
        {
            return;
        }

        await mbtilesConnection.OpenAsync().ConfigureAwait(false);
        if (ReadOnly)
        {
            await ExecuteAsync("""

                                   PRAGMA journal_mode=OFF;          -- 完全禁用日志（纯读时安全）
                                   PRAGMA locking_mode=NORMAL;       -- 避免独占锁（默认已足够）
                                   PRAGMA temp_store=MEMORY;         -- 临时表用内存
                                   PRAGMA mmap_size=268435456;       -- 256MB 内存映射（减少I/O）
                                   PRAGMA cache_size=-64000;         -- 64MB 缓存

                               """);
        }
    }

    public async Task InitializeMBTilesAsync(string name, string format, string url, int minLevel = 0,
        int maxLevel = 19)
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

    private Task<SqliteDataReader> GetReaderAsync(SqliteCommand cmd, string sql, (string, object)[] parameters)
    {
        cmd.CommandText = sql;

        if (parameters != null)
        {
            foreach (var (parameterName, value) in parameters)
            {
                cmd.Parameters.AddWithValue(parameterName, value);
            }
        }

        return cmd.ExecuteReaderAsync();
    }

    private async Task<List<T>> QueryAsync<T>(string sql, Func<SqliteDataReader, T> mapper,
        params (string, object)[] parameters)
    {
        CheckConnectionOpen();
        await using var cmd = mbtilesConnection.CreateCommand();
        await using var reader =await GetReaderAsync(cmd, sql, parameters);
        var results = new List<T>();

        while (await reader.ReadAsync())
        {
            results.Add(mapper(reader));
        }

        return results;
    }

    private async Task<T> QuerySingleValueAsync<T>(string sql, Func<SqliteDataReader, T> mapper,
        params (string, object)[] parameters)
    {
        CheckConnectionOpen();
        await using var cmd = mbtilesConnection.CreateCommand();
        await using var reader =await GetReaderAsync(cmd, sql, parameters);
        return await reader.ReadAsync() ? mapper(reader) : default;
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