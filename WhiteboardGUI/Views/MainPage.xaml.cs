/******************************************************************************
 * Filename    = MainPage.xaml.cs
 *
 * Author      = Yash Mittal
 *
 * Project     = WhiteBoard
 *
 * Description = Code-behind for MainPage handling user interactions and shape manipulations
 *****************************************************************************/

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using WhiteboardGUI.Models;
using WhiteboardGUI.ViewModel;
using System;
using System.Collections.Generic;

namespace WhiteboardGUI.Views;

/// <summary>
/// Interaction logic for MainPage.xaml
/// </summary>
public partial class MainPage : Page
{
    private MainPageViewModel? ViewModel => DataContext as MainPageViewModel;
    private IShape? _resizingShape;
    private string? _currentHandle;
    private Point _startPoint;
    private Rect _initialBounds;
    private List<Point>? _initialPoints;

    public MainPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles the left mouse button down event on the canvas.
    /// Initiates dragging operation.
    /// </summary>
    private void Canvas_LeftMouseButtonDown(object sender, MouseButtonEventArgs e)
    {
        System.Diagnostics.Trace.TraceInformation("Entering Canvas_LeftMouseButtonDown");
        ViewModel?.CanvasLeftMouseDownCommand.Execute(e);
        ViewModel.IsDragging = true;
        System.Diagnostics.Trace.TraceInformation("Exiting Canvas_LeftMouseButtonDown");
    }

    /// <summary>
    /// Handles the mouse move event on the canvas.
    /// Executes the corresponding command in the ViewModel.
    /// </summary>
    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        System.Diagnostics.Trace.TraceInformation("Entering Canvas_MouseMove");
        ViewModel?.CanvasMouseMoveCommand.Execute(e);
        System.Diagnostics.Trace.TraceInformation("Exiting Canvas_MouseMove");
    }

    /// <summary>
    /// Handles the left mouse button up event on the canvas.
    /// Ends dragging operation.
    /// </summary>
    private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        System.Diagnostics.Trace.TraceInformation("Entering Canvas_MouseLeftButtonUp");
        ViewModel?.CanvasMouseUpCommand.Execute(e);
        ViewModel.IsDragging = false;
        System.Diagnostics.Trace.TraceInformation("Exiting Canvas_MouseLeftButtonUp");
    }

    /// <summary>
    /// Opens the color palette popup when the toggle button is checked.
    /// </summary>
    private void PaletteToggleButton_Checked(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Trace.TraceInformation("PaletteToggleButton_Checked called. Opening ColorPopup.");
        ColorPopup.IsOpen = true;
        System.Diagnostics.Trace.TraceInformation("ColorPopup opened.");
    }

    /// <summary>
    /// Closes the color palette popup when the toggle button is unchecked.
    /// </summary>
    private void PaletteToggleButton_Unchecked(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Trace.TraceInformation("PaletteToggleButton_Unchecked called. Closing ColorPopup.");
        ColorPopup.IsOpen = false;
        System.Diagnostics.Trace.TraceInformation("ColorPopup closed.");
    }

    /// <summary>
    /// Opens the upload popup when the upload button is clicked.
    /// </summary>
    private void UploadButton_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Trace.TraceInformation("UploadButton_Click called. Opening UploadPopup.");
        UploadPopup.IsOpen = true;
        System.Diagnostics.Trace.TraceInformation("UploadPopup opened.");
    }

    /// <summary>
    /// Handles the submission of the filename in the upload popup.
    /// Closes the popup and notifies the user.
    /// </summary>
    private void SubmitFileName_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Trace.TraceInformation("SubmitFileName_Click called. Closing UploadPopup and notifying user.");
        UploadPopup.IsOpen = false;
        MessageBox.Show($"Filename '{ViewModel.SnapShotFileName}' has been set.", "Filename Set", MessageBoxButton.OK, MessageBoxImage.Information);
        System.Diagnostics.Trace.TraceInformation($"Filename '{ViewModel.SnapShotFileName}' has been set and user notified.");
    }

    /// <summary>
    /// Initiates the resizing of a shape when a resize handle is pressed.
    /// Captures the mouse and stores initial state for resizing.
    /// </summary>
    private void ResizeHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        System.Diagnostics.Trace.TraceInformation("Entering ResizeHandle_MouseLeftButtonDown");
        if (sender is FrameworkElement rect)
        {
            _currentHandle = rect.Tag as string;
            _resizingShape = rect.DataContext as IShape;
            System.Diagnostics.Trace.TraceInformation($"Resize handle '{_currentHandle}' pressed for shape ID: {_resizingShape?.ShapeId}");
            if (_resizingShape != null)
            {
                _startPoint = e.GetPosition(this);
                Mouse.Capture(rect);
                System.Diagnostics.Trace.TraceInformation($"Mouse captured on resize handle '{_currentHandle}' at point {_startPoint}");

                // Store initial bounds and points for scaling
                _initialBounds = _resizingShape.GetBounds();
                System.Diagnostics.Trace.TraceInformation($"Initial bounds stored for shape ID: {_resizingShape.ShapeId}");
                if (_resizingShape is ScribbleShape scribble)
                {
                    _initialPoints = new List<Point>(scribble.Points);
                    System.Diagnostics.Trace.TraceInformation($"Initial points stored for ScribbleShape ID: {scribble.ShapeId}");
                }

                e.Handled = true;
            }
        }
        System.Diagnostics.Trace.TraceInformation("Exiting ResizeHandle_MouseLeftButtonDown");
    }

    /// <summary>
    /// Handles the mouse move event during a resize operation.
    /// Updates the shape's size based on mouse movement.
    /// </summary>
    private void ResizeHandle_MouseMove(object sender, MouseEventArgs e)
    {
        System.Diagnostics.Trace.TraceInformation("Entering ResizeHandle_MouseMove");
        if (_resizingShape != null && e.LeftButton == MouseButtonState.Pressed)
        {
            Point currentPoint = e.GetPosition(this);
            Vector totalDelta = currentPoint - _startPoint;
            System.Diagnostics.Trace.TraceInformation($"Mouse moved to {currentPoint} with delta {totalDelta}");
            ResizeShape(_resizingShape, _currentHandle, currentPoint);

            // Conditionally update _startPoint only for CircleShape and LineShape
            if (!(_resizingShape is ScribbleShape))
            {
                _startPoint = currentPoint;
                System.Diagnostics.Trace.TraceInformation($"_startPoint updated to {currentPoint} for shape ID: {_resizingShape.ShapeId}");
            }

            e.Handled = true;
        }
        System.Diagnostics.Trace.TraceInformation("Exiting ResizeHandle_MouseMove");
    }

    /// <summary>
    /// Finalizes the resize operation when the left mouse button is released.
    /// Releases the mouse capture and updates the rendering service.
    /// </summary>
    private void ResizeHandle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        System.Diagnostics.Trace.TraceInformation("Entering ResizeHandle_MouseLeftButtonUp");
        if (_resizingShape != null)
        {
            Mouse.Capture(null);
            System.Diagnostics.Trace.TraceInformation($"Mouse capture released for shape ID: {_resizingShape.ShapeId}");
            if (this.DataContext is MainPageViewModel viewModel)
            {
                viewModel.RenderingService.RenderShape(_resizingShape, "MODIFY");
                System.Diagnostics.Trace.TraceInformation($"Rendered shape ID: {_resizingShape.ShapeId} with action 'MODIFY'");
            }
            _resizingShape = null;
            _currentHandle = null;
            _initialBounds = Rect.Empty;
            _initialPoints = null;
            e.Handled = true;
            System.Diagnostics.Trace.TraceInformation("Exiting ResizeHandle_MouseLeftButtonUp");
        }
    }

    /// <summary>
    /// Resizes the specified shape based on the handle being dragged and the current mouse position.
    /// Delegates to specific resize methods based on shape type.
    /// </summary>
    private void ResizeShape(IShape shape, string handle, Point currentPoint)
    {
        System.Diagnostics.Trace.TraceInformation($"Entering ResizeShape for shape ID: {shape.ShapeId} with handle: {handle}");
        if (shape is LineShape line)
        {
            ResizeLineShape(line, handle, currentPoint);
        }
        else if (shape is CircleShape circle)
        {
            ResizeCircleShape(circle, handle, currentPoint - _startPoint);
            _startPoint = currentPoint; // Update for CircleShape
            System.Diagnostics.Trace.TraceInformation($"_startPoint updated for CircleShape ID: {circle.ShapeId}");
        }
        else if (shape is ScribbleShape scribble)
        {
            ResizeScribbleShape(scribble, handle, currentPoint - _startPoint);
            // Do not update _startPoint for ScribbleShape
            System.Diagnostics.Trace.TraceInformation($"Resized ScribbleShape ID: {scribble.ShapeId} without updating _startPoint");
        }
        // Add more shapes if needed
        System.Diagnostics.Trace.TraceInformation($"Exiting ResizeShape for shape ID: {shape.ShapeId}");
    }

    /// <summary>
    /// Resizes a line shape by updating its start or end coordinates.
    /// </summary>
    private void ResizeLineShape(LineShape line, string handle, Point currentPoint)
    {
        System.Diagnostics.Trace.TraceInformation($"Resizing LineShape ID: {line.ShapeId} using handle: {handle}");
        if (handle == "Start")
        {
            line.StartX = currentPoint.X;
            line.StartY = currentPoint.Y;
            System.Diagnostics.Trace.TraceInformation($"LineShape ID: {line.ShapeId} start point updated to ({line.StartX}, {line.StartY})");
        }
        else if (handle == "End")
        {
            line.EndX = currentPoint.X;
            line.EndY = currentPoint.Y;
            System.Diagnostics.Trace.TraceInformation($"LineShape ID: {line.ShapeId} end point updated to ({line.EndX}, {line.EndY})");
        }
    }

    // Existing methods for other shapes remain unchanged

    /// <summary>
    /// Resizes a scribble shape by scaling its points based on the handle and mouse movement.
    /// Ensures the shape does not collapse below minimum size.
    /// </summary>
    private void ResizeScribbleShape(ScribbleShape scribble, string handle, Vector totalDelta)
    {
        System.Diagnostics.Trace.TraceInformation($"Entering ResizeScribbleShape for ScribbleShape ID: {scribble.ShapeId}");
        if (_initialBounds == Rect.Empty || _initialPoints == null)
        {
            System.Diagnostics.Trace.TraceWarning($"Initial bounds or points not set for ScribbleShape ID: {scribble.ShapeId}");
            return;
        }

        double minWidth = 8;
        double minHeight = 8;

        double newLeft = _initialBounds.Left;
        double newTop = _initialBounds.Top;
        double newWidth = _initialBounds.Width;
        double newHeight = _initialBounds.Height;

        // Adjust new bounds based on handle and totalDelta
        switch (handle)
        {
            case "TopLeft":
                newLeft += totalDelta.X;
                newTop += totalDelta.Y;
                newWidth -= totalDelta.X;
                newHeight -= totalDelta.Y;
                break;
            case "TopRight":
                newTop += totalDelta.Y;
                newWidth += totalDelta.X;
                newHeight -= totalDelta.Y;
                break;
            case "BottomLeft":
                newLeft += totalDelta.X;
                newWidth -= totalDelta.X;
                newHeight += totalDelta.Y;
                break;
            case "BottomRight":
                newWidth += totalDelta.X;
                newHeight += totalDelta.Y;
                break;
            default:
                break;
        }

        System.Diagnostics.Trace.TraceInformation($"Resizing ScribbleShape ID: {scribble.ShapeId} with newLeft: {newLeft}, newTop: {newTop}, newWidth: {newWidth}, newHeight: {newHeight}");

        // Enforce minimum size
        if (newWidth < minWidth)
        {
            if (handle == "TopLeft" || handle == "BottomLeft")
            {
                newLeft = _initialBounds.Right - minWidth;
            }
            newWidth = minWidth;
            System.Diagnostics.Trace.TraceInformation($"ScribbleShape ID: {scribble.ShapeId} width adjusted to minimum size: {minWidth}");
        }

        if (newHeight < minHeight)
        {
            if (handle == "TopLeft" || handle == "TopRight")
            {
                newTop = _initialBounds.Bottom - minHeight;
            }
            newHeight = minHeight;
            System.Diagnostics.Trace.TraceInformation($"ScribbleShape ID: {scribble.ShapeId} height adjusted to minimum size: {minHeight}");
        }

        // Determine the anchor point based on handle
        Point anchor;
        switch (handle)
        {
            case "TopLeft":
                anchor = new Point(_initialBounds.Right, _initialBounds.Bottom);
                break;
            case "TopRight":
                anchor = new Point(_initialBounds.Left, _initialBounds.Bottom);
                break;
            case "BottomLeft":
                anchor = new Point(_initialBounds.Right, _initialBounds.Top);
                break;
            case "BottomRight":
                anchor = new Point(_initialBounds.Left, _initialBounds.Top);
                break;
            default:
                anchor = new Point(_initialBounds.Left, _initialBounds.Top);
                break;
        }

        System.Diagnostics.Trace.TraceInformation($"Anchor point for ScribbleShape ID: {scribble.ShapeId} set to {anchor}");

        // Calculate scaling factors
        double scaleX = _initialBounds.Width != 0 ? newWidth / _initialBounds.Width : 1;
        double scaleY = _initialBounds.Height != 0 ? newHeight / _initialBounds.Height : 1;

        System.Diagnostics.Trace.TraceInformation($"Scaling factors for ScribbleShape ID: {scribble.ShapeId} - scaleX: {scaleX}, scaleY: {scaleY}");

        // Apply scaling to each point relative to the anchor
        List<Point> newPoints = new List<Point>();
        foreach (Point point in _initialPoints)
        {
            double newX = anchor.X + (point.X - anchor.X) * scaleX;
            double newY = anchor.Y + (point.Y - anchor.Y) * scaleY;
            newPoints.Add(new Point(newX, newY));
        }

        // Update the ScribbleShape's points
        scribble.Points = newPoints;
        System.Diagnostics.Trace.TraceInformation($"ScribbleShape ID: {scribble.ShapeId} points updated after resizing.");
        System.Diagnostics.Trace.TraceInformation($"Exiting ResizeScribbleShape for ScribbleShape ID: {scribble.ShapeId}");
    }

    /// <summary>
    /// Resizes a circle shape by updating its center and radii based on the handle and mouse movement.
    /// Ensures the circle does not collapse below minimum size.
    /// </summary>
    private void ResizeCircleShape(CircleShape circle, string handle, Vector delta)
    {
        System.Diagnostics.Trace.TraceInformation($"Entering ResizeCircleShape for CircleShape ID: {circle.ShapeId} with handle: {handle}");
        double minSize = 8; // Minimum size to prevent collapsing

        switch (handle)
        {
            case "TopLeft":
                {
                    double newLeft = circle.Left + delta.X;
                    double newTop = circle.Top + delta.Y;
                    double newWidth = circle.Width - delta.X;
                    double newHeight = circle.Height - delta.Y;

                    System.Diagnostics.Trace.TraceInformation($"Resizing TopLeft of CircleShape ID: {circle.ShapeId} to newLeft: {newLeft}, newTop: {newTop}, newWidth: {newWidth}, newHeight: {newHeight}");

                    if (newWidth >= minSize && newHeight >= minSize)
                    {
                        circle.CenterX = newLeft + newWidth / 2;
                        circle.CenterY = newTop + newHeight / 2;
                        circle.RadiusX = newWidth / 2;
                        circle.RadiusY = newHeight / 2;
                        System.Diagnostics.Trace.TraceInformation($"CircleShape ID: {circle.ShapeId} resized successfully.");
                    }
                    else
                    {
                        System.Diagnostics.Trace.TraceWarning($"CircleShape ID: {circle.ShapeId} resize ignored due to minimum size constraints.");
                    }
                    break;
                }
            case "TopRight":
                {
                    double newTop = circle.Top + delta.Y;
                    double newWidth = circle.Width + delta.X;
                    double newHeight = circle.Height - delta.Y;

                    System.Diagnostics.Trace.TraceInformation($"Resizing TopRight of CircleShape ID: {circle.ShapeId} to newTop: {newTop}, newWidth: {newWidth}, newHeight: {newHeight}");

                    if (newWidth >= minSize && newHeight >= minSize)
                    {
                        circle.CenterX = circle.Left + newWidth / 2;
                        circle.CenterY = newTop + newHeight / 2;
                        circle.RadiusX = newWidth / 2;
                        circle.RadiusY = newHeight / 2;
                        System.Diagnostics.Trace.TraceInformation($"CircleShape ID: {circle.ShapeId} resized successfully.");
                    }
                    else
                    {
                        System.Diagnostics.Trace.TraceWarning($"CircleShape ID: {circle.ShapeId} resize ignored due to minimum size constraints.");
                    }
                    break;
                }
            case "BottomLeft":
                {
                    double newLeft = circle.Left + delta.X;
                    double newWidth = circle.Width - delta.X;
                    double newHeight = circle.Height + delta.Y;

                    System.Diagnostics.Trace.TraceInformation($"Resizing BottomLeft of CircleShape ID: {circle.ShapeId} to newLeft: {newLeft}, newWidth: {newWidth}, newHeight: {newHeight}");

                    if (newWidth >= minSize && newHeight >= minSize)
                    {
                        circle.CenterX = newLeft + newWidth / 2;
                        circle.CenterY = circle.Top + newHeight / 2;
                        circle.RadiusX = newWidth / 2;
                        circle.RadiusY = newHeight / 2;
                        System.Diagnostics.Trace.TraceInformation($"CircleShape ID: {circle.ShapeId} resized successfully.");
                    }
                    else
                    {
                        System.Diagnostics.Trace.TraceWarning($"CircleShape ID: {circle.ShapeId} resize ignored due to minimum size constraints.");
                    }
                    break;
                }
            case "BottomRight":
                {
                    double newWidth = circle.Width + delta.X;
                    double newHeight = circle.Height + delta.Y;

                    System.Diagnostics.Trace.TraceInformation($"Resizing BottomRight of CircleShape ID: {circle.ShapeId} to newWidth: {newWidth}, newHeight: {newHeight}");

                    if (newWidth >= minSize && newHeight >= minSize)
                    {
                        circle.CenterX = circle.Left + newWidth / 2;
                        circle.CenterY = circle.Top + newHeight / 2;
                        circle.RadiusX = newWidth / 2;
                        circle.RadiusY = newHeight / 2;
                        System.Diagnostics.Trace.TraceInformation($"CircleShape ID: {circle.ShapeId} resized successfully.");
                    }
                    else
                    {
                        System.Diagnostics.Trace.TraceWarning($"CircleShape ID: {circle.ShapeId} resize ignored due to minimum size constraints.");
                    }
                    break;
                }
            default:
                System.Diagnostics.Trace.TraceWarning($"Unknown handle '{handle}' for CircleShape ID: {circle.ShapeId}");
                break;
        }
        System.Diagnostics.Trace.TraceInformation($"Exiting ResizeCircleShape for CircleShape ID: {circle.ShapeId}");
    }

    /// <summary>
    /// Handles the right mouse button down event on text shapes.
    /// Generates a context menu with text-specific options.
    /// </summary>
    private void Shape_MouseRightButtonDownText(object sender, MouseButtonEventArgs e)
    {
        System.Diagnostics.Trace.TraceInformation("Entering Shape_MouseRightButtonDownText");
        GenerateContextMenuForRightClick(sender, e, true);
        System.Diagnostics.Trace.TraceInformation("Exiting Shape_MouseRightButtonDownText");
    }

    /// <summary>
    /// Handles the right mouse button down event on non-text shapes.
    /// Generates a context menu with general shape options.
    /// </summary>
    private void Shape_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        System.Diagnostics.Trace.TraceInformation("Entering Shape_MouseRightButtonDown");
        GenerateContextMenuForRightClick(sender, e, false);
        System.Diagnostics.Trace.TraceInformation("Exiting Shape_MouseRightButtonDown");
    }

    /// <summary>
    /// Generates and displays a context menu for the clicked shape.
    /// Adds additional options if the shape is a text shape.
    /// </summary>
    private void GenerateContextMenuForRightClick(object sender, MouseButtonEventArgs e, bool isText)
    {
        System.Diagnostics.Trace.TraceInformation($"Entering GenerateContextMenuForRightClick with isText={isText}");
        // Get the ViewModel
        var vm = this.DataContext as MainPageViewModel;

        // Check if the current tool is Select
        if (vm.CurrentTool != ShapeType.Select)
        {
            System.Diagnostics.Trace.TraceInformation("Current tool is not Select. Context menu will not be generated.");
            return;
        }

        // Get the shape from the sender's DataContext
        var shapeElement = sender as FrameworkElement;
        var shape = shapeElement.DataContext as IShape;

        // Check if the shape is the selected shape
        if (vm.SelectedShape != shape)
        {
            System.Diagnostics.Trace.TraceInformation("Clicked shape is not the selected shape. Context menu will not be generated.");
            return;
        }

        // Create the ContextMenu
        var contextMenu = new ContextMenu();

        var sendBackwardMenuItem = new MenuItem {
            Header = "Send Backward",
            Command = vm.SendBackwardCommand,
            CommandParameter = shape
        };
        System.Diagnostics.Trace.TraceInformation("Send Backward menu item created.");

        var sendToBackMenuItem = new MenuItem {
            Header = "Send to Back",
            Command = vm.SendToBackCommand,
            CommandParameter = shape
        };
        System.Diagnostics.Trace.TraceInformation("Send to Back menu item created.");

        if (isText)
        {
            var editMenuItem = new MenuItem {
                Header = "Edit Text",
                Command = vm.EditTextCommand,
                CommandParameter = shape
            };
            contextMenu.Items.Add(editMenuItem);
            System.Diagnostics.Trace.TraceInformation("Edit Text menu item added for text shape.");
        }

        contextMenu.Items.Add(sendBackwardMenuItem);
        contextMenu.Items.Add(sendToBackMenuItem);
        System.Diagnostics.Trace.TraceInformation("Send Backward and Send to Back menu items added to context menu.");

        // Assign the ContextMenu to the shape element
        shapeElement.ContextMenu = contextMenu;
        System.Diagnostics.Trace.TraceInformation("ContextMenu assigned to shape element.");

        // Open the ContextMenu
        contextMenu.IsOpen = true;
        System.Diagnostics.Trace.TraceInformation("ContextMenu opened.");

        // Mark the event as handled to prevent further processing
        e.Handled = true;
        System.Diagnostics.Trace.TraceInformation("Event handled in GenerateContextMenuForRightClick.");
        System.Diagnostics.Trace.TraceInformation($"Exiting GenerateContextMenuForRightClick for shape ID: {shape?.ShapeId}");
    }

    /// <summary>
    /// Handles the value changed event of the slider.
    /// Currently not implemented.
    /// </summary>
    private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        System.Diagnostics.Trace.TraceInformation("Slider_ValueChanged event triggered.");
        // Currently not implemented.
    }
}
