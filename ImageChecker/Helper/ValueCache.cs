using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageChecker.Helper;

public class ValueCache<T>
{
    #region Fields
    private CancellationTokenSource _cts;
    #endregion Fields

    #region Properties
    /// <summary>
    /// Gibt an, ob ein Wert enthalten ist.
    /// </summary>
    public bool HasValue { get; private set; }
    /// <summary>
    /// Ausgewerteter Wert.
    /// </summary>
    public T Value { get; private set; }

    /// <summary>
    /// Wenn gesetzt, wird nach dem Setzen des Wertes nach dieser Zeitspanne der Wert abgelöscht.
    /// </summary>
    public TimeSpan? CacheAutoClearTimespan { get; set; }

    /// <summary>
    /// Wenn gesetzt, wird der CacheClearTimer zurückgesetzt wenn der Wert erfolgreich ausgelesen wird.
    /// </summary>
    public bool RestartCacheAutoClearTimerOnValueAccess { get; set; } = false;
    #endregion Properties

    #region Methods
    /// <summary>
    /// Setzt den Wert und gibt diesen zurück.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public T SetValue(T value)
    {
        HasValue = true;
        Value = value;

        RestartElapseTimer();

        return value;
    }

    private void RestartElapseTimer()
    {
        CancelElapseTimer();
        StartElapseTimer();
    }

    private void CancelElapseTimer()
    {
        _cts?.Cancel();
    }

    private void StartElapseTimer()
    {
        if (CacheAutoClearTimespan is TimeSpan elapseTimespan)
        {
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            _ = Task.Run(async () =>
            {
                await Task.Delay(elapseTimespan);

                if (token.IsCancellationRequested == false)
                    ClearValue();
            });
        }
    }

    /// <summary>
    /// Entfernt den Wert und das Ausgewertet-Flag.
    /// </summary>
    public void ClearValue()
    {
        HasValue = false;

        if (Value is IDisposable disposable)
            disposable.Dispose();

        Value = default;
    }

    /// <summary>
    /// Attemts to retrieve the evaluated Value.
    /// </summary>
    /// <param name="value"></param>
    /// <returns>'True' when a Value is already evaluated, otherwise 'false'</returns>
    public bool TryGetValue(out T value)
    {
        if (HasValue)
        {
            if (RestartCacheAutoClearTimerOnValueAccess)
                RestartElapseTimer();

            value = Value;
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }
    #endregion Methods
}
