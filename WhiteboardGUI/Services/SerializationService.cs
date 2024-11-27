/**************************************************************************************************
 * Filename    = Searialization.cs
 *
 * Authors     = Vishnu Nair
 *
 * Product     = WhiteBoard Application
 * 
 * Project     = Data serialization
 *
 * Description = Handles serialization and de-serialization of the shapes
 *************************************************************************************************/


using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Diagnostics;
using WhiteboardGUI.Models;

namespace WhiteboardGUI.Services;

/// <summary>
/// Provides methods for serializing and deserializing shapes and snapshots.
/// </summary>
public static class SerializationService
{
    /// <summary>
    /// Serializes an <see cref="IShape"/> object to a JSON string.
    /// </summary>
    /// <param name="shape">The shape object to serialize.</param>
    /// <returns>A JSON string representation of the shape.</returns>
    public static string SerializeShape(IShape shape)
    {
        Trace.TraceInformation("Entering SerializeShape");
        string result = JsonConvert.SerializeObject(shape);
        Trace.TraceInformation("Exiting SerializeShape");
        return result;
    }

    /// <summary>
    /// Serializes a <see cref="SnapShot"/> object to a JSON string.
    /// </summary>
    /// <param name="snapShot">The snapshot object to serialize.</param>
    /// <returns>A JSON string representation of the snapshot.</returns>
    public static string SerializeSnapShot(SnapShot snapShot)
    {
        Trace.TraceInformation("Entering SerializeSnapShot");
        string result = JsonConvert.SerializeObject(snapShot);
        Trace.TraceInformation("Exiting SerializeSnapShot");
        return result;
    }

    /// <summary>
    /// Deserializes a JSON string into a <see cref="SnapShot"/> object.
    /// </summary>
    /// <param name="data">The JSON string to deserialize.</param>
    /// <returns>A <see cref="SnapShot"/> object represented by the JSON string.</returns>
    public static SnapShot DeserializeSnapShot(string data)
    {
        Trace.TraceInformation("Entering DeserializeSnapShot");
        SnapShot result = JsonConvert.DeserializeObject<SnapShot>(data, new JsonSerializerSettings {
            TypeNameHandling = TypeNameHandling.Auto
        });
        Trace.TraceInformation("Exiting DeserializeSnapShot");
        return result;
    }

    /// <summary>
    /// Deserializes a JSON string into an <see cref="IShape"/> object.
    /// </summary>
    /// <param name="data">The JSON string to deserialize.</param>
    /// <returns>An <see cref="IShape"/> object represented by the JSON string.</returns>
    /// <exception cref="NotSupportedException">Thrown when the shape type is not supported.</exception>
    public static IShape DeserializeShape(string data)
    {
        Trace.TraceInformation("Entering DeserializeShape");
        Dictionary<string, object>? shapeDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(data);
        string shapeType = shapeDict["ShapeType"].ToString();
        Trace.TraceInformation($"Deserializing shape of type: {shapeType}");

        IShape result = shapeType switch {
            "Circle" => JsonConvert.DeserializeObject<CircleShape>(data),
            "Line" => JsonConvert.DeserializeObject<LineShape>(data),
            "Scribble" => JsonConvert.DeserializeObject<ScribbleShape>(data),
            "TextShape" => JsonConvert.DeserializeObject<TextShape>(data),
            _ => throw new NotSupportedException("Shape type not supported"),
        };

        Trace.TraceInformation("Exiting DeserializeShape");
        return result;
    }

    /// <summary>
    /// Serializes an <see cref="ObservableCollection{IShape}"/> to a JSON string.
    /// </summary>
    /// <param name="shapes">The collection of shapes to serialize.</param>
    /// <returns>A JSON string representation of the shape collection.</returns>
    public static string SerializeShapes(ObservableCollection<IShape> shapes)
    {
        Trace.TraceInformation("Entering SerializeShapes");
        string result = JsonConvert.SerializeObject(shapes);
        Trace.TraceInformation("Exiting SerializeShapes");
        return result;
    }

    /// <summary>
    /// Deserializes a JSON string into an <see cref="ObservableCollection{IShape}"/>.
    /// </summary>
    /// <param name="data">The JSON string to deserialize.</param>
    /// <returns>An <see cref="ObservableCollection{IShape}"/> represented by the JSON string.</returns>
    /// <exception cref="NotSupportedException">Thrown when a shape type in the JSON is not supported.</exception>
    public static ObservableCollection<IShape> DeserializeShapes(string data)
    {
        Trace.TraceInformation("Entering DeserializeShapes");
        List<Dictionary<string, object>>? shapeList = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(data);
        var shapes = new ObservableCollection<IShape>();

        foreach (Dictionary<string, object> shapeDict in shapeList)
        {
            string shapeType = shapeDict["ShapeType"].ToString();
            Trace.TraceInformation($"Deserializing shape of type: {shapeType}");

            IShape shape = shapeType switch {
                "Circle" => JsonConvert.DeserializeObject<CircleShape>(JsonConvert.SerializeObject(shapeDict)),
                "Line" => JsonConvert.DeserializeObject<LineShape>(JsonConvert.SerializeObject(shapeDict)),
                "Scribble" => JsonConvert.DeserializeObject<ScribbleShape>(JsonConvert.SerializeObject(shapeDict)),
                "TextShape" => JsonConvert.DeserializeObject<TextShape>(JsonConvert.SerializeObject(shapeDict)),
                _ => throw new NotSupportedException($"Shape type '{shapeType}' not supported"),
            };

            shapes.Add(shape);
        }

        Trace.TraceInformation("Exiting DeserializeShapes");
        return shapes;
    }
}

