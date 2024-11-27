using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhiteboardGUI.Models;

namespace WhiteboardGUI.Services;

/// <summary>
/// Service for handling received data and managing shape synchronization.
/// </summary>
public class ReceivedDataService
{
    /// <summary>
    /// List of synchronized shapes.
    /// </summary>
    public List<IShape> _synchronizedShapes = new();

    /// <summary>
    /// Event triggered when a shape is received.
    /// </summary>
    public event Action<IShape, bool> ShapeReceived;

    /// <summary>
    /// Event triggered when a shape is deleted.
    /// </summary>
    public event Action<IShape> ShapeDeleted;

    /// <summary>
    /// Event triggered when a shape is sent to the back.
    /// </summary>
    public event Action<IShape> ShapeSendToBack;

    /// <summary>
    /// Event triggered when a shape is sent backward.
    /// </summary>
    public event Action<IShape> ShapeSendBackward;

    /// <summary>
    /// Event triggered when a shape is modified.
    /// </summary>
    public event Action<IShape> ShapeModified;

    /// <summary>
    /// Event triggered when a shape is unlocked.
    /// </summary>
    public event Action<IShape> ShapeUnlocked;

    /// <summary>
    /// Event triggered when a shape is locked.
    /// </summary>
    public event Action<IShape> ShapeLocked;

    /// <summary>
    /// Event triggered when all shapes are cleared.
    /// </summary>
    public event Action ShapesClear;

    /// <summary>
    /// The ID of the current service instance.
    /// </summary>
    private int _id;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReceivedDataService"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the instance.</param>
    public ReceivedDataService(int id)
    {
        Trace.TraceInformation($"Initializing ReceivedDataService with ID: {id}");
        _id = id;
        Trace.TraceInformation("ReceivedDataService initialized successfully");
    }

    /// <summary>
    /// Processes received data and triggers appropriate events based on the message content.
    /// </summary>
    /// <param name="receivedData">The data received as a string.</param>
    /// <returns>Returns 0 on successful processing, or -1 if the data is invalid or from the same sender.</returns>
    public int DataReceived(string receivedData)
    {
        Trace.TraceInformation("Entering DataReceived");

        if (receivedData == null)
        {
            Trace.TraceWarning("DataReceived: Received null data");
            return -1;
        }

        int index = receivedData.IndexOf("END");
        if (index < 0)
        {
            Trace.TraceWarning("DataReceived: 'END' marker not found in data");
            return -1;
        }

        int senderId = int.Parse(receivedData.Substring(2, index - 2));
        if (senderId == _id)
        {
            Trace.TraceInformation($"DataReceived: Ignoring data from the same sender (ID: {senderId})");
            return -1;
        }

        receivedData = receivedData.Substring(index + "END".Length);

        try
        {
            if (receivedData.StartsWith("DELETE:"))
            {
                Trace.TraceInformation("DataReceived: Processing DELETE command");
                string data = receivedData.Substring(7);
                IShape shape = SerializationService.DeserializeShape(data);
                if (shape != null)
                {
                    IShape? currentShape = _synchronizedShapes
                        .Where(s => s.ShapeId == shape.ShapeId && s.UserID == shape.UserID)
                        .FirstOrDefault();
                    if (currentShape != null)
                    {
                        Trace.TraceInformation($"DataReceived: Deleting shape with ID {shape.ShapeId}");
                        ShapeDeleted?.Invoke(currentShape);
                    }
                }
            }
            else if (receivedData.StartsWith("CLEAR:"))
            {
                Trace.TraceInformation("DataReceived: Processing CLEAR command");
                ShapesClear?.Invoke();
            }
            else if (receivedData.StartsWith("INDEX-BACK:"))
            {
                Trace.TraceInformation("DataReceived: Processing INDEX-BACK command");
                string data = receivedData.Substring(11);
                IShape shape = SerializationService.DeserializeShape(data);
                if (shape != null)
                {
                    IShape? currentShape = _synchronizedShapes
                        .Where(s => s.ShapeId == shape.ShapeId && s.UserID == shape.UserID)
                        .FirstOrDefault();
                    if (currentShape != null)
                    {
                        Trace.TraceInformation($"DataReceived: Sending shape with ID {shape.ShapeId} to back");
                        ShapeSendToBack?.Invoke(currentShape);
                    }
                }
            }
            else if (receivedData.StartsWith("INDEX-BACKWARD:"))
            {
                Trace.TraceInformation("DataReceived: Processing INDEX-BACKWARD command");
                string data = receivedData.Substring(15);
                IShape shape = SerializationService.DeserializeShape(data);
                if (shape != null)
                {
                    IShape? currentShape = _synchronizedShapes
                        .Where(s => s.ShapeId == shape.ShapeId && s.UserID == shape.UserID)
                        .FirstOrDefault();
                    if (currentShape != null)
                    {
                        Trace.TraceInformation($"DataReceived: Sending shape with ID {shape.ShapeId} backward");
                        ShapeSendBackward?.Invoke(currentShape);
                    }
                }
            }
            else if (receivedData.StartsWith("MODIFY:"))
            {
                Trace.TraceInformation("DataReceived: Processing MODIFY command");
                string data = receivedData.Substring(7);
                IShape shape = SerializationService.DeserializeShape(data);
                if (shape != null)
                {
                    Trace.TraceInformation($"DataReceived: Modifying shape with ID {shape.ShapeId}");
                    ShapeModified?.Invoke(shape);
                }
            }
            else if (receivedData.StartsWith("CREATE:"))
            {
                Trace.TraceInformation("DataReceived: Processing CREATE command");
                string data = receivedData.Substring(7);
                IShape shape = SerializationService.DeserializeShape(data);
                if (shape != null)
                {
                    Trace.TraceInformation($"DataReceived: Creating shape with ID {shape.ShapeId}");
                    ShapeReceived?.Invoke(shape, true);
                }
            }
            else if (receivedData.StartsWith("DOWNLOAD:"))
            {
                Trace.TraceInformation("DataReceived: Processing DOWNLOAD command");
                string data = receivedData.Substring(9);
                IShape shape = SerializationService.DeserializeShape(data);
                if (shape != null)
                {
                    Trace.TraceInformation($"DataReceived: Downloading shape with ID {shape.ShapeId}");
                    ShapeReceived?.Invoke(shape, false);
                }
            }
            else if (receivedData.StartsWith("UNLOCK:"))
            {
                Trace.TraceInformation("DataReceived: Processing UNLOCK command");
                string data = receivedData.Substring(7);
                IShape shape = SerializationService.DeserializeShape(data);
                if (shape != null)
                {
                    IShape? existingShape = _synchronizedShapes
                        .FirstOrDefault(s => s.ShapeId == shape.ShapeId);
                    if (existingShape != null)
                    {
                        Trace.TraceInformation($"DataReceived: Unlocking shape with ID {shape.ShapeId}");
                        existingShape.IsLocked = false;
                        existingShape.LockedByUserID = -1;
                        ShapeUnlocked?.Invoke(existingShape);
                    }
                }
            }
            else if (receivedData.StartsWith("LOCK:"))
            {
                Trace.TraceInformation("DataReceived: Processing LOCK command");
                string data = receivedData.Substring(5);
                IShape shape = SerializationService.DeserializeShape(data);
                if (shape != null)
                {
                    IShape? existingShape = _synchronizedShapes
                        .FirstOrDefault(s => s.ShapeId == shape.ShapeId);
                    if (existingShape != null)
                    {
                        Trace.TraceInformation($"DataReceived: Locking shape with ID {shape.ShapeId}");
                        existingShape.IsLocked = true;
                        existingShape.LockedByUserID = shape.LockedByUserID;
                        ShapeLocked?.Invoke(existingShape);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Trace.TraceError($"DataReceived: Exception occurred - {ex.Message}");
            return -1;
        }

        Trace.TraceInformation("Exiting DataReceived");
        return 0;
    }
}
