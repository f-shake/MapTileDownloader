using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using BruTile;
using BruTile.Wms;
using MapTileDownloader.Models;
using Exception = System.Exception;

namespace MapTileDownloader.Services;

public class TileDownloadService : IDisposable, IAsyncDisposable
{
    private readonly HttpClient httpClient;
    private readonly SemaphoreSlim semaphore;
    private readonly TileDataSource tileSource;
    private bool disposed = false;
    private ISet<TileIndex> existingTiles;
    private bool isInitialized = false;
    private MbtilesService mbtilesService;

    public TileDownloadService(TileDataSource tileDataSource, string mbtilesPath, int maxConcurrency = 8)
    {
        tileSource = tileDataSource ?? throw new ArgumentNullException(nameof(tileDataSource));
        semaphore = new SemaphoreSlim(maxConcurrency);
        mbtilesService = new MbtilesService(mbtilesPath, false);
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
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }
        disposed = true;
        httpClient?.Dispose();
        mbtilesService.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return;
        }
        disposed = true;
        httpClient?.Dispose();
        await mbtilesService.DisposeAsync();
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
                if (tile.Status is DownloadStatus.Skip or DownloadStatus.Success or DownloadStatus.Failed)
                {
                    continue;
                }

                var z = tile.TileIndex.Level;
                var x = tile.TileIndex.Col;
                var y = tile.TileIndex.Row;

                var key = new TileIndex(x, y, z);

                if (existingTiles.Contains(key))
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
                            // existingTiles.TryAdd(key, default); // 添加到存在列表
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
        await mbtilesService.UpdateMetadataAsync(tileSource.Name, $"download from {tileSource.Url}", false);
    }

    public async Task InitializeAsync()
    {
        await mbtilesService.InitializeAsync();

        existingTiles = await mbtilesService.GetExistingTilesAsync();

        isInitialized = true;
    }

    private string BuildTileUrl(TileIndex index)
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