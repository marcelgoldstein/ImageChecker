using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace ImageChecker.Controls;

public partial class MultiSelectDataGrid : DataGrid
{
    public MultiSelectDataGrid()
    {
        SelectionChanged += MultiSelectDataGrid_SelectionChanged;
    }

    void MultiSelectDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SelectedItemsList = SelectedItems;
    }

    public IList SelectedItemsList
    {
        get => (IList)GetValue(SelectedItemsListProperty);
        set => SetValue(SelectedItemsListProperty, value);
    }

    public static readonly DependencyProperty SelectedItemsListProperty = DependencyProperty.Register(nameof(SelectedItemsList), typeof(IList), typeof(MultiSelectDataGrid), new PropertyMetadata(null));
}
