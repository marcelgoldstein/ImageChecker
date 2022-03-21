using ImageChecker.Properties;
using ImageChecker.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ImageChecker.Factory;

public static class WindowService
{
    private static ObservableCollection<Window> _openWindows;
    public static ObservableCollection<Window> OpenWindows
    {
        get
        {
            if (_openWindows == null)
            {
                _openWindows = new ObservableCollection<Window>();
                _openWindows.CollectionChanged += OpenWindows_CollectionChanged;
            }
            return _openWindows;
        }
    }
    static void OpenWindows_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        ObservableCollection<Window> oc = sender as ObservableCollection<Window>;

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                (e.NewItems[0] as Window).Closed += (sender2, args) => { oc.Remove(sender2 as Window); };
                break;
            case NotifyCollectionChangedAction.Move:
                break;
            case NotifyCollectionChangedAction.Remove:
                break;
            case NotifyCollectionChangedAction.Replace:
                break;
            case NotifyCollectionChangedAction.Reset:
                break;
            default:
                break;
        }
    }

    public async static Task<Window> OpenWindow(ViewModelBase vm, bool modal, Action<object, CancelEventArgs> onClosing, Action<object, EventArgs> onClosed)
    {
        MainWindow w = new MainWindow
        {
            Content = vm
        };

        #region Sizing
        var setting = GetSettingFor(vm);

        if (setting != null)
        {
            w.Width = double.Parse(setting[1]);
            w.Height = double.Parse(setting[2]);
            w.Left = double.Parse(setting[3]);
            w.Top = double.Parse(setting[4]);
            w.WindowState = bool.Parse(setting[5]) ? WindowState.Maximized : WindowState.Normal;
        }

        w.Closing += W_Closing;
        w.Closed += W_Closed;
        #endregion

        #region Closing / Closed
        if (onClosing != null)
        {
            w.Closing += (sender, args) =>
            {
                onClosing.Invoke(sender, args);
            };
        }

        if (onClosed != null)
        {
            w.Closed += (sender, args) =>
            {
                onClosed.Invoke(sender, args);
            };
        }
        #endregion

        OpenWindows.Add(w);

        if (modal)
        {
            // Show ModalWindow without blocking code-execution
            await w.ShowDialogAsync(new CancellationToken());
        }
        else
        {
            w.Show();
        }

        return w;
    }

    #region Async Modal Window -> ShowDialogAsync / CloseDialogAsync
    // show a modal dialog asynchronously
    private static async Task ShowDialogAsync(this Window window, CancellationToken token)
    {
        var tcs = new TaskCompletionSource<bool>();
        using (token.Register(() => tcs.TrySetCanceled(), useSynchronizationContext: true))
        {
            void loadedHandler(object s, RoutedEventArgs e) =>
                tcs.TrySetResult(true);

            window.Loaded += loadedHandler;
            try
            {
                // show the dialog asynchronously 
                // (presumably on the next iteration of the message loop)
                SynchronizationContext.Current.Post((_) =>
                    window.ShowDialog(), null);
                await tcs.Task;
            }
            finally
            {
                window.Loaded -= loadedHandler;
            }
        }
    }

    // async wait for a dialog to get closed
    private static async Task CloseDialogAsync(this Window window, CancellationToken token)
    {
        var tcs = new TaskCompletionSource<bool>();
        using (token.Register(() => tcs.TrySetCanceled(), useSynchronizationContext: true))
        {
            void closedHandler(object s, EventArgs e) =>
                tcs.TrySetResult(true);

            window.Closed += closedHandler;
            try
            {
                await tcs.Task;
            }
            finally
            {
                window.Closed -= closedHandler;
            }
        }
    }

    public async static Task CloseWindowsAsync(Func<Window, bool> predicate)
    {
        var targetWindows = _openWindows.Where(predicate);

        foreach (Window w in targetWindows.ToList())
        {
            await w.CloseDialogAsync(new CancellationToken());
        }
    }
    #endregion



    public static void CloseWindows(Func<Window, bool> predicate)
    {
        var targetWindows = _openWindows.Where(predicate);

        foreach (Window w in targetWindows.ToList())
        {
            w.Close();
        }
    }

    #region Sizing
    private static void W_Closed(object sender, EventArgs e)
    {
        Window w = sender as Window;
        w.Closing -= W_Closing;
        w.Closed -= W_Closed;
    }

    private static void W_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        Window w = sender as Window;

        if (w.Content is ViewModelBase)
        {
            var setting = GetSettingFor(w.Content as ViewModelBase);

            if (setting != null)
            { // setzen
                if (w.WindowState == WindowState.Normal)
                {
                    setting[1] = w.ActualWidth.ToString();
                    setting[2] = w.ActualHeight.ToString();
                    setting[3] = w.Left.ToString();
                    setting[4] = w.Top.ToString();
                    setting[5] = false.ToString();
                }
                else if (w.WindowState == WindowState.Maximized)
                {
                    w.WindowState = WindowState.Normal;

                    setting[1] = w.ActualWidth.ToString();
                    setting[2] = w.ActualHeight.ToString();
                    setting[3] = w.Left.ToString();
                    setting[4] = w.Top.ToString();
                    setting[5] = true.ToString();
                }
            }
            else
            { // erstellen und dann setzen
                setting = new List<string>();

                if (w.WindowState == WindowState.Normal)
                {
                    setting.Add((w.Content as ViewModelBase).GetType().ToString().Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries).Last());
                    setting.Add(w.ActualWidth.ToString());
                    setting.Add(w.ActualHeight.ToString());
                    setting.Add(w.Left.ToString());
                    setting.Add(w.Top.ToString());
                    setting.Add(false.ToString());
                }
                else if (w.WindowState == WindowState.Maximized)
                {
                    w.WindowState = WindowState.Normal;

                    setting.Add((w.Content as ViewModelBase).GetType().ToString().Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries).Last());
                    setting.Add(w.ActualWidth.ToString());
                    setting.Add(w.ActualHeight.ToString());
                    setting.Add(w.Left.ToString());
                    setting.Add(w.Top.ToString());
                    setting.Add(true.ToString());
                }
            }

            SetSettingFor(w.Content as ViewModelBase, setting);

            Settings.Default.Save();
        }
    }

    private static string SerializeWindowSetting(List<string> input)
    {
        var output = string.Join(";", input);

        return output;
    }

    private static List<string> DeSerializeWindowSetting(string input)
    {
        var output = input.Split(new string[] { ";" }, StringSplitOptions.None).ToList();

        return output;
    }

    private static List<string> GetSettingFor(ViewModelBase vm)
    {
        string vmName = vm.GetType().ToString().Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries).Last();

        var windowSettings = Settings.Default.WindowSizes;

        if (windowSettings != null)
        {
            var settings = windowSettings.OfType<string>();

            var setting = settings.SingleOrDefault(a => DeSerializeWindowSetting(a)[0] == vmName);

            if (setting != null)
            { // auslesen wenn vorhanden
                return DeSerializeWindowSetting(setting);
            }
            else
            { // keine werte angeben, aber setting hinzufügen, damit beim speichern die werte gesetzt werden können
                return null;
            }
        }
        else
        {
            throw new NotImplementedException("WindowSizes - Setting not available!");
        }
    }

    private static void ResetSettings()
    {
        Settings.Default.Reset();
    }

    public static void CheckSettingsUpgrade()
    {
        foreach (var setting in Settings.Default.WindowSizes)
        {
            var list = DeSerializeWindowSetting(setting);

            for (int i = 1; i < 6; i++)
            {
                if (list.Count < i)
                {
                    switch (i)
                    {
                        case 6: // the 6th item may not exist
                            list.Add(false.ToString()); // default value
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }

    private static void SetSettingFor(ViewModelBase vm, List<string> target)
    {
        string vmName = vm.GetType().ToString().Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries).Last();

        var windowSettings = Settings.Default.WindowSizes;

        if (windowSettings != null)
        {
            var settings = windowSettings.OfType<string>();

            var setting = settings.SingleOrDefault(a => DeSerializeWindowSetting(a)[0] == vmName);

            if (setting != null)
            { // auslesen wenn vorhanden
                windowSettings.Remove(setting);
                setting = SerializeWindowSetting(target);
                windowSettings.Add(setting);
            }
            else
            { // keine werte angeben, aber setting hinzufügen, damit beim speichern die werte gesetzt werden können
                setting = SerializeWindowSetting(target);

                windowSettings.Add(setting);
            }
        }
        else
        {
            throw new NotImplementedException("WindowSizes - Setting not available!");
        }
    }
    #endregion
}
