﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FzLib.Avalonia.Messages;
using MapTileDownloader.Helpers;
using MapTileDownloader.UI.Messages;
using NetTopologySuite.Geometries;

namespace MapTileDownloader.UI.ViewModels;

public partial class DownloadViewModel : ViewModelBase
{
    public override void Initialize()
    {
        Coordinates = Configs.Instance.DownloadArea;
        MinLevel = Configs.Instance.MinLevel;
        MaxLevel = Configs.Instance.MaxLevel;
        DownloadDir = Configs.Instance.DownloadDir ?? Path.Combine(AppContext.BaseDirectory, "tiles");
        if (Coordinates != null)
        {
            SendMessage(new DisplayPolygonOnMapMessage(Coordinates));
        }

        base.Initialize();
    }

    [ObservableProperty]
    private string selectionMessage;

    [ObservableProperty]
    private bool isEnabled = true;

    [ObservableProperty]
    private Coordinate[] coordinates;

    [ObservableProperty]
    private int minLevel;

    [ObservableProperty]
    private int maxLevel;

    [ObservableProperty]
    private string downloadDir;

    [ObservableProperty]
    private ObservableCollection<LevelTilesViewModel> levels;

    [ObservableProperty]
    private LevelTilesViewModel selectedLevel;

    [ObservableProperty]
    private bool isSelecting = false;

    partial void OnCoordinatesChanged(Coordinate[] value)
    {
        if (value == null)
        {
            SelectionMessage = "还未选择区域";
        }
        else
        {
            SelectionMessage = $"已选择区域（{value.Length}边形）";
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (IsInitialized && e.PropertyName is
                nameof(Coordinates)
                or nameof(MinLevel)
                or nameof(MaxLevel)
                or nameof(DownloadDir))
        {
            SaveConfigs();
        }
    }

    private void SaveConfigs()
    {
        Configs.Instance.DownloadArea = Coordinates;
        Configs.Instance.MinLevel = MinLevel;
        Configs.Instance.MaxLevel = MaxLevel;
        Configs.Instance.DownloadDir = DownloadDir;
        Configs.Instance.Save();
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task SelectOnMapAsync(CancellationToken cancellationToken)
    {
        IsEnabled = false;
        IsSelecting = true;
        var m = SendMessage(new SelectOnMapMessage(cancellationToken));
        try
        {
            Coordinates = await m.Task;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("地图错误", ex);
        }
        finally
        {
            IsEnabled = true;
            IsSelecting = false;
        }
    }

    [RelayCommand]
    private void Clear()
    {
        Coordinates = null;
        SendMessage(new DisplayPolygonOnMapMessage(null));
    }

    [RelayCommand]
    private Task InitializeAsync()
    {
        var tileHelper = new TileHelper(SendMessage(new GetSelectedDataSourceMessage()).DataSource);
        return TryWithLoadingAsync(Task.Run(() =>
        {
            Levels = new ObservableCollection<LevelTilesViewModel>();
            var count = tileHelper.EstimateIntersectingTileCount(Coordinates, MaxLevel);
            if (count > 1_000_000)
            {
                throw new Exception("当前设置下，需要下载的瓦片数量可能超过100万个，请缩小区域或降低最大级别");
                return;
            }

            for (int i = MinLevel; i <= MaxLevel; i++)
            {
                var tiles = tileHelper.GetIntersectingTiles(Coordinates, i);
                var levelTile = new LevelTilesViewModel(i, tiles);
                Levels.Add(levelTile);
            }
        }));
    }

    partial void OnSelectedLevelChanged(LevelTilesViewModel value)
    {
        if (value == null)
        {
            return;
        }

        SendMessage(new DisplayTilesOnMapMessage(value.Tiles));
    }

    [RelayCommand]
    private async Task ImportCoordinatesAsync()
    {
        try
        {
            var storageProvider = SendMessage(new GetStorageProviderMessage()).StorageProvider;

            // 2. 配置文件选择选项（仅允许CSV）
            var options = new FilePickerOpenOptions
            {
                Title = "选择坐标文件",
                AllowMultiple = false,
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new FilePickerFileType("CSV文件")
                    {
                        Patterns = ["*.csv"],
                        MimeTypes = ["text/csv"]
                    }
                }
            };

            // 3. 显示文件选择对话框
            var files = await storageProvider.OpenFilePickerAsync(options);
            if (files.Count == 0 || files[0]?.TryGetLocalPath() is not string filePath)
            {
                return; // 用户取消选择
            }

            // 4. 读取文件内容
            var csvContent = await File.ReadAllTextAsync(filePath);

            // 5. 解析CSV数据（简单实现，可根据需要改用CSV解析库）
            var lines = csvContent.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries);
            var importedCoordinates = new List<Coordinate>();

            for (int i = 0; i < lines.Length; i++)
            {
                // 跳过标题行（假设第一行是"X,Y"）
                if (i == 0 && lines[i].Trim().Equals("X,Y", StringComparison.OrdinalIgnoreCase))
                    continue;

                var parts = lines[i].Split(',');
                if (parts.Length == 2 &&
                    double.TryParse(parts[0], out var x) &&
                    double.TryParse(parts[1], out var y))
                {
                    importedCoordinates.Add(new Coordinate(x, y));
                }
            }

            // 6. 验证并更新数据
            if (importedCoordinates.Count == 0)
            {
                await ShowErrorAsync("导入失败", "文件中未找到有效的坐标数据");
                return;
            }

            Coordinates = importedCoordinates.ToArray();
            SendMessage(new DisplayPolygonOnMapMessage(Coordinates));
            await ShowOkAsync("导入成功", $"已导入 {importedCoordinates.Count} 个坐标点");
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("导入错误", $"发生错误: {ex.Message}");
        }
    }


    [RelayCommand]
    private async Task ExportCoordinatesAsync()
    {
        if (Coordinates is not { Length: > 0 })
        {
            await ShowErrorAsync("范围为空", "下载范围为空");
            return;
        }

        var options = new FilePickerSaveOptions
        {
            DefaultExtension = "csv", // 默认文件扩展名
            SuggestedFileName = "tiles.csv", // 可选：默认文件名
            FileTypeChoices = new List<FilePickerFileType>
            {
                new FilePickerFileType("CSV 文件")
                {
                    Patterns = ["*.csv"],
                    MimeTypes = ["text/csv"]
                }
            }
        };
        var file = await SendMessage(new GetStorageProviderMessage()).StorageProvider.SaveFilePickerAsync(options);

        var filePath = file?.TryGetLocalPath();
        if (filePath == null)
        {
            return;
        }

        var content = string.Join(Environment.NewLine, ["X,Y", ..Coordinates.Select(p => $"{p.X},{p.Y}")]);
        try
        {
            await File.WriteAllTextAsync(filePath, content);
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("保存失败", ex);
        }
    }
}