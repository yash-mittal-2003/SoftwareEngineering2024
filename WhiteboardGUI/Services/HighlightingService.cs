/**************************************************************************************************
 * Filename    : HighlightingService.cs
 *
 * Author      : Rachit Jain
 *
 * Product     : WhiteBoard
 * 
 * Project     : Tooltip Feature for Canvas Shapes
 *
 * Description : Implements tooltip-like functionality that displays information about shapes
 *               and their creators when the user hovers over them on the canvas.
 *************************************************************************************************/

using System.Diagnostics; // For Trace
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WhiteboardGUI.Adorners;
using WhiteboardGUI.Models;
using WhiteboardGUI.ViewModel;

namespace WhiteboardGUI.Services;

/// <summary>
/// Provides services for highlighting shapes on mouse hover by adding hover adorners.
/// </summary>
public static class HighlightingService
{
    public static readonly DependencyProperty EnableHighlightingProperty =
        DependencyProperty.RegisterAttached(
            "EnableHighlighting",
            typeof(bool),
            typeof(HighlightingService),
            new PropertyMetadata(false, OnEnableHighlightingChanged));

    public static bool GetEnableHighlighting(DependencyObject obj)
    {
        Trace.TraceInformation("Entering GetEnableHighlighting");
        bool result = (bool)obj.GetValue(EnableHighlightingProperty);
        Trace.TraceInformation("Exiting GetEnableHighlighting");
        return result;
    }

    public static void SetEnableHighlighting(DependencyObject obj, bool value)
    {
        Trace.TraceInformation("Entering SetEnableHighlighting");
        obj.SetValue(EnableHighlightingProperty, value);
        Trace.TraceInformation("Exiting SetEnableHighlighting");
    }

    private static readonly DependencyProperty s_hoverTimerProperty =
        DependencyProperty.RegisterAttached(
            "HoverTimer",
            typeof(DispatcherTimer),
            typeof(HighlightingService),
            new PropertyMetadata(null));

    private static DispatcherTimer GetHoverTimer(DependencyObject obj)
    {
        Trace.TraceInformation("Entering GetHoverTimer");
        var result = (DispatcherTimer)obj.GetValue(s_hoverTimerProperty);
        Trace.TraceInformation("Exiting GetHoverTimer");
        return result;
    }

    private static void SetHoverTimer(DependencyObject obj, DispatcherTimer value)
    {
        Trace.TraceInformation("Entering SetHoverTimer");
        obj.SetValue(s_hoverTimerProperty, value);
        Trace.TraceInformation("Exiting SetHoverTimer");
    }

    private static void OnEnableHighlightingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        Trace.TraceInformation("Entering OnEnableHighlightingChanged");
        if (d is UIElement element)
        {
            if ((bool)e.NewValue)
            {
                element.MouseEnter += Element_MouseEnter;
                element.MouseLeave += Element_MouseLeave;
            }
            else
            {
                element.MouseEnter -= Element_MouseEnter;
                element.MouseLeave -= Element_MouseLeave;
            }
        }
        Trace.TraceInformation("Exiting OnEnableHighlightingChanged");
    }

    private static void Element_MouseEnter(object sender, MouseEventArgs e)
    {
        Trace.TraceInformation("Entering Element_MouseEnter");
        if (sender is FrameworkElement element)
        {
            DispatcherTimer hoverTimer = new DispatcherTimer {
                Interval = TimeSpan.FromSeconds(0.4)
            };
            hoverTimer.Tick += (s, args) => {
                Trace.TraceInformation("Hover timer tick started");
                hoverTimer.Stop();
                SetHoverTimer(element, null);

                MainPageViewModel? viewModel = FindParentViewModel(element);
                if (viewModel is MainPageViewModel vm && element.DataContext is IShape shape)
                {
                    Point elementPosition = element.TranslatePoint(new Point(0, 0), Application.Current.MainWindow);
                    AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(element);

                    if (adornerLayer != null)
                    {
                        RemoveHoverAdorner(adornerLayer, vm);
                        vm.HoveredShape = shape;
                        vm.IsShapeHovered = true;
                        Color shapeColor = (Color)ColorConverter.ConvertFromString(shape.Color);
                        ImageSource imageSource = GetImageSourceForShape(shape);

                        HoverAdorner hoverAdorner = new HoverAdorner(element, vm.HoveredShapeDetails, elementPosition, imageSource, shapeColor);
                        adornerLayer.Add(hoverAdorner);

                        vm.CurrentHoverAdorner = hoverAdorner;
                    }
                }
                Trace.TraceInformation("Hover timer tick ended");
            };

            SetHoverTimer(element, hoverTimer);
            hoverTimer.Start();
        }
        Trace.TraceInformation("Exiting Element_MouseEnter");
    }

    private static ImageSource GetImageSourceForShape(IShape shape)
    {
        Trace.TraceInformation("Entering GetImageSourceForShape");
        try
        {
            var result = new BitmapImage(new Uri(shape.ProfilePictureURL));
            Trace.TraceInformation("Exiting GetImageSourceForShape successfully");
            return result;
        }
        catch
        {
            Trace.TraceWarning("Failed to get image source for shape");
            return null;
        }
    }

    private static void Element_MouseLeave(object sender, MouseEventArgs e)
    {
        Trace.TraceInformation("Entering Element_MouseLeave");
        if (sender is FrameworkElement element)
        {
            DispatcherTimer hoverTimer = GetHoverTimer(element);
            hoverTimer?.Stop();
            SetHoverTimer(element, null);

            MainPageViewModel? viewModel = FindParentViewModel(element);
            if (viewModel is MainPageViewModel vm && element.DataContext is IShape shape)
            {
                AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(element);
                if (adornerLayer != null && vm.CurrentHoverAdorner != null)
                {
                    adornerLayer.Remove(vm.CurrentHoverAdorner);
                    vm.CurrentHoverAdorner = null;
                }

                vm.HoveredShape = null;
                vm.IsShapeHovered = false;
            }
        }
        Trace.TraceInformation("Exiting Element_MouseLeave");
    }

    private static MainPageViewModel? FindParentViewModel(FrameworkElement element)
    {
        Trace.TraceInformation("Entering FindParentViewModel");
        DependencyObject parent = element;
        while (parent != null)
        {
            parent = VisualTreeHelper.GetParent(parent);
            if (parent is FrameworkElement fe && fe.DataContext is MainPageViewModel vm)
            {
                Trace.TraceInformation("Exiting FindParentViewModel with result");
                return vm;
            }
        }
        Trace.TraceWarning("Exiting FindParentViewModel with null");
        return null;
    }

    private static void RemoveHoverAdorner(AdornerLayer adornerLayer, MainPageViewModel vm)
    {
        Trace.TraceInformation("Entering RemoveHoverAdorner");
        if (vm.CurrentHoverAdorner != null)
        {
            adornerLayer.Remove(vm.CurrentHoverAdorner);
            vm.CurrentHoverAdorner = null;
        }
        Trace.TraceInformation("Exiting RemoveHoverAdorner");
    }
}
