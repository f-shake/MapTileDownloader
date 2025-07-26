using MapTileDownloader.UI.Services;
using MapTileDownloader.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace MapTileDownloader.UI.Services;

public class MainViewService : IMainViewService
{
    private MainView mainView;

    public void Attach(MainView mainView)
    {
        this.mainView = mainView;
    }

    public void SetLoadingVisible(bool isVisible)
    {
        mainView.SetLoadingVisible(isVisible);
    }

    public void SetTabSelectable(bool isEnabled)
    {
        mainView.SetTabSelectable(isEnabled);
    }
}