﻿using System.Windows;
using System.Windows.Input;

namespace ImageChecker.Behavior;

/// <summary>
/// This is an Attached Behavior and is intended for use with
/// XAML objects to enable binding a drag and drop event to
/// an ICommand.
/// </summary>
public static class DropBehavior
{
    #region The dependecy Property
    /// <summary>
    /// The Dependency property. To allow for Binding, a dependency
    /// property must be used.
    /// </summary>
    private static readonly DependencyProperty _previewDropCommandProperty =
                DependencyProperty.RegisterAttached
                (
                    "PreviewDropCommand",
                    typeof(ICommand),
                    typeof(DropBehavior),
                    new PropertyMetadata(PreviewDropCommandPropertyChangedCallBack)
                );
    #endregion

    #region The getter and setter
    /// <summary>
    /// The setter. This sets the value of the PreviewDropCommandProperty
    /// Dependency Property. It is expected that you use this only in XAML
    ///
    /// This appears in XAML with the "Set" stripped off.
    /// XAML usage:
    ///
    /// <Grid mvvm:DropBehavior.PreviewDropCommand="{Binding DropCommand}" />
    ///
    /// </summary>
    /// <param name="inUIElement">A UIElement object. In XAML this is automatically passed
    /// in, so you don't have to enter anything in XAML.</param>
    /// <param name="inCommand">An object that implements ICommand.</param>
    public static void SetPreviewDropCommand(this UIElement inUIElement, ICommand inCommand)
    {
        inUIElement.SetValue(_previewDropCommandProperty, inCommand);
    }

    /// <summary>
    /// Gets the PreviewDropCommand assigned to the PreviewDropCommandProperty
    /// DependencyProperty. As this is only needed by this class, it is private.
    /// </summary>
    /// <param name="inUIElement">A UIElement object.</param>
    /// <returns>An object that implements ICommand.</returns>
    public static ICommand GetPreviewDropCommand(UIElement inUIElement)
    {
        return (ICommand)inUIElement.GetValue(_previewDropCommandProperty);
    }
    #endregion

    #region The PropertyChangedCallBack method
    /// <summary>
    /// The OnCommandChanged method. This event handles the initial binding and future
    /// binding changes to the bound ICommand
    /// </summary>
    /// <param name="inDependencyObject">A DependencyObject</param>
    /// <param name="inEventArgs">A DependencyPropertyChangedEventArgs object.</param>
    private static void PreviewDropCommandPropertyChangedCallBack(
        DependencyObject inDependencyObject, DependencyPropertyChangedEventArgs inEventArgs)
    {
        if (inDependencyObject is not UIElement uiElement) return;

        uiElement.PreviewDragOver += (sender, args) =>
        {
            args.Handled = true;
        };

        uiElement.PreviewDragEnter += (sender, args) =>
        {
            var dataObject = args.Data as DataObject;

            // Check for file list
            if (dataObject.ContainsFileDropList())
                args.Effects = DragDropEffects.Copy;
            else
                args.Effects = DragDropEffects.None;
            args.Handled = true;
        };

        uiElement.Drop += (sender, args) =>
        {
            GetPreviewDropCommand(uiElement).Execute(args.Data);
            args.Handled = true;
        };
    }
    #endregion
}
