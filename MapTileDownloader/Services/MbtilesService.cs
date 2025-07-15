using System.Data;
using BruTile;
using BruTile.Predefined;
using MapTileDownloader.Models;
using Microsoft.Data.Sqlite;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using SixLabors.ImageSharp;

namespace MapTileDownloader.Services;

public class MbtilesService : IAsyncDisposable, IDisposable
{
    public const string UnknownFormat = "未知";

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
                p => new TileIndex(p.GetInt32(0), p.GetInt32(1), p.GetInt32(2)))
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
            try
            {
                await ExecuteAsync("""

                                   PRAGMA journal_mode=OFF;          -- 完全禁用日志（纯读时安全）
                                   PRAGMA temp_store=MEMORY;         -- 临时表用内存
                                   PRAGMA mmap_size=268435456;       -- 256MB 内存映射（减少I/O）
                                   PRAGMA cache_size=-64000;         -- 64MB 缓存

                               """);
            }
            catch (Exception ex)
            {

            }
        }
        else
        {
            try
            {
                await ExecuteAsync("""
                PRAGMA journal_mode=WAL;
                PRAGMA wal_autocheckpoint = 8192;
                """);
            }
            catch (Exception ex)
            {

            }
            await EnsureSchemaAsync();
            await ValidateMBTilesSchemaAsync();
        }

    }

    public async Task UpdateMetadataAsync(string name, string description, bool useTms)
    {
        var mbtilesInfo = await GetMbtilesInfoAsync(useTms: false).ConfigureAwait(false);
        await InsertOrUpdateMetadataAsync("name", name ?? "Unnamed Layer");
        await InsertOrUpdateMetadataAsync("type", "baselayer");
        await InsertOrUpdateMetadataAsync("version", "1.3");
        await InsertOrUpdateMetadataAsync("format", mbtilesInfo.Format);
        await InsertOrUpdateMetadataAsync("description", description);
        await InsertOrUpdateMetadataAsync("minzoom", mbtilesInfo.MinZoom.ToString());
        await InsertOrUpdateMetadataAsync("maxzoom", mbtilesInfo.MaxZoom.ToString());
        await InsertOrUpdateMetadataAsync("bounds", "-180.0,-85.0511,180.0,85.0511");
        await InsertOrUpdateMetadataAsync("bounds", $"{mbtilesInfo.MinLongitude:F6},{mbtilesInfo.MinLatitude:F6},{mbtilesInfo.MaxLongitude:F6},{mbtilesInfo.MaxLatitude:F6}");
        await InsertOrUpdateMetadataAsync("scheme", useTms ? "tms" : "xyz");
    }

    private async Task InsertOrUpdateMetadataAsync(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }
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


    public async Task<MbtilesInfo> GetMbtilesInfoAsync(bool useTms)
    {
        CheckConnectionOpen();

        int count = (await QueryAsync("SELECT COUNT(1) FROM tiles",
            r => r.GetInt32(0)).ConfigureAwait(false)).First();

        // 获取所有zoom级别
        var zoomLevels = await QueryAsync("SELECT DISTINCT zoom_level FROM tiles",
            r => r.GetInt32(0)).ConfigureAwait(false);

        int minZoom = zoomLevels.Any() ? zoomLevels.Min() : -1;
        int maxZoom = zoomLevels.Any() ? zoomLevels.Max() : -1;

        // 获取一个瓦片样本以确定格式和大小
        string format = UnknownFormat;
        int size = 0;
        try
        {
            var sampleTile = await QuerySingleValueAsync("SELECT tile_data FROM tiles LIMIT 1",
                r => r.GetFieldValue<byte[]>(0)).ConfigureAwait(false);

            if (sampleTile != null)
            {
                format = ImageUtility.GetImageType(sampleTile).type ?? UnknownFormat;
                var imageInfo = Image.Identify(sampleTile);
                if (imageInfo.Size.Width == imageInfo.Size.Height)
                {
                    size = imageInfo.Size.Width;
                }
            }
        }
        catch
        {
            // 忽略错误，保持默认值
        }

        // 使用BruTile计算实际bounds（基于最大zoom级别）
        Extent worldExtent = default;
        if (maxZoom >= 0)
        {
            try
            {
                // 获取最大zoom级别的瓦片范围
                var tileExtent = await QueryAsync(
                    $@"SELECT MIN(tile_column), MIN(tile_row), MAX(tile_column), MAX(tile_row) 
                   FROM tiles WHERE zoom_level = {maxZoom}",
                    r => new
                    {
                        MinX = r.GetInt32(0),
                        MinY = r.GetInt32(1),
                        MaxX = r.GetInt32(2),
                        MaxY = r.GetInt32(3)
                    }).ConfigureAwait(false);

                if (tileExtent.Any())
                {
                    var extent = tileExtent.First();
                    var s = new TileIntersectionService(useTms);
                    worldExtent = s.GetWorldExtent(maxZoom, extent.MinX, extent.MinY, extent.MaxX, extent.MaxY);
                }
            }
            catch
            {
                // 如果计算失败，使用默认bounds
            }
        }

        return new MbtilesInfo
        {
            Path = SqlitePath,
            MinZoom = minZoom,
            MaxZoom = maxZoom,
            TileSize = size,
            TileCount = count,
            Format = format,
            MinLatitude = worldExtent.MinY,
            MaxLatitude = worldExtent.MaxY,
            MinLongitude = worldExtent.MinX,
            MaxLongitude = worldExtent.MaxX
        };
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