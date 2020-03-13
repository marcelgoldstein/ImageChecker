using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ImageChecker.Behavior
{
    public class DataGridSyncSelectedCellBehavior
    {
        public static readonly DependencyProperty SyncSelectedCellProperty =
            DependencyProperty.RegisterAttached("SyncSelectedCell", typeof(bool), typeof(DataGridSyncSelectedCellBehavior), new UIPropertyMetadata(false, OnSyncSelectedCellChanged));

        public static bool GetSyncSelectedCell(UIElement obj)
        {
            return (bool)obj.GetValue(SyncSelectedCellProperty);
        }

        public static void SetSyncSelectedCell(UIElement obj, bool value)
        {
            obj.SetValue(SyncSelectedCellProperty, value);
        }

        private static void OnSyncSelectedCellChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (bool.Parse(e.NewValue.ToString()))
            {
                ((DataGrid)d).SelectionChanged += AssociatedObject_SelectionChanged;
            }
            else
            {
                ((DataGrid)d).SelectionChanged -= AssociatedObject_SelectionChanged;
            }
        }

        private static void AssociatedObject_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid)
            {
                DataGrid grid = (sender as DataGrid);
                if (grid.SelectedItem != null)
                {
                    grid.Dispatcher.Invoke(() =>
                    {
                        var selectedRow = grid.ItemContainerGenerator.ContainerFromItem(grid.SelectedItem) as DataGridRow;

                        // selectedRow can be null due to virtualization
                        if (selectedRow != null)
                        {
                            selectedRow.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                        }
                    });
                }
            }
        }
    }
}
