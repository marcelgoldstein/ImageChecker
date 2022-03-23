using System.Windows;
using System.Windows.Controls;

namespace ImageChecker.Behavior;

public class ScrollIntoViewBehavior
{
    public static readonly DependencyProperty ScrollIntoViewProperty =
        DependencyProperty.RegisterAttached("ScrollIntoView", typeof(bool), typeof(ScrollIntoViewBehavior), new UIPropertyMetadata(false, OnScrollIntoViewChanged));

    public static bool GetScrollIntoView(UIElement obj)
    {
        return (bool)obj.GetValue(ScrollIntoViewProperty);
    }

    public static void SetScrollIntoView(UIElement obj, bool value)
    {
        obj.SetValue(ScrollIntoViewProperty, value);
    }

    private static void OnScrollIntoViewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
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
                    grid.UpdateLayout();
                    grid.ScrollIntoView(grid.SelectedItem, null);
                });
            }
        }
    }
}
