using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using WhiteboardGUI.Models;

namespace WhiteboardGUI.Services;

/// <summary>
/// Manages Z-indexing of shapes in an ObservableCollection, allowing for reordering based on overlap and user actions.
/// </summary>
public class MoveShapeZIndexing
{
    /// <summary>
    /// Collection of shapes managed for Z-indexing.
    /// </summary>
    private ObservableCollection<IShape> _shapes;

    /// <summary>
    /// Initializes a new instance of the <see cref="MoveShapeZIndexing"/> class.
    /// </summary>
    /// <param name="shapes">The collection of shapes to manage.</param>
    public MoveShapeZIndexing(ObservableCollection<IShape> shapes)
    {
        Trace.TraceInformation("Entering constructor of MoveShapeZIndexing");
        _shapes = shapes;
        Trace.TraceInformation("Exiting constructor of MoveShapeZIndexing");
    }

    /// <summary>
    /// Moves the specified shape to the back of the collection, giving it the lowest Z-index.
    /// </summary>
    /// <param name="shape">The shape to move.</param>
    public void MoveShapeBack(IShape shape)
    {
        Trace.TraceInformation("Entering MoveShapeBack");
        Application.Current.Dispatcher.Invoke(() => {
            if (shape == null || !(bool)_shapes.Any(s => (s as dynamic).ShapeId.Equals(shape.ShapeId)))
            {
                Trace.TraceWarning("MoveShapeBack: Shape is null or not found in the collection.");
                return;
            }

            _shapes.Remove(_shapes.FirstOrDefault(s => (s as dynamic).ShapeId == shape.ShapeId));
            _shapes.Insert(0, shape);
            Trace.TraceInformation("MoveShapeBack: Shape moved to the back of the collection.");
            UpdateZIndices();
        });
        Trace.TraceInformation("Exiting MoveShapeBack");
    }

    /// <summary>
    /// Updates the Z-index of all shapes in the collection to reflect their position.
    /// </summary>
    public void UpdateZIndices()
    {
        Trace.TraceInformation("Entering UpdateZIndices");
        for (int i = 0; i < _shapes.Count; i++)
        {
            _shapes[i].ZIndex = i;
        }
        Trace.TraceInformation("Exiting UpdateZIndices");
    }

    /// <summary>
    /// Moves the specified shape one step backward in the Z-index, if possible, and swaps it with an overlapping shape.
    /// </summary>
    /// <param name="shape">The shape to move.</param>
    public void MoveShapeBackward(IShape shape)
    {
        Trace.TraceInformation("Entering MoveShapeBackward");
        Application.Current.Dispatcher.Invoke(() => {
            if (shape == null || !(bool)_shapes.Any(s => (s as dynamic).ShapeId.Equals(shape.ShapeId)))
            {
                Trace.TraceWarning("MoveShapeBackward: Shape is null or not found in the collection.");
                return;
            }

            int currentIndex = _shapes.Select((s, index) => new { s, index })
                             .FirstOrDefault(item => (item.s as dynamic).ShapeId.Equals(shape.ShapeId))?.index ?? -1;
            if (currentIndex <= 0)
            {
                Trace.TraceWarning("MoveShapeBackward: Shape is already at the bottom of the collection.");
                return; // Already at the bottom
            }

            // Iterate backward to find the first overlapping shape based on stroke
            for (int i = currentIndex - 1; i >= 0; i--)
            {
                IShape otherShape = _shapes[i];
                if (AreShapesOverlapping(shape, otherShape))
                {
                    Trace.TraceInformation($"MoveShapeBackward: Swapping shape at index {currentIndex} with shape at index {i}.");
                    _shapes.Move(currentIndex, i);
                    UpdateZIndices();
                    break;
                }
            }
        });
        Trace.TraceInformation("Exiting MoveShapeBackward");
    }

    /// <summary>
    /// Determines if the strokes of two shapes overlap.
    /// </summary>
    /// <param name="shape1">The first shape.</param>
    /// <param name="shape2">The second shape.</param>
    /// <returns><c>true</c> if the shapes overlap; otherwise, <c>false</c>.</returns>
    private bool AreShapesOverlapping(IShape shape1, IShape shape2)
    {
        Trace.TraceInformation("Entering AreShapesOverlapping");
        Geometry strokeGeometry1 = GetShapeStrokeGeometry(shape1);
        Geometry strokeGeometry2 = GetShapeStrokeGeometry(shape2);

        if (strokeGeometry1 == null || strokeGeometry2 == null)
        {
            Trace.TraceWarning("AreShapesOverlapping: One or both shapes have null stroke geometries.");
            return false;
        }

        PathGeometry combinedGeometry = Geometry.Combine(
            strokeGeometry1,
            strokeGeometry2,
            GeometryCombineMode.Intersect,
            null);

        bool result = !combinedGeometry.IsEmpty();
        Trace.TraceInformation($"AreShapesOverlapping: Overlap result is {result}.");
        Trace.TraceInformation("Exiting AreShapesOverlapping");
        return result;
    }

    /// <summary>
    /// Generates the stroke geometry of a shape based on its type, stroke color, and thickness.
    /// </summary>
    /// <param name="shape">The shape to process.</param>
    /// <returns>A <see cref="Geometry"/> object representing the stroke area, or <c>null</c> if unsupported.</returns>
    private Geometry GetShapeStrokeGeometry(IShape shape)
    {
        Trace.TraceInformation("Entering GetShapeStrokeGeometry");
        if (shape == null)
        {
            Trace.TraceWarning("GetShapeStrokeGeometry: Shape is null.");
            return null;
        }

        Geometry geometry = null;

        switch (shape)
        {
            case CircleShape circle:
                Trace.TraceInformation("GetShapeStrokeGeometry: Processing CircleShape.");
                geometry = new EllipseGeometry(new Point(circle.Left + circle.Width / 2, circle.Top + circle.Height / 2),
                                               circle.Width / 2, circle.Height / 2);
                break;

            case LineShape line:
                Trace.TraceInformation("GetShapeStrokeGeometry: Processing LineShape.");
                geometry = new LineGeometry(new Point(line.StartX, line.StartY), new Point(line.EndX, line.EndY));
                break;

            case ScribbleShape scribble:
                Trace.TraceInformation("GetShapeStrokeGeometry: Processing ScribbleShape.");
                if (scribble.PointCollection == null || !scribble.PointCollection.Any())
                {
                    Trace.TraceWarning("GetShapeStrokeGeometry: ScribbleShape has an empty or null PointCollection.");
                    return null;
                }
                StreamGeometry streamGeometry = new StreamGeometry();
                using (StreamGeometryContext ctx = streamGeometry.Open())
                {
                    ctx.BeginFigure(scribble.PointCollection.First(), false, false);
                    ctx.PolyLineTo(scribble.PointCollection.Skip(1).ToList(), true, true);
                }
                streamGeometry.Freeze();
                geometry = streamGeometry;
                break;

            default:
                Trace.TraceWarning("GetShapeStrokeGeometry: Unsupported shape type.");
                return null;
        }

        if (geometry != null)
        {
            try
            {
                Color color = (Color)ColorConverter.ConvertFromString(shape.Color.ToString());
                Pen pen = new Pen(new SolidColorBrush(color), shape.StrokeThickness);
                pen.Freeze();
                PathGeometry strokeGeometry = geometry.GetWidenedPathGeometry(pen);
                strokeGeometry.Freeze();
                Trace.TraceInformation("GetShapeStrokeGeometry: Geometry successfully widened.");
                return strokeGeometry;
            }
            catch (Exception ex)
            {
                Trace.TraceError($"GetShapeStrokeGeometry: Error widening geometry - {ex.Message}");
                return null;
            }
        }

        Trace.TraceInformation("Exiting GetShapeStrokeGeometry");
        return null;
    }
}
