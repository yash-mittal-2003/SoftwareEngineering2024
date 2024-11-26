/******************************************************************************
* Filename    = TestClientViewModel.cs
*
* Author      = Garima Ranjan
*
* Product     = Updater
* 
* Project     = Lab Monitoring Software
*
* Description = Unit Tests for ClientViewModel.cs
*****************************************************************************/

using Moq;
using Updater;
using ViewModel.UpdaterViewModel;

/// <summary>
/// Unit Tests for the ClientViewModel class.
/// Verifies the functionality and integration with the LogServiceViewModel and Client classes.
/// </summary>
namespace TestsUpdater;

[TestClass]
public class TestClientViewModel
{
    /// <summary>
    /// Mocked instance of the LogServiceViewModel used to verify interactions with the logging system.
    /// </summary>
    private Mock<LogServiceViewModel>? _mockLogServiceViewModel;

    /// <summary>
    /// Instance of the ClientViewModel being tested.
    /// </summary>
    private ClientViewModel? _viewModel;

    /// <summary>
    /// Mocked instance of the Client class, created using reflection.
    /// </summary>
    private Client? _mockClient;

    /// <summary>
    /// Initializes the test environment before each test method runs.
    /// Sets up mock objects and injects dependencies into the ClientViewModel.
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
        // Mock LogServiceViewModel
        _mockLogServiceViewModel = new Mock<LogServiceViewModel>();

        // Use reflection to instantiate the Client class
        System.Reflection.ConstructorInfo? constructorInfo = typeof(Client).GetConstructor(
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null,
            Array.Empty<Type>(),
            null);

        if (constructorInfo == null)
        {
            Assert.Fail("Unable to find a suitable constructor for Client.");
        }

        _mockClient = (Client)constructorInfo.Invoke(null);

        // Replace the LogServiceViewModel with a mocked one in Client
        typeof(Client)
            .GetField("OnLogUpdate", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(null, (Action<string>)_mockLogServiceViewModel.Object.UpdateLogDetails);

        // Initializing ClientViewModel and injecting the mock LogServiceViewModel
        _viewModel = new ClientViewModel(_mockLogServiceViewModel.Object);

        // Injecting the private s_client field in ClientViewModel with our mock
        typeof(ClientViewModel)
            .GetField("s_client", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(_viewModel, _mockClient);
    }

    /// <summary>
    /// Tests that the SyncUpAsync method invokes the client's sync-up logic
    /// and logs the completion of the operation.
    /// </summary>
    [TestMethod]
    public async Task TestSyncUpAsyncInvokesClientAndLogsCompletion()
    {
        bool syncUpCalled = false;

        // Mock the SyncUp method behavior
        typeof(Client)
            .GetMethod("SyncUp", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
            ?.Invoke(_mockClient, Array.Empty<object>());

        if (_mockLogServiceViewModel == null)
        {
            Assert.Fail("_mockLogServiceViewModel is not initialized.");
        }

        // Update LogServiceViewModel mock
        _mockLogServiceViewModel
            .Setup(log => log.UpdateLogDetails("Sync completed."))
            .Callback(() => syncUpCalled = true)
            .Verifiable();

        if (_viewModel == null)
        {
            Assert.Fail("_viewModel is not initialized.");
        }

        await _viewModel.SyncUpAsync();

        // Assert that the sync-up was called and the log was updated
        Assert.IsTrue(syncUpCalled, "SyncUp should have been called.");
        _mockLogServiceViewModel.Verify(
            log => log.UpdateLogDetails("Sync completed."),
            Times.Once,
            "LogServiceViewModel.UpdateLogDetails should be called with 'Sync completed.'"
        );
    }

    /// <summary>
    /// Verifies that the PropertyChanged event is raised when a property changes.
    /// </summary>
    [TestMethod]
    public void TestOnPropertyChangedEventIsRaised()
    {
        bool eventRaised = false;
        string? raisedPropertyName = null;

        Assert.IsNotNull(_viewModel, "ClientViewModel should be initialized.");
        _viewModel!.PropertyChanged += (sender, args) => {
            eventRaised = true;
            raisedPropertyName = args.PropertyName;
        };

        // Trigger the PropertyChanged event
        _viewModel.GetType()
            .GetMethod("OnPropertyChanged", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.Invoke(_viewModel, new object[] { "TestProperty" });

        // Assert that the event was raised with the correct property name
        Assert.IsTrue(eventRaised, "PropertyChanged event should be raised.");
        Assert.AreEqual("TestProperty", raisedPropertyName, "PropertyChanged event should be raised with the correct property name.");
    }
}
