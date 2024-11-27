/**************************************************************************************************
 * Filename    : HoverAdorner.cs
 *
 * Author      : Rachit Jain
 *
 * Product     : WhiteBoard
 * 
 * Project     : Tooltip Feature for Canvas Shapes
 *
 * Description : Implements an adorner that displays a tooltip-like UI element near the mouse pointer,
 *               showing information about shapes and their creators when the user hovers over them
 *               on the canvas.
 *************************************************************************************************/

using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace WhiteboardGUI.Adorners;

/// <summary>
/// Represents a hover adorner that displays a tooltip-like UI element near the mouse pointer.
/// </summary>
public class HoverAdorner : Adorner
{
    private readonly VisualCollection _visuals;
    private readonly Border _border;
    private readonly StackPanel _stackPanel;
    private readonly Image _image;
    private readonly TextBlock _textBlockCreator;
    private readonly TextBlock _textBlockModified;
    private readonly Point _mousePosition;

    /// <summary>
    /// Initializes a new instance of the <see cref="HoverAdorner"/> class.
    /// </summary>
    /// <param name="adornedElement">The element to which the adorner is attached.</param>
    /// <param name="text">The text to display in the adorner.</param>
    /// <param name="mousePosition">The initial position of the mouse pointer.</param>
    /// <param name="imageSource">The image source to display in the adorner.</param>
    /// <param name="shapeColor">The color of the shape being hovered over.</param>
    public HoverAdorner(UIElement adornedElement, string text, Point mousePosition, ImageSource imageSource, Color shapeColor)
        : base(adornedElement)
    {
        Trace.TraceInformation("Initializing HoverAdorner");
        _mousePosition = mousePosition;
        _visuals = new VisualCollection(this);
        string[] lines = text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        string userName = null;
        string lastModifiedBy = null;

        foreach (string line in lines)
        {
            string[] parts = line.Split(new[] { ": " }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                if (parts[0].Trim() == "Created By")
                {
                    userName = parts[1].Trim().Split(' ')[0];
                }
                else if (parts[0].Trim() == "Last Modified By")
                {
                    lastModifiedBy = parts[1].Trim().Split(' ')[0];
                }
            }
        }
        if (!string.IsNullOrEmpty(userName) && userName.Length > 15)
        {
            userName = userName.Substring(0, 15);
        }

        if (!string.IsNullOrEmpty(lastModifiedBy) && lastModifiedBy.Length > 15)
        {
            lastModifiedBy = lastModifiedBy.Substring(0, 15);
        }

        _image = new Image {
            Source = imageSource,
            Width = 60,
            Height = 60,
            Margin = new Thickness(5, 5, 5, 5),
            IsHitTestVisible = false
        };

        _textBlockCreator = new TextBlock {
            Text = $"Created By: {userName}... ",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14,
            Foreground = Brushes.Black,
            Background = Brushes.Transparent,
            Padding = new Thickness(0),
            IsHitTestVisible = false
        };

        _textBlockModified = new TextBlock {
            Text = $"Last Modified By: {lastModifiedBy}... ",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14,
            Foreground = Brushes.Black,
            Background = Brushes.Transparent,
            Padding = new Thickness(0),
            IsHitTestVisible = false
        };

        _stackPanel = new StackPanel {
            Orientation = Orientation.Vertical,
            Children = { _image, _textBlockCreator, _textBlockModified },
            IsHitTestVisible = false
        };

        _border = new Border {
            Child = _stackPanel,
            Background = Brushes.LightYellow,
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(3),
            IsHitTestVisible = false
        };

        _visuals.Add(_border);
        Trace.TraceInformation("HoverAdorner initialized successfully");
    }

    /// <summary>
    /// Gets the number of visual children managed by the adorner.
    /// </summary>
    protected override int VisualChildrenCount
    {
        get {
            Trace.TraceInformation("Getting VisualChildrenCount");
            return _visuals.Count;
        }
    }

    /// <summary>
    /// Gets a specific visual child by index.
    /// </summary>
    /// <param name="index">The index of the visual child to retrieve.</param>
    /// <returns>The visual child at the specified index.</returns>
    protected override Visual GetVisualChild(int index)
    {
        Trace.TraceInformation($"Getting VisualChild at index: {index}");
        return _visuals[index];
    }

    /// <summary>
    /// Measures the size required by the adorner.
    /// </summary>
    /// <param name="constraint">The size constraint for measuring.</param>
    /// <returns>The desired size of the adorner.</returns>
    protected override Size MeasureOverride(Size constraint)
    {
        Trace.TraceInformation("Measuring size for HoverAdorner");
        _border.Measure(constraint);
        return _border.DesiredSize;
    }

    /// <summary>
    /// Arranges the adorner at a specific position relative to the mouse pointer, ensuring it stays within bounds.
    /// </summary>
    /// <param name="finalSize">The final size of the adorned element.</param>
    /// <returns>The arranged size of the adorner.</returns>
    protected override Size ArrangeOverride(Size finalSize)
    {
        Trace.TraceInformation("Arranging HoverAdorner");
        double x = _mousePosition.X + 10;
        double y = _mousePosition.Y + 10;

        if (x + _border.DesiredSize.Width > finalSize.Width)
        {
            x = finalSize.Width - _border.DesiredSize.Width - 10;
        }

        if (y + _border.DesiredSize.Height > finalSize.Height)
        {
            y = finalSize.Height - _border.DesiredSize.Height - 10;
        }

        _border.Arrange(new Rect(x, y, _border.DesiredSize.Width, _border.DesiredSize.Height));
        return finalSize;
    }

    /// <summary>
    /// Updates the text displayed in the adorner.
    /// </summary>
    /// <param name="text">The new text to display.</param>
    public void UpdateText(string text)
    {
        Trace.TraceInformation("Updating text in HoverAdorner");
        string[] lines = text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        string userName = null;
        string lastModifiedBy = null;

        foreach (string line in lines)
        {
            string[] parts = line.Split(new[] { ": " }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                if (string.Equals(parts[0].Trim(), "Created By", StringComparison.OrdinalIgnoreCase))
                {
                    string[] nameParts = parts[1].Trim().Split(' ');
                    userName = nameParts.Length > 0 ? nameParts[0] : parts[1].Trim();
                }
                else if (string.Equals(parts[0].Trim(), "Last Modified By", StringComparison.OrdinalIgnoreCase))
                {
                    string[] nameParts = parts[1].Trim().Split(' ');
                    lastModifiedBy = nameParts.Length > 0 ? nameParts[0] : parts[1].Trim();
                }
            }
        }
        if (!string.IsNullOrEmpty(userName) && userName.Length > 15)
        {
            userName = userName.Substring(0, 15);
        }

        if (!string.IsNullOrEmpty(lastModifiedBy) && lastModifiedBy.Length > 15)
        {
            lastModifiedBy = lastModifiedBy.Substring(0, 15);
        }

        _textBlockCreator.Text = $"Created By: {userName}";
        _textBlockModified.Text = $"Last Modified By: {lastModifiedBy}";

        InvalidateMeasure();
        InvalidateVisual();
        Trace.TraceInformation("HoverAdorner text updated successfully");
    }
}

