using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace ImageChecker.Helper
{
    public static class VisualTreeExtension
    {
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
