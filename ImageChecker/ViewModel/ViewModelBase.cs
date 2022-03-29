using ImageChecker.Helper;
using System.Windows.Shell;

namespace ImageChecker.ViewModel;

public class ViewModelBase : ABindableBase
{
    #region Window
    private string _windowTitle;
    public string WindowTitle
    {
        get { return _windowTitle; }
        set { SetProperty(ref _windowTitle, value); }
    }

    private string _windowIcon;
    public string WindowIcon
    {
        get { return _windowIcon; }
        set { SetProperty(ref _windowIcon, value); }
    }

    private TaskbarItemInfo _windowTaskbarInfo;
    public TaskbarItemInfo WindowTaskbarInfo
    {
        get { if (_windowTaskbarInfo == null) _windowTaskbarInfo = new TaskbarItemInfo(); return _windowTaskbarInfo; }
    }
    #endregion Window
}
