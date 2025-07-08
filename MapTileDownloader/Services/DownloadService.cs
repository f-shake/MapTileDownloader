using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MapTileDownloader.Models;
using Microsoft.Data.Sqlite;

namespace MapTileDownloader.Services;

public class DownloadService
{
    private readonly TileDataSource tileDataSource;
    private readonly HttpClient httpClient;
    private readonly SqliteConnection mbtilesConnection;
    private readonly SemaphoreSlim semaphore;
    private readonly HashSet<string> existingTiles;

    public DownloadService(TileDataSource tileDataSource, string mbtilesPath, int maxConcurrency = 8)
    {
        this.tileDataSource = tileDataSource ?? throw new ArgumentNullException(nameof(tileDataSource));
        this.semaphore = new SemaphoreSlim(maxConcurrency);
        this.existingTiles = new HashSet<string>();
        new FileInfo(mbtilesPath).Directory.Create();

        // 初始化 HttpClient
        httpClient = new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.All
        });

        if (!string.IsNullOrWhiteSpace(tileDataSource.Referer))
            httpClient.DefaultRequestHeaders.Referrer = new Uri(tileDataSource.Referer);

        if (!string.IsNullOrWhiteSpace(tileDataSource.UserAgent))
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(tileDataSource.UserAgent);

        if (!string.IsNullOrWhiteSpace(tileDataSource.Host))
            httpClient.DefaultRequestHeaders.Host = tileDataSource.Host;

        if (!string.IsNullOrWhiteSpace(tileDataSource.Origin))
            httpClient.DefaultRequestHeaders.Add("Origin", tileDataSource.Origin);

        // 初始化 SQLite 连接
        bool isNewFile = !File.Exists(mbtilesPath);
        mbtilesConnection = new SqliteConnection($"Data Source={mbtilesPath}");
        mbtilesConnection.Open();

        if (isNewFile)
        {
            InitializeMBTiles();
        }

        LoadExistingTiles(); // 预加载已存在瓦片
    }

    private void InitializeMBTiles()
    {
        using var cmd = mbtilesConnection.CreateCommand();
        cmd.CommandText = """
                          CREATE TABLE tiles (zoom_level INTEGER, tile_column INTEGER, tile_row INTEGER, tile_data BLOB);
                          CREATE UNI QUE INDEX tile_index ON tiles (zoom_level, tile_column, tile_row);
                          CREATE TABLE metadata (name TEXT, value TEXT);
                          CREATE UNIQUE INDEX name ON metadata (name);
                          """;
        cmd.ExecuteNonQuery();

        InsertOrUpdateMetadata("name", tileDataSource.Name ?? "Unnamed Layer");
        InsertOrUpdateMetadata("type", "baselayer");
        InsertOrUpdateMetadata("version", "1.0");
        InsertOrUpdateMetadata("format", tileDataSource.Format.ToLowerInvariant());
        InsertOrUpdateMetadata("description", $"Tiles downloaded from {tileDataSource.Url ?? "unknown"}");
        InsertOrUpdateMetadata("minzoom", "0");
        InsertOrUpdateMetadata("maxzoom", tileDataSource.MaxLevel.ToString());
        InsertOrUpdateMetadata("bounds", "-180.0,-85.0511,180.0,85.0511"); // 全世界范围
    }

    private void InsertOrUpdateMetadata(string name, string value)
    {
        using var cmd = mbtilesConnection.CreateCommand();
        cmd.CommandText = """
                          INSERT OR REPLACE INTO metadata (name, value)
                          VALUES (@name, @value);
                          """;
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@value", value);
        cmd.ExecuteNonQuery();
    }


    private void LoadExistingTiles()
    {
        using var cmd = mbtilesConnection.CreateCommand();
        cmd.CommandText = "SELECT zoom_level, tile_column, tile_row FROM tiles";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            int z = reader.GetInt32(0);
            int x = reader.GetInt32(1);
            int y = reader.GetInt32(2);
            string key = $"{z}/{x}/{y}";
            existingTiles.Add(key);
        }
    }

    public async Task DownloadTilesAsync(IEnumerable<IDownloadingLevel> levels, CancellationToken cancellationToken)
    {
        var downloadTasks = new List<Task>();

        foreach (var level in levels)
        {
            foreach (var tile in level.Tiles)
            {
                var z = tile.TileIndex.Level;
                var x = tile.TileIndex.Col;
                var y = tile.TileIndex.Row;

                string key = $"{z}/{x}/{y}";

                if (existingTiles.Contains(key))
                {
                    tile.SetStatus(DownloadStatus.Skip, "已存在");
                    continue;
                }

                if (tile.Status != DownloadStatus.Ready)
                    continue;

                await semaphore.WaitAsync(cancellationToken);

                downloadTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        tile.SetStatus(DownloadStatus.Downloading, "下载中");
                        string url = BuildTileUrl(tile.TileIndex);
                        Debug.WriteLine($"开始下载{url}");
                        byte[] data = await HttpDownloadAsync(url, cancellationToken);

                        if (data is { Length: > 0 })
                        {
                            await SaveTileToMBTilesAsync(tile.TileIndex, data);
                            existingTiles.Add(key); // 添加到存在列表
                            tile.SetStatus(DownloadStatus.Success, "下载成功");
                        }
                        else
                        {
                            tile.SetStatus(DownloadStatus.Failed, "内容为空");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // 取消操作
                    }
                    catch (Exception ex)
                    {
                        tile.SetStatus(DownloadStatus.Failed, ex.Message);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken));
            }
        }

        await Task.WhenAll(downloadTasks);
    }

    public async Task<byte[]> HttpDownloadAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    private async Task SaveTileToMBTilesAsync(BruTile.TileIndex tileIndex, byte[] data)
    {
        int z = tileIndex.Level;
        int x = tileIndex.Col;
        int y = tileIndex.Row;

        await using var cmd = mbtilesConnection.CreateCommand();
        cmd.CommandText = """
                          INSERT OR REPLACE INTO tiles (zoom_level, tile_column, tile_row, tile_data)
                          VALUES (@z, @x, @y, @data);
                          """;
        cmd.Parameters.AddWithValue("@z", z);
        cmd.Parameters.AddWithValue("@x", x);
        cmd.Parameters.AddWithValue("@y", y);
        cmd.Parameters.AddWithValue("@data", data);
        await cmd.ExecuteNonQueryAsync();
    }

    private string BuildTileUrl(BruTile.TileIndex index)
    {
        int x = index.Col;
        int y = index.Row;
        int z = index.Level;
        string format = tileDataSource.Format.ToLowerInvariant();

        return tileDataSource.Url
            .Replace("{x}", x.ToString())
            .Replace("{y}", y.ToString())
            .Replace("{z}", z.ToString())
            .Replace("{format}", format);
    }
}