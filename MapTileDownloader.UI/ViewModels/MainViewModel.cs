namespace MapTileDownloader.UI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
    }

    public DataSourceViewModel DataSourceViewModel { get; } = new DataSourceViewModel();
    public DownloadViewModel DownloadViewModel { get; } = new DownloadViewModel();
    public override void Initialize()
    {
        DataSourceViewModel.Initialize();
        DownloadViewModel.Initialize();
        base.Initialize();
    }
}