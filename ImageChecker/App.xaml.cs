using ImageChecker.Factory;
using ImageChecker.ViewModel;
using System;
using System.IO;
using System.Windows;

namespace ImageChecker
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            System.Windows.FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(System.Windows.FrameworkElement),
                new System.Windows.FrameworkPropertyMetadata(
                    System.Windows.Markup.XmlLanguage.GetLanguage(System.Globalization.CultureInfo.CurrentCulture.IetfLanguageTag)));

            AppDomain.CurrentDomain.UnhandledException += (a, exception) => File.AppendAllText("errorlog.txt", string.Format("Sender: {1}    -    Type: {2}{0}", Environment.NewLine, a.ToString(), exception.ToString()));

            //CheckSettingsUpgrade();

            await WindowService.OpenWindow(new VMImageChecker(), false, null, null);
        }

        //private void CheckSettingsUpgrade()
        //{
        //    if (Settings.Default.UpgradeRequired)
        //    {
        //        Settings.Default.Upgrade(); // stellt user-values wiederher
        //        Settings.Default.UpgradeRequired = false;
                
        //        WindowService.CheckSettingsUpgrade();

        //        Settings.Default.Save();
        //    }
        //}
        
    }
}
