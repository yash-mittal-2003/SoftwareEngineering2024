/******************************************************************************
* Filename    = TestServerViewModel.cs
*
* Author      = Garima Ranjan & Karumudi Harika
*
* Product     = Updater
* 
* Project     = Lab Monitoring Software
*
* Description = Unit Tests for ServerViewModel.cs
*****************************************************************************/

using Moq;
using ViewModel.UpdaterViewModel;
using Updater;
using System.Diagnostics;

namespace TestsUpdater;

[TestClass]
public class TestServerViewModel
{
    /// <summary>
    /// Mocked instance of LogServiceViewModel used for verifying interactions with logging.
    /// </summary>
    private Mock<LogServiceViewModel>? _mockLogServiceViewModel;

    /// <summary>
    /// Mocked instance of ToolAssemblyLoader used for dependency injection in the ServerViewModel.
    /// </summary>
    private Mock<ToolAssemblyLoader>? _mockLoader;

    /// <summary>
    /// The ServerViewModel instance being tested.
    /// </summary>
    private ServerViewModel? _serverViewModel;

    /// <summary>
    /// Path to the test folder for simulation purposes.
    /// </summary>
    private readonly string _testFolderPath = @"../../../TestingFolder";

    /// <summary>
    /// Valid test folder path, resolved relative to the application's base directory.
    /// </summary>
    private string? _validTestFolderPath;

    /// <summary>
    /// Initializes the test environment before each test.
    /// Sets up mock objects, dependencies, and paths.
    /// </summary>
    [TestInitialize]
    public void TestInitialize()
    {
        _validTestFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _testFolderPath);

        // Arrange: Mock LogServiceViewModel and ToolAssemblyLoader
        _mockLogServiceViewModel = new Mock<LogServiceViewModel>();
        _mockLoader = new Mock<ToolAssemblyLoader>();

        // Act: Create an instance of ServerViewModel using the mocks
        _serverViewModel = new ServerViewModel(_mockLogServiceViewModel.Object, _mockLoader.Object);
    }

    /// <summary>
    /// Verifies that the ServerViewModel is successfully initialized when the constructor is called.
    /// </summary>
    [TestMethod]
    public void TestConstructorShouldInitializeServerWhenCalled()
    {
        Assert.IsNotNull(_serverViewModel);
    }

    /// <summary>
    /// Verifies that GetServer returns a valid Server instance.
    /// </summary>
    [TestMethod]
    public void TestGetServerInstance()
    {
        if (_serverViewModel != null)
        {
            Server server = _serverViewModel.GetServer();
            Assert.IsNotNull(server);
        }
    }

    /// <summary>
    /// Verifies that calling AddLogMessage on ServerViewModel updates the log with the specified message.
    /// </summary>
    [TestMethod]
    public void TestAddLogMessageShouldUpdateLogWhenCalled()
    {
        string logMessage = "Test log message";

        if (_serverViewModel != null && _mockLogServiceViewModel != null)
        {
            // Act: Add a log message
            _serverViewModel.AddLogMessage(logMessage);

            // Assert: Verify that the log update method was called with the correct message
            _mockLogServiceViewModel.Verify(log => log.UpdateLogDetails(It.Is<string>(msg => msg == logMessage)), Times.Once);
        }
    }

    /// <summary>
    /// Verifies that calling BroadcastToClients logs a message indicating files are being sent to clients.
    /// </summary>
    [TestMethod]
    public void TestBroadcastToClientsShouldLogMessageWhenCalled()
    {
        string testFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../TestsUpdater/CopyTestFolder");
        string filePath = Path.Combine(testFolderPath, "InvalidDLL.dll");
        string fileName = "InvalidDLL.dll";

        if (_serverViewModel != null && _mockLogServiceViewModel != null)
        {
            // Arrange: Mock the log method
            _mockLogServiceViewModel.Setup(log => log.UpdateLogDetails(It.IsAny<string>())).Verifiable();

            // Act: Broadcast files to clients
            _serverViewModel.BroadcastToClients(filePath, fileName);

            // Assert: Verify that the correct log message was created
            _mockLogServiceViewModel.Verify(
                log => log.UpdateLogDetails(It.Is<string>(msg => msg == "Sending files to all connected clients")),
                Times.Once
            );
        }
    }

    /// <summary>
    /// Verifies that GetServerData logs an error when the specified directory does not exist.
    /// </summary>
    [TestMethod]
    public void TestGetServerDataShouldLogErrorWhenDirectoryDoesNotExist()
    {
        string invalidFolderPath = @"C:\invalidFolder";

        // Capture Trace output for verification
        using var stringWriter = new System.IO.StringWriter();
        Trace.Listeners.Add(new TextWriterTraceListener(stringWriter));

        if (_serverViewModel != null)
        {
            // Act: Try to get server data from a non-existent directory
            string? result = _serverViewModel.GetServerData(invalidFolderPath);

            // Assert: Verify that the error message was logged
            string output = stringWriter.ToString();
            Assert.IsTrue(output.Contains("Server directory not found."), "Expected log message not found.");

            // Assert: Ensure it returns an empty list as JSON
            Assert.AreEqual("[]", result);
        }
    }
}
