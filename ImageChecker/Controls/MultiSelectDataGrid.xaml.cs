using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace ImageChecker.Controls
{
    public partial class MultiSelectDataGrid : DataGrid
    {

        public MultiSelectDataGrid()
        {
            this.SelectionChanged += MultiSelectDataGrid_SelectionChanged;
        }

        void MultiSelectDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.SelectedItemsList = this.SelectedItems;
        }
        #region SelectedItemsList

        public IList SelectedItemsList
        {
            get { return (IList)GetValue(SelectedItemsListProperty); }
            set { SetValue(SelectedItemsListProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemsListProperty =
                DependencyProperty.Register("SelectedItemsList", typeof(IList), typeof(MultiSelectDataGrid), new PropertyMetadata(null));

        #endregion
    }
}
