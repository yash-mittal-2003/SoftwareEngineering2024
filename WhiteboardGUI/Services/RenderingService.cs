﻿/**************************************************************************************************
 * Filename    = RenderingService.cs
 *
 * Authors     = Likith Anaparty
 *
 * Product     = WhiteBoard
 * 
 * Project     = Rendering shapes
 *
 * Description = Helps in rendering shapes sent over the network on the whiteboard
 *************************************************************************************************/


using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using WhiteboardGUI.Models;

namespace WhiteboardGUI.Services;

/// <summary>
/// Manages rendering operations for the whiteboard application, including shape creation,
/// modification, deletion, and synchronization across clients.
/// </summary>
public class RenderingService
{
    /// <summary>
    /// Service responsible for network communications and shape synchronization.
    /// </summary>
    NetworkingService _networkingService;

    /// <summary>
    /// Service that manages undo and redo operations for shape manipulations.
    /// </summary>
    UndoRedoService _undoRedoService;

    /// <summary>
    /// Collection of shapes currently present on the whiteboard.
    /// </summary>
    ObservableCollection<IShape> _shapes;

    /// <summary>
    /// Unique identifier for the current user.
    /// </summary>
    double _userId;
    string _name;

    /// <summary>
    /// Initializes a new instance of the <see cref="RenderingService"/> class.
    /// </summary>
    /// <param name="networkingService">The networking service for communication.</param>
    /// <param name="undoRedoService">The service managing undo and redo operations.</param>
    /// <param name="shapes">The collection of shapes on the whiteboard.</param>
    /// <param name="userID">The unique identifier of the user.</param>
    public RenderingService(NetworkingService networkingService, UndoRedoService undoRedoService, ObservableCollection<IShape> shapes, double userID, string name)
    {
        Trace.TraceInformation("Initializing RenderingService");
        _networkingService = networkingService;
        _undoRedoService = undoRedoService;
        _shapes = shapes;
        _userId = userID;
        _name = name;
        Trace.TraceInformation("RenderingService initialized successfully");
    }

    /// <summary>
    /// Updates the synchronized shapes by replacing the existing shape with the new one.
    /// </summary>
    /// <param name="shape">The new shape to synchronize.</param>
    /// <returns>The previous version of the shape, if it existed; otherwise, null.</returns>
    private IShape UpdateSynchronizedShapes(IShape shape)
    {
        Trace.TraceInformation("Entering UpdateSynchronizedShapes");
        IShape? prevShape = _networkingService._synchronizedShapes.Where(s => s.ShapeId == shape.ShapeId && s.UserID == shape.UserID).FirstOrDefault();
        _networkingService._synchronizedShapes.Remove(prevShape);
        _networkingService._synchronizedShapes.Add(shape);
        Trace.TraceInformation("Exiting UpdateSynchronizedShapes");
        return prevShape;
    }

    /// <summary>
    /// Renders the specified shape based on the given command.
    /// </summary>
    /// <param name="currentShape">The shape to render or manipulate.</param>
    /// <param name="command">The command indicating the action to perform (e.g., CREATE, MODIFY, DELETE).</param>
    public virtual void RenderShape(IShape currentShape, string command)
    {
        Trace.TraceInformation($"Entering RenderShape with command: {command}");

        try
        {
            if (command == "CREATE")
            {
                Trace.TraceInformation("Processing CREATE command");
                IShape newShape = currentShape.Clone();
                _networkingService._synchronizedShapes.Add(newShape);
                newShape.IsSelected = false;
                _undoRedoService.UpdateLastDrawing(newShape, null);
            }
            else if (command.StartsWith("INDEX"))
            {
                Trace.TraceInformation("Processing INDEX command");
                _networkingService._synchronizedShapes.Clear();
                foreach (IShape shape in _shapes)
                {
                    _networkingService._synchronizedShapes.Add(shape);
                }
            }
            else if (command == "DOWNLOAD")
            {
                Trace.TraceInformation("Processing DOWNLOAD command");
                IShape newShape = currentShape.Clone();
                _networkingService._synchronizedShapes.Add(newShape);
                newShape.IsSelected = false;
            }
            else if (command == "MODIFY")
            {
                Trace.TraceInformation("Processing MODIFY command");
                IShape newShape = currentShape.Clone();
                IShape prevShape = UpdateSynchronizedShapes(newShape);
                newShape.IsSelected = false;
                _undoRedoService.UpdateLastDrawing(newShape, prevShape);
            }
            else if (command == "CLEAR")
            {
                Trace.TraceInformation("Processing CLEAR command");
                _shapes.Clear();
                _undoRedoService._undoList.Clear();
                _undoRedoService._redoList.Clear();
                _networkingService._synchronizedShapes.Clear();
                string clearMessage = $"ID{_userId}{command}:";
                Debug.WriteLine(clearMessage);
                _networkingService.BroadcastShapeData(clearMessage);
                Trace.TraceInformation("Exiting RenderShape after CLEAR command");
                return;
            }
            else if (command == "UNDO")
            {
                Trace.TraceInformation("Processing UNDO command");
                IShape prevShape = _undoRedoService._undoList[_undoRedoService._undoList.Count - 1].Item1;
                IShape currentShapeRendered = _undoRedoService._undoList[_undoRedoService._undoList.Count - 1].Item2;
                if (currentShapeRendered == null)
                {
                    Trace.TraceInformation("Undoing CREATE operation");
                    currentShape = prevShape;
                    foreach (IShape s in _shapes)
                    {
                        if (s.ShapeId == currentShape.ShapeId)
                        {
                            _shapes.Remove(s);
                            break;
                        }
                    }
                    _networkingService._synchronizedShapes.Remove(currentShape);
                    prevShape.LastModifierID = _networkingService._clientID;
                    command = "DELETE";
                }
                else if (prevShape == null)
                {
                    Trace.TraceInformation("Undoing DELETE operation");
                    currentShape = currentShapeRendered;
                    IShape newShape = currentShape.Clone();
                    _shapes.Add(newShape);
                    _networkingService._synchronizedShapes.Add(newShape);
                    command = "CREATE";
                }
                else
                {
                    Trace.TraceInformation("Undoing MODIFY operation");
                    currentShape = currentShapeRendered;
                    IShape newShape = currentShape.Clone();
                    UpdateSynchronizedShapes(newShape);
                    foreach (IShape s in _shapes)
                    {
                        if (s.ShapeId == prevShape.ShapeId)
                        {
                            _shapes.Remove(s);
                            break;
                        }
                    }
                    _shapes.Add(currentShape);
                    currentShapeRendered.LastModifierID = _networkingService._clientID;
                    command = "MODIFY";
                }
                _undoRedoService.Undo();
            }
            else if (command == "REDO")
            {
                Trace.TraceInformation("Processing REDO command");
                IShape prevShape = _undoRedoService._redoList[_undoRedoService._redoList.Count - 1].Item1;
                IShape currentShapeRendered = _undoRedoService._redoList[_undoRedoService._redoList.Count - 1].Item2;
                if (currentShapeRendered == null)
                {
                    Trace.TraceInformation("Redoing CREATE operation");
                    currentShape = prevShape;
                    foreach (IShape s in _shapes)
                    {
                        if (s.ShapeId == prevShape.ShapeId)
                        {
                            _shapes.Remove(s);
                            break;
                        }
                    }
                    _networkingService._synchronizedShapes.Remove(currentShape);
                    prevShape.LastModifierID = _networkingService._clientID;
                    command = "DELETE";
                }
                else if (prevShape == null)
                {
                    Trace.TraceInformation("Redoing DELETE operation");
                    currentShape = currentShapeRendered;
                    IShape newShape = currentShape.Clone();
                    _shapes.Add(newShape);
                    _networkingService._synchronizedShapes.Add(newShape);
                    command = "CREATE";
                }
                else
                {
                    Trace.TraceInformation("Redoing MODIFY operation");
                    currentShape = currentShapeRendered;
                    IShape newShape = currentShape.Clone();
                    UpdateSynchronizedShapes(newShape);
                    foreach (IShape s in _shapes)
                    {
                        if (s.ShapeId == currentShape.ShapeId)
                        {
                            _shapes.Remove(s);
                            break;
                        }
                    }
                    _shapes.Add(currentShape);
                    currentShapeRendered.LastModifierID = _networkingService._clientID;
                    command = "MODIFY";
                }
                _undoRedoService.Redo();
            }
            else if (command == "DELETE")
            {
                Trace.TraceInformation("Processing DELETE command");
                _shapes.Remove(currentShape);
                _networkingService._synchronizedShapes.Remove(currentShape);
                _undoRedoService.UpdateLastDrawing(null, currentShape);
            }

            currentShape.LastModifierID = _networkingService._clientID;
            currentShape.LastModifiedBy = _name;
            string serializedShape = SerializationService.SerializeShape(currentShape);
            string serializedMessage = $"ID{_userId}END{command}:{serializedShape}";
            Debug.WriteLine(serializedMessage);
            _networkingService.BroadcastShapeData(serializedMessage);
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Error in RenderShape: {ex.Message}");
        }

        Trace.TraceInformation("Exiting RenderShape");
    }
}
