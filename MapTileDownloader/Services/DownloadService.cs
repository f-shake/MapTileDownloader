using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MapTileDownloader.Models;

namespace MapTileDownloader.Services;
public class DownloadService : IAsyncDisposable, IDisposable
{
    private readonly HttpClient httpClient;
    private readonly MbtilesService mbtilesService;
    private readonly SemaphoreSlim semaphore;
    private readonly TileDataSource tileSource;
    private ConcurrentDictionary<string, byte> existingTiles;
    private bool isInitialized = false;

    public DownloadService(TileDataSource tileDataSource, string mbtilesPath, int maxConcurrency = 8)
    {
        tileSource = tileDataSource ?? throw new ArgumentNullException(nameof(tileDataSource));
        semaphore = new SemaphoreSlim(maxConcurrency);
        new FileInfo(mbtilesPath).Directory.Create();

        // 初始化 HttpClient
        httpClient = new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.All
        });

        if (!string.IsNullOrWhiteSpace(tileDataSource.Referer))
        {
            httpClient.DefaultRequestHeaders.Referrer = new Uri(tileDataSource.Referer);
        }

        if (!string.IsNullOrWhiteSpace(tileDataSource.UserAgent))
        {
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(tileDataSource.UserAgent);
        }

        if (!string.IsNullOrWhiteSpace(tileDataSource.Host))
        {
            httpClient.DefaultRequestHeaders.Host = tileDataSource.Host;
        }

        if (!string.IsNullOrWhiteSpace(tileDataSource.Origin))
        {
            httpClient.DefaultRequestHeaders.Add("Origin", tileDataSource.Origin);
        }
        mbtilesService = new MbtilesService(mbtilesPath);
    }

    public void Dispose()
    {
        mbtilesService?.Dispose();
        httpClient?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (httpClient is not null)
        {
            httpClient.Dispose();
        }
        if (mbtilesService is not null)
        {
            await mbtilesService.DisposeAsync().ConfigureAwait(false);
        }
    }

    public async Task DownloadTilesAsync(IEnumerable<IDownloadingLevel> levels, CancellationToken cancellationToken)
    {
        if (!isInitialized)
        {
            throw new InvalidOperationException("DownloadService未初始化，请先调用InitializeAsync方法。");
        }

        var downloadTasks = new List<Task>();

        foreach (var level in levels)
        {
            foreach (var tile in level.Tiles)
            {
                var z = tile.TileIndex.Level;
                var x = tile.TileIndex.Col;
                var y = tile.TileIndex.Row;

                string key = $"{z}/{x}/{y}";

                if (existingTiles.ContainsKey(key))
                {
                    tile.SetStatus(DownloadStatus.Skip, "已存在", null);
                    continue;
                }

                if (tile.Status != DownloadStatus.Ready)
                    continue;

                await semaphore.WaitAsync(cancellationToken);

                downloadTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        tile.SetStatus(DownloadStatus.Downloading, "下载中", null);
                        string url = BuildTileUrl(tile.TileIndex);
                        Debug.WriteLine($"开始下载{url}");
                        byte[] data = await HttpDownloadAsync(url, cancellationToken);

                        if (data is { Length: > 0 })
                        {
                            await mbtilesService.WriteTileAsync(tile.TileIndex.Col, tile.TileIndex.Row, tile.TileIndex.Level, data);
                            existingTiles.TryAdd(key, default); // 添加到存在列表
                            tile.SetStatus(DownloadStatus.Success, "下载成功", null);
                        }
                        else
                        {
                            tile.SetStatus(DownloadStatus.Failed, "内容为空", null);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // 取消操作
                    }
                    catch (Exception ex)
                    {
                        tile.SetStatus(DownloadStatus.Failed, ex.Message, ex.ToString());
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

    public async Task InitializeAsync()
    {
        bool isNewFile = !File.Exists(mbtilesService.SqlitePath);

        await mbtilesService.InitializeAsync();
        if (isNewFile)
        {
            await mbtilesService.InitializeMBTilesAsync(tileSource.Name, tileSource.Format, tileSource.Url, 0, tileSource.MaxLevel);
        }

        existingTiles = new ConcurrentDictionary<string, byte>((await mbtilesService.GetExistingTilesAsync())
            .Select(p => new KeyValuePair<string, byte>(p, default)));

        isInitialized = true;
    }

    private string BuildTileUrl(BruTile.TileIndex index)
    {
        int x = index.Col;
        int y = index.Row;
        int z = index.Level;
        string format = tileSource.Format.ToLowerInvariant();

        return tileSource.Url
            .Replace("{x}", x.ToString())
            .Replace("{y}", y.ToString())
            .Replace("{z}", z.ToString())
            .Replace("{format}", format);
    }

    private async Task<byte[]> HttpDownloadAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }
}