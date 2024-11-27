/******************************************************************************
* Filename    = TestClient.cs
*
* Author      = Amithabh A & Garima Ranjan
*
* Product     = Updater
* 
* Project     = Lab Monitoring Software
*
* Description = Unit Tests for Client.cs
*****************************************************************************/

using System.Diagnostics;
using Updater;
using Networking.Communication;

namespace TestsUpdater;

[TestClass]
public class TestClient
{
    private static ICommunicator? _mockCommunicator;
    private static Client? _mockClient;
    private static StringWriter? _traceOutput;

    // <summary>
    // Test initialization before any tests are run. Sets up necessary components like the mock communicator, client instance, and trace listener.
    // </summary>
    [ClassInitialize]
    public static void ClassInit(TestContext context)
    {
        _mockCommunicator = CommunicationFactory.GetCommunicator(isClientSide: true);
        _mockClient = Client.GetClientInstance(notificationReceived: (message) => Trace.WriteLine(message));
        _traceOutput = new StringWriter();
        Trace.Listeners.Clear();
        Trace.Listeners.Add(new TextWriterTraceListener(_traceOutput));
    }

    // <summary>
    // Initializes resources before each individual test. Clears the trace output to start fresh for each test.
    // </summary>
    [TestInitialize]
    public void TestInitialize()
    {
        _traceOutput?.GetStringBuilder().Clear();
        Trace.Listeners.Add(new TextWriterTraceListener(_traceOutput));
    }

    // <summary>
    // Tests that the Client instance follows the Singleton pattern.
    // </summary>
    [TestMethod]
    public void TestGetClientInstanceSingleton()
    {
        var client1 = Client.GetClientInstance();
        var client2 = Client.GetClientInstance();
        Assert.AreEqual(client1, client2, "Client instance is not singleton");
    }

    // <summary>
    // Tests the packet demultiplexer for an invalid sync packet and verifies the appropriate handler is called.
    // </summary>
    [TestMethod]
    public void TestPacketDemultiplexerInvalidSync()
    {
        var fileContent = new FileContent("invalid.txt", Utils.SerializeObject(new List<string> { "file1.txt" })!);
        var fileContentList = new List<Updater.FileContent>();
        fileContentList.Add(fileContent);
        var dataPacket = new DataPacket(DataPacket.PacketType.InvalidSync, fileContentList);
        string serializedData = Utils.SerializeObject(dataPacket)!;

        if (_mockCommunicator != null)
        {
            Client.PacketDemultiplexer(serializedData, _mockCommunicator);

            Thread.Sleep(1500);
            Trace.Flush();

            Assert.IsTrue(_traceOutput?.ToString().Contains("Received invalid file names from server"),
                          "InvalidSyncHandler was not called correctly");
        }
    }

    // <summary>
    // Tests the packet demultiplexer for a broadcast packet and verifies the appropriate handler is called.
    // </summary>
    [TestMethod]
    public void TestPacketDemultiplexerBroadcast()
    {
        var fileContent = new FileContent("test.txt", Utils.SerializeObject("test content")!);
        var fileContentList = new List<Updater.FileContent>();
        fileContentList.Add(fileContent);
        var dataPacket = new DataPacket(DataPacket.PacketType.Broadcast, fileContentList);
        string serializedData = Utils.SerializeObject(dataPacket)!;

        if (_mockCommunicator != null)
        {
            Client.PacketDemultiplexer(serializedData, _mockCommunicator);

            Thread.Sleep(1500);
            Trace.Flush();

            Assert.IsTrue(_traceOutput?.ToString().Contains("Up-to-date with the server"),
                          "BroadcastHandler was not called correctly");
        }
    }

    // <summary>
    // Tests the packet demultiplexer for a sync-up packet and verifies the appropriate handler is called.
    // </summary>
    [TestMethod]
    public void TestPacketDemultiplexerSyncUp()
    {
        var dataPacket = new DataPacket(DataPacket.PacketType.SyncUp, []);
        string serializedData = Utils.SerializeObject(dataPacket)!;

        if (_mockCommunicator != null)
        {
            Client.PacketDemultiplexer(serializedData, _mockCommunicator);

            Thread.Sleep(1500);
            Trace.Flush();

            Assert.IsTrue(_traceOutput?.ToString().Contains("[Updater] Metadata sent to server"),
                          "SyncUpHandler not called correctly");
        }
    }

    // <summary>
    // Tests the packet demultiplexer for a differences packet and verifies the appropriate handler is called.
    // </summary>
    [TestMethod]
    public void TestPacketDemultiplexerDifferences()
    {
        var diffContent = new FileContent("differences", Utils.SerializeObject(new List<MetadataDifference>())!);
        var fileContent = new FileContent("file.txt", Utils.SerializeObject("content")!);
        var dataPacket = new DataPacket(DataPacket.PacketType.Differences, [diffContent, fileContent]);
        string serializedData = Utils.SerializeObject(dataPacket)!;

        if (_mockCommunicator != null)
        {
            Client.PacketDemultiplexer(serializedData, _mockCommunicator);

            Thread.Sleep(1500);
            Trace.Flush();

            Assert.IsTrue(_traceOutput?.ToString().Contains("Sending files to server"),
                          "DifferencesHandler was not called correctly");
        }
    }

    // <summary>
    // Tests the sync-up handler to ensure metadata is sent to the server correctly.
    // </summary>
    [TestMethod]
    public void TestSyncUpHandler()
    {
        var fileContentList = new List<Updater.FileContent>();

        var syncUpPacket = new DataPacket(DataPacket.PacketType.SyncUp, fileContentList);
        if (_mockCommunicator != null)
        {
            Client.SyncUpHandler(_mockCommunicator);

            Thread.Sleep(1500);
            Trace.Flush();

            Assert.IsTrue(_traceOutput?.ToString().Contains("Metadata sent to server"),
                          "SyncUpHandler did not send metadata");
        }
    }

    // <summary>
    // Tests the invalid sync handler to verify the correct handling of invalid sync packets.
    // </summary>
    [TestMethod]
    public void TestInvalidSyncHandler()
    {
        var fileContent = new FileContent("invalid.txt", Utils.SerializeObject(new List<string> { "file1.txt" })!);
        var fileContentList = new List<Updater.FileContent>();
        fileContentList.Add(fileContent);
        var dataPacket = new DataPacket(DataPacket.PacketType.InvalidSync, fileContentList);

        if (_mockCommunicator != null)
        {
            Client.InvalidSyncHandler(dataPacket);

            Thread.Sleep(1500);
            Trace.Flush();

            Assert.IsTrue(_traceOutput?.ToString().Contains("Received invalid file names from server"),
                          "InvalidSyncHandler did not log expected message");
        }
    }

    // <summary>
    // Tests the broadcast handler to verify the broadcast packet handling functionality.
    // </summary>
    [TestMethod]
    public void TestBroadcastHandler()
    {
        var fileContent = new FileContent("test.txt", Utils.SerializeObject("test content")!);
        var fileContentList = new List<Updater.FileContent>();
        fileContentList.Add(fileContent);
        var dataPacket = new DataPacket(DataPacket.PacketType.Broadcast, fileContentList);

        if (_mockCommunicator != null)
        {
            Client.BroadcastHandler(dataPacket);

            Thread.Sleep(1500);
            Trace.Flush();

            Assert.IsTrue(_traceOutput?.ToString().Contains("Up-to-date with the server"),
                          "BroadcastHandler did not update correctly");
        }
    }

    // <summary>
    // Tests the differences handler to verify the correct handling of difference data between client and server.
    // </summary>
    [TestMethod]
    public void TestDifferencesHandler()
    {
        var diffContent = new FileContent("differences", Utils.SerializeObject(new List<MetadataDifference>())!);
        var fileContent = new FileContent("file.txt", Utils.SerializeObject("content")!);
        var fileContentList = new List<Updater.FileContent>();
        fileContentList.Add(diffContent);
        fileContentList.Add(fileContent);
        var dataPacket = new DataPacket(DataPacket.PacketType.Differences, fileContentList);

        if (_mockCommunicator != null)
        {
            Client.DifferencesHandler(dataPacket, _mockCommunicator);

            Thread.Sleep(1500);
            Trace.Flush();

            Assert.IsTrue(_traceOutput?.ToString().Contains("Sending files to server"),
                          "DifferencesHandler did not send files correctly");
        }
    }

    // <summary>
    // Tests the functionality to show invalid files in the UI.
    // </summary>
    [TestMethod]
    public void TestShowInvalidFilesInUI()
    {
        Client.ShowInvalidFilesInUI(new[] { "file1.txt", "file2.txt" }.ToList());
        Thread.Sleep(1500);
        Trace.Flush();
        Assert.IsTrue(_traceOutput?.ToString().Contains("Invalid filenames"),
                      "ShowInvalidFilesInUI did not log expected message");
        Assert.IsTrue(_traceOutput?.ToString().Contains("Sync up failed"),
                      "ShowInvalidFilesInUI did not log expected message");
    }

    // <summary>
    // Tests the packet demultiplexer when handling empty data and verifies proper handling of such cases.
    // </summary>
    [TestMethod]
    public void TestPacketDemultiplexerEmptyData()
    {
        if (_mockCommunicator != null)
        {
            Client.PacketDemultiplexer(string.Empty, _mockCommunicator);

            Thread.Sleep(1500);
            Trace.Flush();

            Assert.IsTrue(_traceOutput?.ToString().Contains("[Updater] Error in PacketDemultiplexer: Object reference not set to an instance of an object.\r\n"),
                          "PacketDemultiplexer did not handle empty data correctly");
        }
    }

    // <summary>
    // Tests the retrieval of the client ID and the proper logging of success.
    // </summary>
    [TestMethod]
    public void TestGetClientId()
    {
        var clientId = "testClientId";
        var client = Client.GetClientInstance();

        client.GetClientId(clientId);

        Thread.Sleep(1500);
        Trace.Flush();
        Assert.IsTrue(_traceOutput?.ToString().Contains($"[Updater] Client ID recieved successfully."),
                      "Expected log message for Client ID not found.");
    }

    // <summary>
    // Tests the sync-up process when the client ID is null, ensuring the error is logged.
    // </summary>
    [TestMethod]
    public void TestSyncUpClientIdIsNull()
    {
        var client = Client.GetClientInstance();
        client.GetClientId(null);

        client.SyncUp();

        Thread.Sleep(1500);
        Trace.Flush();
        Assert.IsTrue(_traceOutput?.ToString().Contains("[Updater] Error in SyncUp: Client ID is null"),
                      "Expected error message for null Client ID not logged.");
    }

    // <summary>
    // Tests the successful sync-up process when a valid client ID is provided.
    // </summary>
    [TestMethod]
    public void TestSyncUpSuccessfulSync()
    {
        var clientId = "testClientId";
        var client = Client.GetClientInstance();
        client.GetClientId(clientId);

        client.SyncUp();
        Thread.Sleep(1500);
        Trace.Flush();
        Assert.IsTrue(_traceOutput?.ToString().Contains("[Updater] Sending data as FileTransferHandler from Manual Sync up..."),
                      "Expected trace message not found.");
    }

    // <summary>
    // Tests the InvalidSyncHandler to verify the correct logging of exceptions when encountering invalid sync data.
    // </summary>
    [TestMethod]
    public void TestInvalidSyncHandlerTraceException()
    {
        var fileContent = new FileContent("invalid.txt", null);
        var dataPacket = new DataPacket(DataPacket.PacketType.InvalidSync, [fileContent]);

        try
        {
            if (_mockCommunicator != null)
            {
                Client.InvalidSyncHandler(dataPacket);
            }

            Assert.Fail("Expected exception due to null SerializedContent");
        }
        catch (Exception)
        {
            Assert.IsTrue(_traceOutput?.ToString().Contains("Error in InvalidSyncHandler: SerializedContent in FileContent is null"),
                          "InvalidSyncHandler did not log the expected error message");
        }
    }

    // <summary>
    // Test cleanup after each test, ensuring that trace listeners are cleared.
    // </summary>
    [TestCleanup]
    public void TestCleanup()
    {
        Trace.Flush();
        Trace.Listeners.Clear(); // Ensure listeners are fully cleared after each test
        _traceOutput?.GetStringBuilder().Clear();
    }

    // <summary>
    // Class cleanup after all tests have completed, ensuring all static variables are cleaned up.
    // </summary>
    [ClassCleanup]
    public static void ClassCleanup()
    {
        _mockCommunicator = null;
        _mockClient = null;
        _traceOutput = null;
    }
}
