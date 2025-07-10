using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FzLib.Avalonia.Messages;
using MapTileDownloader.Services;
using MapTileDownloader.UI.Messages;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MapTileDownloader.Models;
using MapTileDownloader.UI.Mapping;
using System.Diagnostics;

namespace MapTileDownloader.UI.ViewModels;

public partial class MapAreaSelectorViewModel : ViewModelBase
{
    [ObservableProperty]
    private Coordinate[] coordinates = Configs.Instance.Coordinates;

    [ObservableProperty]
    private bool isSelecting = false;

    [ObservableProperty]
    private string selectionMessage;

    public MapAreaSelectorViewModel()
    {
        CoordinatesChanged += (s, e) => Coordinates = Configs.Instance.Coordinates;
        OnCoordinatesChanged(Configs.Instance.Coordinates);
    }

    public static event EventHandler CoordinatesChanged;

    [RelayCommand]
    private void Clear()
    {
        Coordinates = null;
        Map.DisplayPolygon(null);
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

        var content = string.Join(Environment.NewLine, ["X,Y", .. Coordinates.Select(p => $"{p.X},{p.Y}")]);
        try
        {
            await File.WriteAllTextAsync(filePath, content);
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("保存失败", ex);
        }
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
            Map.DisplayPolygon(Coordinates);
            await ShowOkAsync("导入成功", $"已导入 {importedCoordinates.Count} 个坐标点");
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("导入错误", $"发生错误: {ex.Message}");
        }
    }

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
        Configs.Instance.Coordinates = Coordinates;
        CoordinatesChanged?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task SelectOnMapAsync(CancellationToken cancellationToken)
    {
        IsSelecting = true;
        await TryWithTabDisabledAsync(async () =>
        {
            Coordinates = await Map.DrawAsync(cancellationToken);
        }, "地图选择错误");
        IsSelecting = false;
    }
}