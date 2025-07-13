using System.Data;
using BruTile;
using Microsoft.Data.Sqlite;

namespace MapTileDownloader.Services;

public class MbtilesService : IAsyncDisposable, IDisposable
{
    private readonly SemaphoreSlim connectionLock = new(1, 1);

    private readonly SqliteConnection mbtilesConnection;

    private bool disposed = false;

    public MbtilesService(string mbtilesPath, bool readOnly)
    {
        if (string.IsNullOrWhiteSpace(mbtilesPath))
        {
            throw new ArgumentException($"“{nameof(mbtilesPath)}”不能为 null 或空白。", nameof(mbtilesPath));
        }

        SqlitePath = mbtilesPath;
        ReadOnly = readOnly;
        var connectionString = $"Data Source={mbtilesPath}";
        if (readOnly)
        {
            connectionString += ";Mode=ReadOnly";
        }

        mbtilesConnection = new SqliteConnection(connectionString);
    }

    public bool ReadOnly { get; }

    public string SqlitePath { get; }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }
        disposed = true;
        try
        {
            if (!ReadOnly && mbtilesConnection.State == ConnectionState.Open)
            {
                using var cmd = mbtilesConnection.CreateCommand();
                cmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
                cmd.CommandText = "PRAGMA journal_mode=DELETE;";
                cmd.ExecuteNonQuery();
            }
            mbtilesConnection?.Dispose();
        }
        catch
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return;
        }
        disposed = true;
        try
        {
            if (!ReadOnly && mbtilesConnection.State == ConnectionState.Open)
            {
                await ExecuteAsync("PRAGMA wal_checkpoint(TRUNCATE);");
                await ExecuteAsync("PRAGMA journal_mode=DELETE;");
            }

            await mbtilesConnection.DisposeAsync().ConfigureAwait(false);
        }
        catch
        {
        }
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

    public async Task<ISet<TileIndex>> GetExistingTilesAsync()
    {
        var results = await QueryAsync(
                "SELECT  tile_column, tile_row, zoom_level FROM tiles",
                p=>new TileIndex(p.GetInt32(0), p.GetInt32(1), p.GetInt32(2)))
            .ConfigureAwait(false);

        var existingTiles = new HashSet<TileIndex>(capacity: results.Count); // 预分配容量

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
        if (mbtilesConnection.State == ConnectionState.Open)
        {
            return;
        }

        await mbtilesConnection.OpenAsync().ConfigureAwait(false);
        if (ReadOnly)
        {
            await ExecuteAsync("""

                                   PRAGMA journal_mode=OFF;          -- 完全禁用日志（纯读时安全）
                                   PRAGMA temp_store=MEMORY;         -- 临时表用内存
                                   PRAGMA mmap_size=268435456;       -- 256MB 内存映射（减少I/O）
                                   PRAGMA cache_size=-64000;         -- 64MB 缓存

                               """);
        }
        else
        {
            await ExecuteAsync("""
                PRAGMA journal_mode=WAL;
                PRAGMA wal_autocheckpoint = 8192;
                """);
        }

        await EnsureSchemaAsync();
        await ValidateMBTilesSchemaAsync();
    }

    public async Task InitializeMetadataAsync(string name, string format, string url, int minLevel = 0, int maxLevel = 19)
    {
        await InsertOrUpdateMetadataAsync("name", name ?? "Unnamed Layer");
        await InsertOrUpdateMetadataAsync("type", "baselayer");
        await InsertOrUpdateMetadataAsync("version", "1.0");
        await InsertOrUpdateMetadataAsync("format", format?.ToLowerInvariant() ?? "unknown");
        await InsertOrUpdateMetadataAsync("description", $"Tiles downloaded from {url ?? "unknown"}");
        await InsertOrUpdateMetadataAsync("minzoom", minLevel.ToString());
        await InsertOrUpdateMetadataAsync("maxzoom", maxLevel.ToString());
        await InsertOrUpdateMetadataAsync("bounds", "-180.0,-85.0511,180.0,85.0511");
        await InsertOrUpdateMetadataAsync("scheme", "xyz");
        await InsertOrUpdateMetadataAsync("tilejson", "2.0.0");
    }

    public async Task InsertOrUpdateMetadataAsync(string name, string value)
    {
        await ExecuteAsync(
            "INSERT OR REPLACE INTO metadata (name, value) VALUES (@name, @value)",
            ("@name", name),
            ("@value", value)
        ).ConfigureAwait(false);
    }

    public async Task ValidateMBTilesSchemaAsync()
    {
        var requiredTables = new[] { "tiles", "metadata" };
        var requiredTileColumns = new[] { "zoom_level", "tile_column", "tile_row", "tile_data" };
        var requiredMetadataColumns = new[] { "name", "value" };

        // 查询表是否存在
        await using var cmd = mbtilesConnection.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table';";
        var existingTables = new HashSet<string>();
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                existingTables.Add(reader.GetString(0));
            }
        }

        foreach (var table in requiredTables)
        {
            if (!existingTables.Contains(table))
            {
                throw new InvalidOperationException($"表{table}不存在");
            }
        }

        // 查询 tiles 表字段
        await ValidateTableColumnsAsync("tiles", requiredTileColumns);
        await ValidateTableColumnsAsync("metadata", requiredMetadataColumns);

        async Task ValidateTableColumnsAsync(string tableName, string[] requiredColumns)
        {
            await using var cmd = mbtilesConnection.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info({tableName});";

            var existingColumns = new HashSet<string>();
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    existingColumns.Add(reader.GetString(1));
                }
            }

            foreach (var column in requiredColumns)
            {
                if (!existingColumns.Contains(column))
                {
                    throw new InvalidOperationException($"表{tableName}的列{column}不存在");
                }
            }
        }
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

    private async Task EnsureSchemaAsync()
    {
        await ExecuteAsync("""
                           CREATE TABLE IF NOT EXISTS tiles (
                               zoom_level INTEGER,
                               tile_column INTEGER,
                               tile_row INTEGER,
                               tile_data BLOB
                           );
                           """);

        await ExecuteAsync("""
                           CREATE UNIQUE INDEX IF NOT EXISTS tile_index
                           ON tiles (zoom_level, tile_column, tile_row);
                           """);

        await ExecuteAsync("""
                           CREATE TABLE IF NOT EXISTS metadata (
                               name TEXT,
                               value TEXT
                           );
                           """);

        await ExecuteAsync("""
                           CREATE UNIQUE INDEX IF NOT EXISTS name
                           ON metadata (name);
                           """);
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
        await using var reader = await GetReaderAsync(cmd, sql, parameters);
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
        await using var reader = await GetReaderAsync(cmd, sql, parameters);
        return await reader.ReadAsync() ? mapper(reader) : default;
    }
}