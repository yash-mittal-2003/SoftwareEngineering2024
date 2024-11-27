using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using WhiteboardGUI.Models;
using System.Diagnostics;

public class ShapeConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        Trace.TraceInformation($"Entering CanConvert with objectType: {objectType}");
        bool result = typeof(IShape).IsAssignableFrom(objectType) || objectType == typeof(ObservableCollection<IShape>);
        Trace.TraceInformation($"Exiting CanConvert with result: {result}");
        return result;
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        Trace.TraceInformation("Entering ReadJson");

        if (reader.TokenType == JsonToken.StartArray)
        {
            Trace.TraceInformation("Detected JSON array, processing shapes");
            var shapes = new ObservableCollection<IShape>();
            var shapeArray = JArray.Load(reader);

            foreach (JToken shapeDict in shapeArray)
            {
                string shapeType = shapeDict["ShapeType"]?.ToString();
                Trace.TraceInformation($"Deserializing shape of type: {shapeType}");

                IShape shape = shapeType switch {
                    "Circle" => shapeDict.ToObject<CircleShape>(),
                    "Line" => shapeDict.ToObject<LineShape>(),
                    "Scribble" => shapeDict.ToObject<ScribbleShape>(),
                    "TextShape" => shapeDict.ToObject<TextShape>(),
                    "TextboxModel" => shapeDict.ToObject<TextboxModel>(),
                    _ => throw new NotSupportedException($"Shape type '{shapeType}' not supported"),
                };

                shapes.Add(shape);
                Trace.TraceInformation($"Successfully deserialized and added shape of type: {shapeType}");
            }

            Trace.TraceInformation("Exiting ReadJson with deserialized shapes");
            return shapes;
        }

        Trace.TraceError("ReadJson encountered a JSON structure that is not an array");
        throw new JsonReaderException("Expected a JSON array for Shapes.");
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        Trace.TraceInformation("Entering WriteJson");
        serializer.Serialize(writer, value);
        Trace.TraceInformation("Exiting WriteJson");
    }
}
