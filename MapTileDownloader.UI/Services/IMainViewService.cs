using MapTileDownloader.UI.Views;

namespace MapTileDownloader.UI.Services;

public interface IMainViewService
{
    public void Attach(MainView mainView);
    
    public void SetLoadingVisible(bool isVisible);

    public void SetTabSelectable(bool isEnabled);
}