/******************************************************************************
* Filename    = TestServer.cs
*
* Author      = Amithabh A & Garima Ranjan
*
* Product     = Updater
* 
* Project     = Lab Monitoring Software
*
* Description = Unit Tests for Server.cs
*****************************************************************************/

using Networking.Communication;
using Updater;
using System.Diagnostics;
using Moq;
using System.Net.Sockets;
using System.Reflection;

namespace TestsUpdater;

[TestClass]
public class TestServer
{
    private static Mock<CommunicatorServer>? _mockCommunicator;
    private static Mock<Server>? _mockServer;
    private static StringWriter? _traceOutput;

    // <summary>
    // Initializes the test environment before any test methods are run.
    // </summary>
    [ClassInitialize]
    public static void ClassInit(TestContext context)
    {
        _mockCommunicator = new Mock<CommunicatorServer>();
        _mockServer = new Mock<Server>();

        _traceOutput = new StringWriter();
        Trace.Listeners.Clear();
        Trace.Listeners.Add(new TextWriterTraceListener(_traceOutput));
    }

    // <summary>
    // Initializes the test environment before each individual test method is run.
    // </summary>
    [TestInitialize]
    public void TestInitialize()
    {
        _traceOutput?.GetStringBuilder().Clear();
        Trace.Listeners.Add(new TextWriterTraceListener(_traceOutput));
    }

    // <summary>
    // Tests that the Server singleton pattern works correctly by checking that
    // multiple calls to GetServerInstance return the same instance.
    // </summary>
    [TestMethod]
    public void TestGetServerInstance_Singleton()
    {
        var server1 = Server.GetServerInstance();
        var server2 = Server.GetServerInstance();
        Assert.AreEqual(server1, server2, "Server instance is not singleton");
    }

    // <summary>
    // Tests that the UpdateUILogs method invokes the NotificationReceived event
    // when a log message is provided.
    // </summary>
    [TestMethod]
    public void TestUpdateUILogsShouldInvokeNotificationReceivedEvent()
    {
        string logMessage = "Test log message";
        bool eventInvoked = false;

        Server.NotificationReceived += (message) =>
        {
            if (message == logMessage)
            {
                eventInvoked = true;
            }
        };

        Server.UpdateUILogs(logMessage);

        Assert.IsTrue(eventInvoked);
    }

    // <summary>
    // Tests that calling SetUser correctly adds the client to the server's
    // clientConnections dictionary.
    // </summary>
    [TestMethod]
    public void TestSetUserAddsClientToConnections()
    {
        Server server = Server.GetServerInstance();
        string clientId = "client123";
        var mockTcpClient = new Mock<TcpClient>();
        FieldInfo? clientConnectionsField = typeof(Server).GetField("_clientConnections", BindingFlags.NonPublic | BindingFlags.Instance);
        var clientConnections = (Dictionary<string, TcpClient>?)clientConnectionsField?.GetValue(server);

        server.SetUser(clientId, mockTcpClient.Object);

        Assert.IsTrue(clientConnections?.ContainsKey(clientId) ?? false, "Client should be added to connections.");
    }

    // <summary>
    // Tests that OnClientLeft removes a client from the server's clientConnections
    // dictionary when the client leaves.
    // </summary>
    [TestMethod]
    public void TestOnClientLeftRemovesClientFromConnections()
    {
        Server server = Server.GetServerInstance();
        string clientId = "client123";
        var mockTcpClient = new Mock<TcpClient>();
        FieldInfo? clientConnectionsField = typeof(Server).GetField("_clientConnections", BindingFlags.NonPublic | BindingFlags.Instance);
        var clientConnections = (Dictionary<string, TcpClient>?)clientConnectionsField?.GetValue(server);

        server.SetUser(clientId, mockTcpClient.Object);

        server.OnClientLeft(clientId);

        Assert.IsFalse(clientConnections?.ContainsKey(clientId) ?? true, "Client should be removed from connections.");
    }

    // <summary>
    // Tests the SyncUp method by verifying that it sends a sync-up request
    // and logs the appropriate message.
    // </summary>
    [TestMethod]
    public void TestSyncUpSuccess()
    {
        Server server = Server.GetServerInstance();
        string clientId = "serverId";

        server.SyncUp(clientId);

        Assert.IsTrue(_traceOutput?.ToString().Contains($"[Updater] Sending SyncUp request dataPacket to client: {clientId}"),
                      "SyncUp did not send the sync-up request.");
    }

    // <summary>
    // Tests that the UpdateUILogs method triggers the NotificationReceived
    // event with the correct message.
    // </summary>
    [TestMethod]
    public void TestUpdateUILogsNotificationReceived()
    {
        string message = "Test log message";
        bool eventTriggered = false;

        Server.NotificationReceived += (msg) =>
        {
            if (msg == message)
            {
                eventTriggered = true;
            }
        };

        Server.UpdateUILogs(message);

        Assert.IsTrue(eventTriggered, "UpdateUILogs did not trigger the NotificationReceived event.");
    }

    // <summary>
    // Tests that the PacketDemultiplexer handles SyncUp packets when no client ID
    // is provided, and logs the appropriate error.
    // </summary>
    [TestMethod]
    public void TestPacketDemultiplexerSyncUpWithNoClientID()
    {
        Server server = Server.GetServerInstance();
        var dataPacket = new DataPacket(DataPacket.PacketType.SyncUp, []);
        string serializedData = Utils.SerializeObject(dataPacket)!;

        if (_mockCommunicator != null)
        {
            Server.PacketDemultiplexer(serializedData, _mockCommunicator.Object, server, "clientID1");

            Assert.IsTrue(_traceOutput?.ToString().Contains("[Updater] Error in SyncUpHandler: [Updater] No client ID received"),
                          "SyncUpHandler not called correctly");
        }
    }

    // <summary>
    // Tests the PacketDemultiplexer for a Metadata packet when no file content
    // is provided, and verifies the error logging.
    // </summary>
    [TestMethod]
    public void TestPacketDemultiplexerMetadataHandlerWithNoFiles()
    {
        Server server = Server.GetServerInstance();
        string clientId = "1";

        var mockTcpClient = new Mock<TcpClient>();
        FieldInfo? clientConnectionsField = typeof(Server).GetField("_clientConnections", BindingFlags.NonPublic | BindingFlags.Instance);
        var clientConnections = (Dictionary<string, TcpClient>?)clientConnectionsField?.GetValue(server);

        server.SetUser(clientId, mockTcpClient.Object);

        var dataPacket = new DataPacket(DataPacket.PacketType.Metadata, []);
        string serializedData = Utils.SerializeObject(dataPacket)!;

        if (_mockCommunicator != null)
        {
            Server.PacketDemultiplexer(serializedData, _mockCommunicator.Object, server, clientId);

            Assert.IsTrue(_traceOutput?.ToString().Contains("[Updater] Error sending data to client: No file content received in the data packet."),
                          "MetadataHandler was not called correctly");
        }
    }

    // <summary>
    // Tests the PacketDemultiplexer for a Metadata packet when files are provided,
    // verifying the correct processing of the metadata.
    // </summary>
    [TestMethod]
    public void TestPacketDemultiplexerMetadataHandlerWithFiles()
    {
        Server server = Server.GetServerInstance();
        string clientId = "1";

        var mockTcpClient = new Mock<TcpClient>();
        FieldInfo? clientConnectionsField = typeof(Server).GetField("_clientConnections", BindingFlags.NonPublic | BindingFlags.Instance);
        var clientConnections = (Dictionary<string, TcpClient>?)clientConnectionsField?.GetValue(server);

        server.SetUser(clientId, mockTcpClient.Object);
        string _testFolderPath = @"../../../TestsUpdater/TestingFolder";
        string testFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _testFolderPath);
        string? serializedData = Utils.SerializedMetadataPacket(testFolderPath);

        if (_mockCommunicator != null)
        {
            Server.PacketDemultiplexer(serializedData, _mockCommunicator.Object, server, clientId);

            Assert.IsTrue(_traceOutput?.ToString().Contains("[Updater]: Metadata from client received"),
                          "MetadataHandler was not called correctly");
            Assert.IsTrue(_traceOutput?.ToString().Contains("[Updater] Metadata from server generated"),
                          "MetadataHandler was not called correctly");
        }
    }

    // <summary>
    // Tests the PacketDemultiplexer for ClientFiles packets and ensures files
    // are processed and broadcasted correctly.
    // </summary>
    [TestMethod]
    public void TestPacketDemultiplexerClientFilesHandlerWithFiles()
    {
        Server server = Server.GetServerInstance();
        string clientId = "clientID1";

        var mockTcpClient = new Mock<TcpClient>();
        FieldInfo? clientConnectionsField = typeof(Server).GetField("_clientConnections", BindingFlags.NonPublic | BindingFlags.Instance);
        var clientConnections = (Dictionary<string, TcpClient>?)clientConnectionsField?.GetValue(server);

        server.SetUser(clientId, mockTcpClient.Object);

        var fileContent = new FileContent("test.txt", Utils.SerializeObject("test content")!);
        var fileContentList = new List<Updater.FileContent> { fileContent };

        var dataPacket = new DataPacket(DataPacket.PacketType.ClientFiles, fileContentList);
        string serializedData = Utils.SerializeObject(dataPacket)!;

        if (_mockCommunicator != null)
        {
            Server.PacketDemultiplexer(serializedData, _mockCommunicator.Object, server, clientId);

            Trace.Flush();
            Thread.Sleep(1000);

            Assert.IsTrue(_traceOutput?.ToString().Contains("[Updater] Successfully received client's files"),
                          "ClientFilesHandler was not called correctly");
            Assert.IsTrue(_traceOutput?.ToString().Contains("[Updater] Broadcasting the new files"),
                          "ClientFilesHandler was not called correctly");
        }
    }

    // <summary>
    // Cleans up the test environment after each test method is executed.
    // </summary>
    [TestCleanup]
    public void TestCleanup()
    {
        Trace.Flush();
        Trace.Listeners.Clear(); // Ensure listeners are fully cleared after each test
        _traceOutput?.GetStringBuilder().Clear();
    }

    // <summary>
    // Cleans up the test environment after all test methods have been executed.
    // </summary>
    [ClassCleanup]
    public static void ClassCleanup()
    {
        _mockCommunicator = null;
        _mockServer = null;
        _traceOutput = null;
    }
}
