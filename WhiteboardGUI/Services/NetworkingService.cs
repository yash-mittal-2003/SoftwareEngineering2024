/**************************************************************************************************
 * Filename    = NetworkingService.cs
 *
 * Author      = Likith Anaparty
 *
 * Product     = WhiteBoard
 * 
 * Project     = Networking module for transferring the shape
 *
 * Description = The logic that is invoked in the networking layer when whiteboard is invoke
 *************************************************************************************************/

using System.Diagnostics;
using WhiteboardGUI.Models;
using Networking.Communication;
using Networking;

namespace WhiteboardGUI.Services;

/// <summary>
/// Provides networking functionalities for the whiteboard application, enabling hosting, 
/// client connections, and data synchronization between clients.
/// </summary>
public class NetworkingService : INotificationHandler
{
    /// <summary>
    /// The unique identifier of the client.
    /// </summary>
    public double _clientID;

    /// <summary>
    /// Indicates whether the current instance is acting as the host.
    /// </summary>
    private bool _isHost;

    /// <summary>
    /// The communication interface used for sending and receiving data.
    /// </summary>
    public ICommunicator? _communicator;

    /// <summary>
    /// Service to handle received data and maintain synchronized shapes.
    /// </summary>
    public ReceivedDataService _receivedDataService;

    /// <summary>
    /// List of shapes synchronized across all clients.
    /// </summary>
    public List<IShape> _synchronizedShapes;

    /// <summary>
    /// Module identifier for communication purposes.
    /// </summary>
    private string _moduleIdentifier = "WhiteBoard";

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkingService"/> class.
    /// </summary>
    /// <param name="dataTransferService">Service responsible for data synchronization.</param>
    public NetworkingService(ReceivedDataService dataTransferService)
    {
        Trace.TraceInformation("Entering NetworkingService constructor");
        _receivedDataService = dataTransferService;
        _synchronizedShapes = dataTransferService._synchronizedShapes;
        Trace.TraceInformation("Exiting NetworkingService constructor");
    }

    /// <summary>
    /// Starts the service as a host server.
    /// </summary>
    public void StartHost()
    {
        Trace.TraceInformation("Entering StartHost");
        StartHostServer();
        Trace.TraceInformation("Exiting StartHost");
    }

    /// <summary>
    /// Configures and starts the host server.
    /// </summary>
    private void StartHostServer()
    {
        Trace.TraceInformation("Entering StartHostServer");
        try
        {
            _communicator = CommunicationFactory.GetCommunicator(false);
            _communicator.Subscribe(_moduleIdentifier, this, false);
            _communicator.Start();
            _isHost = true;
            Trace.TraceInformation("Host server started successfully");
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Host server error: {ex.Message}");
        }
        Trace.TraceInformation("Exiting StartHostServer");
    }

    /// <summary>
    /// Starts the service as a client server.
    /// </summary>
    public void StartClient()
    {
        Trace.TraceInformation("Entering StartClient");
        StartClientServer();
        Trace.TraceInformation("Exiting StartClient");
    }

    /// <summary>
    /// Configures and starts the client server.
    /// </summary>
    private void StartClientServer()
    {
        Trace.TraceInformation("Entering StartClientServer");
        try
        {
            _communicator = CommunicationFactory.GetCommunicator(true);
            _communicator.Subscribe(_moduleIdentifier, this, false);
            _communicator.Start();
            _isHost = false;
            Trace.TraceInformation("Client server started successfully");
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Client server error: {ex.Message}");
        }
        Trace.TraceInformation("Exiting StartClientServer");
    }

    /// <summary>
    /// Handles data received from the communicator and processes it based on the instance role (host or client).
    /// </summary>
    /// <param name="serializedData">The serialized data received.</param>
    public void OnDataReceived(string serializedData)
    {
        Trace.TraceInformation("Entering OnDataReceived");
        try
        {
            if (_isHost)
            {
                Trace.TraceInformation("Processing data as host");
                int id = _receivedDataService.DataReceived(serializedData);
                if (id == -1)
                {
                    Trace.TraceWarning("Data received is invalid, skipping broadcast");
                    return;
                }
                BroadcastShapeData(serializedData);
                Trace.TraceInformation("Broadcasted data successfully");
            }
            else
            {
                Trace.TraceInformation("Processing data as client");
                _receivedDataService.DataReceived(serializedData);
            }
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Error processing received data: {ex.Message}");
        }
        Trace.TraceInformation("Exiting OnDataReceived");
    }

    /// <summary>
    /// Broadcasts serialized shape data to all connected clients.
    /// </summary>
    /// <param name="serializedData">The serialized shape data to broadcast.</param>
    public void BroadcastShapeData(string serializedData)
    {
        Trace.TraceInformation("Entering BroadcastShapeData");
        try
        {
            _communicator.Send(serializedData, _moduleIdentifier, null);
            Trace.TraceInformation("BroadcastShapeData: Data sent successfully");
        }
        catch (Exception ex)
        {
            Trace.TraceError($"Error broadcasting shape data: {ex.Message}");
        }
        Trace.TraceInformation("Exiting BroadcastShapeData");
    }

    /// <summary>
    /// Stops the host server.
    /// </summary>
    public void StopHost()
    {
        Trace.TraceInformation("Entering StopHost");
        _communicator?.Stop();
        Trace.TraceInformation("Host server stopped");
        Trace.TraceInformation("Exiting StopHost");
    }

    /// <summary>
    /// Stops the client.
    /// </summary>
    public void StopClient()
    {
        Trace.TraceInformation("Entering StopClient");
        _communicator?.Stop();
        Trace.TraceInformation("Client server stopped");
        Trace.TraceInformation("Exiting StopClient");
    }
}

