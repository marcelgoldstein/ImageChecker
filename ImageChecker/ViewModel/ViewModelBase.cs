using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Shell;

namespace ImageChecker.ViewModel;

public class ViewModelBase : INotifyPropertyChanged, INotifyPropertyChanging
{
    #region INotifyPropertyChanged / Changing
    public void RaisePropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    public event PropertyChangedEventHandler PropertyChanged;

    public void RaisePropertyChanging(string propertyName)
    {
        PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
    }
    public event PropertyChangingEventHandler PropertyChanging;

    protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            RaisePropertyChanging(propertyName);
            field = value;
            RaisePropertyChanged(propertyName);
        }
    }
    #endregion INotifyPropertyChanged / Changing

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
