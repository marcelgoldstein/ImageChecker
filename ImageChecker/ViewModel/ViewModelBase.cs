using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ImageChecker.ViewModel;

public class ViewModelBase : INotifyPropertyChanged, INotifyPropertyChanging
{
    #region INotifyPropertyChanged / Changing
    public void RaisePropertyChanged(string propertyName)
    {
        if (PropertyChanged != null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public event PropertyChangedEventHandler PropertyChanged;

    public void RaisePropertyChanging(string propertyName)
    {
        if (PropertyChanging != null)
        {
            PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
        }
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
    #endregion

    
}
