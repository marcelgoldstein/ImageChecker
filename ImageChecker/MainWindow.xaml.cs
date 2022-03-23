using ImageChecker.ViewModel;
using System.Windows;

namespace ImageChecker;

public partial class MainWindow : Window
{
    public ViewModelBase ViewModel { get { return Content as ViewModelBase; } }

    public MainWindow()
    {
        InitializeComponent();
    }
}
