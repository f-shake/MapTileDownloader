using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MapTileDownloader.UI.Messages;
using NetTopologySuite.Geometries;

namespace MapTileDownloader.UI.ViewModels;

public partial class DownloadViewModel : ViewModelBase
{
    public DownloadViewModel()
    {
        Coordinates = Configs.Instance.DownloadArea;
        //TODO：还应在地图上显示
    }
    [ObservableProperty]
    private string selectionMessage;

    [ObservableProperty]
    private bool isEnabled = true;

    [ObservableProperty]
    private Coordinate[] coordinates;

    partial void OnCoordinatesChanged(Coordinate[] value)
    {
        if(value==null)
        {
            SelectionMessage = "还未选择区域";
        }
        else
        {
            SelectionMessage=$"已选择区域（{value.Length}边形）";
        }
        Configs.Instance.DownloadArea = value;
        Configs.Instance.Save();
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task SelectOnMapAsync(CancellationToken cancellationToken)
    {
        IsEnabled = false;
        var m = SendMessage(new SelectOnMapMessage(cancellationToken));
        try
        {
            Coordinates = await m.Task;
        }
        catch (OperationCanceledException)
        {

        }
        finally
        {
            IsEnabled = true;
        }
    }
}