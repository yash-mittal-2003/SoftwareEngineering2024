using System.Diagnostics;
using Moq;
using Updater;
using Networking.Communication;

namespace TestsUpdater;

[TestClass]
public class ClientTests
{
    private static Mock<CommunicatorClient>? _mockCommunicator;
    private static Mock<Client>? _mockClient;
    private static StringWriter? _traceOutput;

    // Test initialization
    [ClassInitialize]
    public static void ClassInit(TestContext context)
    {
        _mockCommunicator = new Mock<CommunicatorClient>();
        _mockClient = new Mock<Client>();

        _traceOutput = new StringWriter();
        Trace.Listeners.Clear();
        Trace.Listeners.Add(new TextWriterTraceListener(_traceOutput));

        // Setup mock behaviors
        _mockClient.Setup(client => client.GetClientId(It.IsAny<string>())).Callback<string>(id => Trace.WriteLine($"Client ID: {id}"));
        _mockClient.Setup(client => client.SyncUp()).Callback(() => Trace.WriteLine("Sending syncup request to the server"));
    }

    [TestInitialize]
    public void TestInitialize()
    {
        _traceOutput?.GetStringBuilder().Clear();
    }

    [TestMethod]
    public void TestGetClientInstanceSingleton()
    {
        var client1 = Client.GetClientInstance();
        var client2 = Client.GetClientInstance();
        Assert.AreEqual(client1, client2, "Client instance is not singleton");
    }

    [TestMethod]
    public void TestPacketDemultiplexerInvalidSync()
    {
        var fileContent = new FileContent("invalid.txt", Utils.SerializeObject(new List<string> { "file1.txt" })!);
        // Create an empty list of FileContent objects
        var fileContentList = new List<Updater.FileContent>();
        fileContentList.Add(fileContent);
        var dataPacket = new DataPacket(DataPacket.PacketType.InvalidSync, fileContentList);
        string serializedData = Utils.SerializeObject(dataPacket)!;

        if (_mockCommunicator != null)
        {
            Client.PacketDemultiplexer(serializedData, _mockCommunicator.Object);

            Assert.IsTrue(_traceOutput?.ToString().Contains("Received invalid file names from server"),
                          "InvalidSyncHandler was not called correctly");
        }
    }

    [TestMethod]
    public void TestPacketDemultiplexerBroadcast()
    {
        var fileContent = new FileContent("test.txt", Utils.SerializeObject("test content")!);
        // Create an empty list of FileContent objects
        var fileContentList = new List<Updater.FileContent>();
        fileContentList.Add(fileContent);
        var dataPacket = new DataPacket(DataPacket.PacketType.Broadcast, fileContentList);
        string serializedData = Utils.SerializeObject(dataPacket)!;

        if (_mockCommunicator != null)
        {
            Client.PacketDemultiplexer(serializedData, _mockCommunicator.Object);

            Assert.IsTrue(_traceOutput?.ToString().Contains("Up-to-date with the server"),
                          "BroadcastHandler was not called correctly");
        }
    }

    [TestMethod]
    public void TestPacketDemultiplexerSyncUp()
    {
        var dataPacket = new DataPacket(DataPacket.PacketType.SyncUp, []);
        string serializedData = Utils.SerializeObject(dataPacket)!;

        if (_mockCommunicator != null)
        {
            Client.PacketDemultiplexer(serializedData, _mockCommunicator.Object);

            Assert.IsTrue(_traceOutput?.ToString().Contains("[Updater] Metadata sent to server"),
                          "SyncUpHandler not called correctly");
        }
    }

    [TestMethod]
    public void TestPacketDemultiplexerDifferences()
    {
        // Create a Differences packet with metadata differences and file content
        var diffContent = new FileContent("differences", Utils.SerializeObject(new List<MetadataDifference>())!);
        var fileContent = new FileContent("file.txt", Utils.SerializeObject("content")!);
        var dataPacket = new DataPacket(DataPacket.PacketType.Differences, [diffContent, fileContent]);
        string serializedData = Utils.SerializeObject(dataPacket)!;

        // Call the PacketDemultiplexer
        if (_mockCommunicator != null)
        {
            Client.PacketDemultiplexer(serializedData, _mockCommunicator.Object);

            // Assert that the DifferencesHandler is called by checking the trace output
            Assert.IsTrue(_traceOutput?.ToString().Contains("Sending files to server"),
                          "DifferencesHandler was not called correctly");
        }
    }

    [TestMethod]
    public void TestSyncUpHandler()
    {
        // Create an empty list of FileContent objects
        var fileContentList = new List<Updater.FileContent>();

        var syncUpPacket = new DataPacket(DataPacket.PacketType.SyncUp, fileContentList);
        if (_mockCommunicator != null)
        {
            Client.SyncUpHandler(_mockCommunicator.Object);

            Assert.IsTrue(_traceOutput?.ToString().Contains("Metadata sent to server"),
                          "SyncUpHandler did not send metadata");
        }
    }

    [TestMethod]
    public void TestInvalidSyncHandler()
    {
        var fileContent = new FileContent("invalid.txt", Utils.SerializeObject(new List<string> { "file1.txt" })!);
        // Create an empty list of FileContent objects
        var fileContentList = new List<Updater.FileContent>();
        fileContentList.Add(fileContent);
        var dataPacket = new DataPacket(DataPacket.PacketType.InvalidSync, fileContentList);

        if (_mockCommunicator != null)
        {
            Client.InvalidSyncHandler(dataPacket);

            Assert.IsTrue(_traceOutput?.ToString().Contains("Received invalid file names from server"),
                          "InvalidSyncHandler did not log expected message");
        }
    }

    [TestMethod]
    public void TestBroadcastHandler()
    {
        var fileContent = new FileContent("test.txt", Utils.SerializeObject("test content")!);
        // Create an empty list of FileContent objects
        var fileContentList = new List<Updater.FileContent>();
        fileContentList.Add(fileContent);
        var dataPacket = new DataPacket(DataPacket.PacketType.Broadcast, fileContentList);

        if (_mockCommunicator != null)
        {
            Client.BroadcastHandler(dataPacket);

            Assert.IsTrue(_traceOutput?.ToString().Contains("Up-to-date with the server"),
                          "BroadcastHandler did not update correctly");
        }
    }

    [TestMethod]
    public void TestDifferencesHandler()
    {
        var diffContent = new FileContent("differences", Utils.SerializeObject(new List<MetadataDifference>())!);
        var fileContent = new FileContent("file.txt", Utils.SerializeObject("content")!);
        // Create an empty list of FileContent objects
        var fileContentList = new List<Updater.FileContent>();
        fileContentList.Add(diffContent);
        fileContentList.Add(fileContent);
        var dataPacket = new DataPacket(DataPacket.PacketType.Differences, fileContentList);

        if (_mockCommunicator != null)
        {
            Client.DifferencesHandler(dataPacket, _mockCommunicator.Object);

            Assert.IsTrue(_traceOutput?.ToString().Contains("Sending files to server"),
                          "DifferencesHandler did not send files correctly");
        }
    }

    [TestMethod]
    public void TestShowInvalidFilesInUI()
    {
        Client.ShowInvalidFilesInUI(new[] { "file1.txt", "file2.txt" }.ToList());
        Assert.IsTrue(_traceOutput?.ToString().Contains("Invalid filenames"),
                      "ShowInvalidFilesInUI did not log expected message");
        Assert.IsTrue(_traceOutput?.ToString().Contains("Sync up failed"),
                      "ShowInvalidFilesInUI did not log expected message");
    }

    [TestMethod]
    public void TestPacketDemultiplexerEmptyData()
    {
        if (_mockCommunicator != null)
        {
            Client.PacketDemultiplexer(string.Empty, _mockCommunicator.Object);

            Assert.IsTrue(_traceOutput?.ToString().Contains("[Updater] Error in PacketDemultiplexer: Object reference not set to an instance of an object.\r\n"),
                          "PacketDemultiplexer did not handle empty data correctly");
        }
    }

    [TestMethod]
    public void TestGetClientId()
    {
        // Arrange
        var clientId = "testClientId";
        var client = Client.GetClientInstance();

        // Act
        client.GetClientId(clientId);

        // Assert
        Assert.IsTrue(_traceOutput?.ToString().Contains($"[Updater] Client ID recieved successfully."),
                      "Expected log message for Client ID not found.");
    }

    [TestMethod]
    public void TestSyncUpClientIdIsNull()
    {
        // Arrange
        var client = Client.GetClientInstance();
        client.GetClientId(null);
        // Act
        client.SyncUp();

        // Assert
        Assert.IsTrue(_traceOutput?.ToString().Contains("[Updater] Error in SyncUp: Client ID is null"),
                      "Expected error message for null Client ID not logged.");
    }

    [TestMethod]
    public void TestSyncUpSuccessfulSync()
    {
        // Arrange
        var clientId = "testClientId";
        var client = Client.GetClientInstance();
        client.GetClientId(clientId);

        // Act
        client.SyncUp();
        Assert.IsTrue(_traceOutput?.ToString().Contains("[Updater] Sending data as FileTransferHandler from Manual Sync up..."),
                      "Expected trace message not found.");
    }

    [TestMethod]
    public void TestInvalidSyncHandlerTraceException()
    {
        var fileContent = new FileContent("invalid.txt", null);
        var dataPacket = new DataPacket(DataPacket.PacketType.InvalidSync, [fileContent]);

        try
        {
            // Call InvalidSyncHandler with the invalid packet
            if (_mockCommunicator != null)
            {
                Client.InvalidSyncHandler(dataPacket);
            }

            // If no exception is thrown, fail the test
            Assert.Fail("Expected exception due to null SerializedContent");
        }
        catch (Exception)
        {
            // Assert the trace log for the exception
            Assert.IsTrue(_traceOutput?.ToString().Contains("Error in InvalidSyncHandler: SerializedContent in FileContent is null"),
                          "InvalidSyncHandler did not log the expected error message");
        }
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _traceOutput?.GetStringBuilder().Clear();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _mockCommunicator = null;
        _mockClient = null;
        _traceOutput = null;
    }
}
