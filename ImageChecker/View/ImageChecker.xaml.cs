using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace ImageChecker.View;

public partial class ImageCheckerView : UserControl
{
    #region Contructor
		public ImageCheckerView()
    {
        InitializeComponent();
    }
    #endregion Contructor

    #region EventHandler
    private void btnMenu_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            btn.ContextMenu.PlacementTarget = btn;
            btn.ContextMenu.Placement = PlacementMode.Bottom;
            btn.ContextMenu.IsOpen = true;
        }
    }
    #endregion EventHandler
}
