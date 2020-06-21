using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ImageChecker.Behavior
{
    /// <summary>
    /// This is an Attached Behavior and is intended for use with
    /// XAML objects to enable binding a drag and drop event to
    /// an ICommand.
    /// </summary>
    public static class WindowSizeBehavior
    {
        #region The dependecy Property
        /// <summary>
        /// The Dependency property. To allow for Binding, a dependency
        /// property must be used.
        /// </summary>
        private static readonly DependencyProperty _windowSizeActivatedProperty =
                    DependencyProperty.RegisterAttached
                    (
                        "WindowSizeActivated",
                        typeof(bool),
                        typeof(WindowSizeBehavior),
                        new PropertyMetadata(WindowSizeActivatedPropertyChangedCallBack)
                    );
        #endregion

        #region The getter and setter
        public static void SetWindowSizeActivated(this Window window, bool value)
        {
            window.SetValue(_windowSizeActivatedProperty, value);
        }

        public static bool GetWindowSizeActivated(Window window)
        {
            return (bool)window.GetValue(_windowSizeActivatedProperty);
        }
        #endregion

        #region The PropertyChangedCallBack method
        private static void WindowSizeActivatedPropertyChangedCallBack(
            DependencyObject inDependencyObject, DependencyPropertyChangedEventArgs inEventArgs)
        {
            if (!(inDependencyObject is Window window)) return;

            window.SizeChanged += Window_SizeChanged;
        }
        #endregion

        static void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var window = (Window)sender;
            var children = window.FindVisualChildren<UserControl>();
            if (children != null && children.Any())
            {
                UserControl uc = children.First() as UserControl;

                window.MinWidth = window.ActualWidth - uc.ActualWidth + uc.MinWidth;
                window.MaxWidth = window.ActualWidth - uc.ActualWidth + uc.MaxWidth;

                window.MinHeight = window.ActualHeight - uc.ActualHeight + uc.MinHeight;
                window.MaxHeight = window.ActualHeight - uc.ActualHeight + uc.MaxHeight;

                window.SizeChanged -= Window_SizeChanged;
            }
        }

        public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject parent) where T : DependencyObject
        {
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                switch (child)
                {
                    case T c:
                        yield return c;
                        break;
                    default:
                        {
                            foreach (var other in FindVisualChildren<T>(child))
                                yield return other;
                            break;
                        }
                }
            }
        }
    }
}