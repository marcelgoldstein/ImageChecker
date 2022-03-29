using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ImageChecker.Helper;

public abstract class ABindableBase : INotifyPropertyChanged, INotifyPropertyChanging, IDisposable
{
    #region INotifyPropertyChanged / Changing
    public event PropertyChangedEventHandler PropertyChanged;
    public void RaisePropertyChanged(string propertyName)
    {
        var args = new PropertyChangedEventArgs(propertyName);

        if (_isDisposed == false)
        {
            OnPropertyChanged(this, args); // virtual method
            PropertyChanged?.Invoke(this, args); // event 
        }
    }

    public event PropertyChangingEventHandler PropertyChanging;
    public void RaisePropertyChanging(string propertyName)
    {
        var args = new PropertyChangingEventArgs(propertyName);

        if (_isDisposed == false)
        {
            OnPropertyChanging(this, args); // virtual method
            PropertyChanging?.Invoke(this, args); // event 
        }
    }

    protected virtual void OnPropertyChanging(object sender, PropertyChangingEventArgs e)
    { }

    protected virtual void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    { }

    protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            RaisePropertyChanging(propertyName);
            field = value;
            RaisePropertyChanged(propertyName);
        }
    }

    protected void SetProperty<T>(Action preRaisePropertyChangedAction, ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            RaisePropertyChanging(propertyName);
            field = value;
            preRaisePropertyChangedAction.Invoke();
            RaisePropertyChanged(propertyName);
        }
    }

    protected void RaisePropertyChanged<T>(Expression<Func<T>> propertyExpression)
    {
        RaisePropertyChanged(ExtractPropertyName(propertyExpression));
    }

    public static string ExtractPropertyName<T>(Expression<Func<T>> propertyExpression)
    {
        if (propertyExpression == null)
        {
            throw new ArgumentNullException(nameof(propertyExpression));
        }
        if (propertyExpression.Body is not MemberExpression body)
        {
            throw new ArgumentException("NotMemberAccessExpression_Exception", nameof(propertyExpression));
        }
        if (body.Member is not PropertyInfo member)
        {
            throw new ArgumentException("ExpressionNotProperty_Exception", nameof(propertyExpression));
        }
        if (member.GetMethod.IsStatic)
        {
            throw new ArgumentException("StaticExpression_Exception", nameof(propertyExpression));
        }
        return body.Member.Name;
    }
    #endregion INotifyPropertyChanged / Changing

    #region IDisposable Support
    protected bool _isDisposed;

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed == false)
        {
            if (disposing)
            {
                #region Alle Event-Subscriber unsubscriben
                if (PropertyChanging != null)
                    foreach (var d in PropertyChanging.GetInvocationList())
                        PropertyChanging -= (d as PropertyChangingEventHandler);

                if (PropertyChanged != null)
                    foreach (var d in PropertyChanged.GetInvocationList())
                        PropertyChanged -= (d as PropertyChangedEventHandler);
                #endregion Alle Event-Subscriber unsubscriben
            }

            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);

        GC.SuppressFinalize(this);
    }
    #endregion IDisposable Support
}
