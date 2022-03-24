using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;

namespace ImageChecker.Helper;

public class RelayCommand : ABindableBase, ICommand, IDisposable
{
    #region Fields
    private readonly Func<object, Task> _execute;
    private readonly Predicate<object> _canExecute;
    private readonly ValueCache<string> _enabledTooltipStore = new ValueCache<string>();
    private readonly List<EventHandler> _attachedCanExecuteEventHandlers = new List<EventHandler>();
    private bool _suppressTooltipPropertyChangedAction;
    #endregion Fields

    #region Properties
    private string _caption;
    public string Caption
    {
        get { return _caption; }
        set { SetProperty(ref _caption, value); }
    }

    private string _tooltip;
    public string Tooltip
    {
        get { return _tooltip; }
        set { SetProperty(ref _tooltip, value); }
    }

    private string _disabledTooltip;
    public string DisabledTooltip
    {
        get { return _disabledTooltip; }
        set { SetProperty(ref _disabledTooltip, value); }
    }

    private bool _useDisabledTooltip;
    public bool UseDisabledTooltip
    {
        get { return _useDisabledTooltip; }
        set { SetProperty(ref _useDisabledTooltip, value); }
    }

    private bool _isExecuting;
    public bool IsExecuting
    {
        get { return _isExecuting; }
        private set { SetProperty(ref _isExecuting, value); }
    }

    private bool _isEnabled;
    /// <summary>
    /// Hält das Ergebnis der letzten 'CanExecute'-Auswertung (bindable)
    /// </summary>
    public bool IsEnabled
    {
        get { return _isEnabled; }
        private set { SetProperty(ref _isEnabled, value); }
    }
    #endregion Properties

    #region Events
    /// <summary>
    /// Event das ausgelöst wird, wenn für diese <see cref="GSCommand"/> Instanz eine Neu-Evaluierung des CanExecute Status über Aufruf von <see cref="ICommand.CanExecute(object)/> notwendig ist.
    /// </summary>
    private event EventHandler _commandScopeCanExecuteChanged;
    #endregion Events

    #region Constructors
    public RelayCommand(Action<object> execute) : this(execute, null)
    { }

    public RelayCommand(Action<object> execute, Predicate<object> canExecute) : this(WrapActionIntoFunction(execute), canExecute)
    { }

    public RelayCommand(Func<object, Task> execute) : this(execute, null)
    { }

    public RelayCommand(Func<object, Task> execute, Predicate<object> canExecute)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }
    #endregion Constructors

    #region Methods
    private static Func<object, Task> WrapActionIntoFunction(Action<object> action)
    {
        return async (p) => { await Task.CompletedTask; action(p); };
    }

    public void RaiseCanExecuteChanged()
    {
        _commandScopeCanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Wird verwendet, um klassenintern den Tooltip zu ändern ohne interne Zusatzlogik auszulösen. Extern soll dennoch das PropertyChanged ausgelöst werden. 
    /// </summary>
    /// <param name="tooltip"></param>
    private void SetTooltipInternal(string tooltip)
    {
        try
        {
            _suppressTooltipPropertyChangedAction = true;

            Tooltip = tooltip;
        }
        finally
        {
            _suppressTooltipPropertyChangedAction = false;
        }
    }
    #endregion Methods

    #region Event-Handler
    protected override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(sender, e);

        switch (e.PropertyName)
        {
            case nameof(Tooltip):
                if (_suppressTooltipPropertyChangedAction == false)
                { // Tooltip-Änderung von außen, also weiterleiten
                    if (_enabledTooltipStore.TryGetValue(out _))
                    { // der normale Tooltip ist gerade nicht gesetzt, sondern in einem separaten Speicherort
                        _enabledTooltipStore.SetValue(Tooltip);
                    }
                }
                break;
            case nameof(DisabledTooltip):
                if (_enabledTooltipStore.TryGetValue(out _))
                { // der normale Tooltip ist gerade nicht gesetzt, sondern in einem separaten Speicherort. Entsprechend ist der DisabledTooltip der aktive, also den aktiven Tooltip mit dem neuen Wert bestücken.
                    SetTooltipInternal(DisabledTooltip);
                }
                break;
        }
    }
    #endregion Event-Handler

    #region ICommand Members
    public bool CanExecute(object parameter)
    {
        bool canExecuteResult = false;

        try
        {
            canExecuteResult = _canExecute?.Invoke(parameter) ?? true;
        }
        catch (Exception)
        { }

        IsEnabled = canExecuteResult;

        if (UseDisabledTooltip)
        {
            if (canExecuteResult == true && _enabledTooltipStore.TryGetValue(out var tt))
            {
                SetTooltipInternal(tt); // gespeicherten Tooltip wiederherstellen
                _enabledTooltipStore.ClearValue(); // gespeicherten Tooltip ablöschen
            }
            else if (canExecuteResult == false)
            {
                if (_enabledTooltipStore.TryGetValue(out _) == false)
                    _enabledTooltipStore.SetValue(Tooltip); // aktuellen Tooltip speichern 

                SetTooltipInternal(DisabledTooltip); // den DisabledTooltip als aktiven Tooltip setzen
            }
        }

        return canExecuteResult;
    }

    public event EventHandler CanExecuteChanged
    {
        add
        {
            if ((_isDisposed == false) && (_attachedCanExecuteEventHandlers.Contains(value) == false))
            {
                _attachedCanExecuteEventHandlers.Add(value);

                // Den subscriber (in der Regel ein control) mit dem <see cref="CommandManager"/> des WPF Systems verheiraten. Dadurch kommt es bei Fokus-Wechsel, control Eingaben und anderen GUI Ereignissen zu automatischen 
                // Aufrufen von <see cref="ICommand.CanExecute(object)"/> durch den subscriber. Weiterhin reagiert der subscriber damit korrekt auf <see cref="ICommand"/> Instanzen übergreifende Anforderungen des 
                // <see cref="CommandManager"/> über Aufruf von <see cref="CommandManager.InvalidateRequerySuggested()"/> seinen CanExecute Status neu zu bewerten.
                CommandManager.RequerySuggested += value;

                // Den Subscriber (control) mit einem Ereignis verheiraten das es erlaubt, ihn manuel und unabhängig vom GUI Zustand zur Neu-Evaluierung des CanExecute Status für diese <see cref="GSCommand"/> Instanz 
                // aufzufordern.
                _commandScopeCanExecuteChanged += value;
            }
        }
        remove
        {
            if ((_isDisposed == false) && _attachedCanExecuteEventHandlers.Contains(value))
            {
                CommandManager.RequerySuggested -= value;

                _commandScopeCanExecuteChanged -= value;

                _attachedCanExecuteEventHandlers.Remove(value);
            }
        }
    }

    public async void Execute(object parameter)
    {
        await ExecuteAsync(parameter);
    }

    public async Task ExecuteAsync(object parameter)
    {
        try
        {
            IsExecuting = true;

            await _execute(parameter);
        }
        finally
        {
            IsExecuting = false;
        }

        RaiseCanExecuteChanged(); // leert den ggf. aktivierten Cache des CanExecute und lässt diesen dann neu auswerten
    }
    #endregion ICommand Members

    #region IDisposable Support
    private bool _isDisposed = false;

    protected override void Dispose(bool disposing)
    {
        if (_isDisposed == false)
        {
            if (disposing)
            {
                foreach (EventHandler eventHandler in _attachedCanExecuteEventHandlers)
                {
                    CommandManager.RequerySuggested -= eventHandler;

                    _commandScopeCanExecuteChanged -= eventHandler;
                }

                _attachedCanExecuteEventHandlers.Clear();
            }

            _isDisposed = true;

            base.Dispose(disposing);
        }
    }
    #endregion IDisposable Support
}
