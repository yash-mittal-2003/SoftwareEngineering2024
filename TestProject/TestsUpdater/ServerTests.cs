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

    // Test initialization
    [ClassInitialize]
    public static void ClassInit(TestContext context)
    {
        _mockCommunicator = new Mock<CommunicatorServer>();
        _mockServer = new Mock<Server>();

        _traceOutput = new StringWriter();
        Trace.Listeners.Clear();
        Trace.Listeners.Add(new TextWriterTraceListener(_traceOutput));
    }

    [TestInitialize]
    public void TestInitialize()
    {
        _traceOutput?.GetStringBuilder().Clear();
    }

    [TestMethod]
    public void TestGetServerInstance_Singleton()
    {
        var server1 = Server.GetServerInstance();
        var server2 = Server.GetServerInstance();
        Assert.AreEqual(server1, server2, "Server instance is not singleton");
    }

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

    [TestMethod]
    public void TestSyncUpSuccess()
    {
        Server server = Server.GetServerInstance();
        string clientId = "serverId";

        server.SyncUp(clientId);

        Assert.IsTrue(_traceOutput?.ToString().Contains($"[Updater] Sending SyncUp request dataPacket to client: {clientId}"),
                      "SyncUp did not send the sync-up request.");
    }

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

    [TestMethod]
    public void TestPacketDemultiplexerSyncUpWithNoClientID()
    {
        Server server = Server.GetServerInstance();
        var dataPacket = new DataPacket(DataPacket.PacketType.SyncUp, []);
        string serializedData = Utils.SerializeObject(dataPacket)!;

        if (_mockCommunicator != null)
        {
            Server.PacketDemultiplexer(serializedData, _mockCommunicator.Object, server, "clientID");

            Assert.IsTrue(_traceOutput?.ToString().Contains("[Updater] Error in SyncUpHandler: [Updater] No client ID received"),
                          "SyncUpHandler not called correctly");
        }
    }

    [TestMethod]
    public void TestPacketDemultiplexerSyncUpWithClientID()
    {
        Server server = Server.GetServerInstance();
        string clientId = "1";

        var mockTcpClient = new Mock<TcpClient>();
        FieldInfo? clientConnectionsField = typeof(Server).GetField("_clientConnections", BindingFlags.NonPublic | BindingFlags.Instance);
        var clientConnections = (Dictionary<string, TcpClient>?)clientConnectionsField?.GetValue(server);

        server.SetUser(clientId, mockTcpClient.Object);

        Assert.IsTrue(clientConnections?.ContainsKey(clientId) ?? false, "Client should be added to connections.");

        List<FileContent> fileContents = new List<FileContent>
        {
            new FileContent(clientId, clientId)
        };

        var dataPacket = new DataPacket(DataPacket.PacketType.SyncUp, fileContents);
        string serializedData = Utils.SerializeObject(dataPacket)!;
        if (_mockCommunicator != null)
        {
            Server.PacketDemultiplexer(serializedData, _mockCommunicator.Object, server, "1");
            Thread.Sleep(1000);
            Trace.Flush();
            Assert.IsTrue(_traceOutput?.ToString().Contains("[Updater] Sending SyncUp request dataPacket to client: 1"),
                          "SyncUpHandler not called correctly");
        }
    }

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

    [TestMethod]
    public void TestPacketDemultiplexerClientFilesHandlerWithFiles()
    {
        Server server = Server.GetServerInstance();
        string clientId = "1";

        var mockTcpClient = new Mock<TcpClient>();
        FieldInfo? clientConnectionsField = typeof(Server).GetField("_clientConnections", BindingFlags.NonPublic | BindingFlags.Instance);
        var clientConnections = (Dictionary<string, TcpClient>?)clientConnectionsField?.GetValue(server);

        server.SetUser(clientId, mockTcpClient.Object);

        var fileContent = new FileContent("test.txt", Utils.SerializeObject("test content")!);
        // Create an empty list of FileContent objects
        var fileContentList = new List<Updater.FileContent>();
        fileContentList.Add(fileContent);

        var dataPacket = new DataPacket(DataPacket.PacketType.ClientFiles, fileContentList);
        string serializedData = Utils.SerializeObject(dataPacket)!;

        if (_mockCommunicator != null)
        {
            Server.PacketDemultiplexer(serializedData, _mockCommunicator.Object, server, clientId);

            Assert.IsTrue(_traceOutput?.ToString().Contains("[Updater] Successfully received client's files"),
                          "ClientFilesHandler was not called correctly");
            Assert.IsTrue(_traceOutput?.ToString().Contains("[Updater] Broadcasting the new files"),
                          "ClientFilesHandler was not called correctly");
        }
    }

    [TestCleanup]
    public void TestCleanup()
    {
        Trace.Flush();
        _traceOutput?.GetStringBuilder().Clear();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _mockCommunicator = null;
        _mockServer = null;
        _traceOutput = null;
    }
}