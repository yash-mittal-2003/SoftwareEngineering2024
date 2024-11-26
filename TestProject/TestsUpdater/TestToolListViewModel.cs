/******************************************************************************
* Filename    = TestToolListViewModel.cs
*
* Author      = Garima Ranjan 
*
* Product     = Updater
* 
* Project     = Lab Monitoring Software
*
* Description = Unit tests for ToolListViewModel.cs
*****************************************************************************/

using Moq;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Updater;
using ViewModel.UpdaterViewModel;

namespace TestsUpdater;

[TestClass]
public class TestToolListViewModel
{
    /// <summary>
    /// Mock object for IToolAssemblyLoader to simulate dependency behavior.
    /// </summary>
    private Mock<IToolAssemblyLoader>? _mockDllLoader;

    /// <summary>
    /// Instance of ToolListViewModel under test.
    /// </summary>
    private ToolListViewModel? _viewModel;

    /// <summary>
    /// Path to the folder containing both v1 and v2 of the tools.
    /// </summary>
    private string _testFolderPath = @"../../../TestsUpdater/TestingFolder";

    /// <summary>
    /// Path to the folder containing only v2 of the tools.
    /// </summary>
    private readonly string _copyTestFolderPath = @"../../../TestsUpdater/CopyTestFolder";

    /// <summary>
    /// Custom trace listener used to capture trace messages for validation.
    /// </summary>
    private class TestTraceListener : TraceListener
    {
        /// <summary>
        /// Captured trace messages.
        /// </summary>
        public List<string> Messages { get; } = new List<string>();

        /// <summary>
        /// Captures trace messages written without a new line.
        /// </summary>
        public override void Write(string? message)
        {
            if (message != null)
            {
                Messages.Add(message);
            }
        }

        /// <summary>
        /// Captures trace messages written with a new line.
        /// </summary>
        public override void WriteLine(string? message)
        {
            if (message != null)
            {
                Messages.Add(message);
            }
        }
    }

    /// <summary>
    /// Instance of the custom trace listener.
    /// </summary>
    private TestTraceListener? _traceListener;

    /// <summary>
    /// Sets up the test environment before each test.
    /// Initializes mocks, directories, and the custom trace listener.
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
        _mockDllLoader = new Mock<IToolAssemblyLoader>();
        _ = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _testFolderPath);

        // Add custom trace listener for capturing trace messages
        _traceListener = new TestTraceListener();
        Trace.Listeners.Add(_traceListener);
    }

    /// <summary>
    /// Cleans up the test environment after each test.
    /// Removes the custom trace listener.
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        if (_traceListener != null)
        {
            Trace.Listeners.Remove(_traceListener);
        }
    }

    /// <summary>
    /// Verifies that calling LoadAvailableTools uses the tools directory defined in TApps constants.
    /// </summary>
    [TestMethod]
    public void TestLoadAvailableToolsUseToolsDirectoryInTAppsConstant()
    {
        _viewModel = new ToolListViewModel();
        _viewModel.LoadAvailableTools();
    }

    /// <summary>
    /// Verifies that LoadAvailableTools populates the AvailableToolsList when valid tools are available.
    /// </summary>
    [TestMethod]
    public void TestLoadAvailableToolsShouldPopulateAvailableToolsListWhenToolsAreAvailable()
    {
        string copyTestFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _copyTestFolderPath);

        _viewModel = new ToolListViewModel(copyTestFolderPath);
        _viewModel.LoadAvailableTools(copyTestFolderPath);

        Assert.IsNotNull(_viewModel.AvailableToolsList);
        Assert.AreEqual(1, _viewModel.AvailableToolsList.Count);

        Tool tool = _viewModel.AvailableToolsList[0];
        Assert.AreEqual("OtherExampleAnalyzer.OtherExample", tool.Name);
        Assert.AreEqual("2.0.0", tool.Version);
    }

    /// <summary>
    /// Verifies that LoadAvailableTools replaces older versions of tools when newer versions exist.
    /// </summary>
    [TestMethod]
    public void TestLoadAvailableToolsShouldReplaceOlderVersionWhenNewerVersionExists()
    {
        string testFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _testFolderPath);

        var viewModel = new ToolListViewModel(testFolderPath);

        viewModel.LoadAvailableTools(testFolderPath);
        ObservableCollection<Tool>? updatedTools = viewModel.AvailableToolsList;

        Assert.AreEqual(1, updatedTools?.Count);
        if (updatedTools != null)
        {
            Tool updatedTool = updatedTools.First();
            Assert.AreEqual("OtherExampleAnalyzer.OtherExample", updatedTool.Name);
            Assert.AreEqual("2.0.0", updatedTool.Version);
        }
    }

    /// <summary>
    /// Verifies that LoadAvailableTools raises a PropertyChanged event when the tools list is updated.
    /// </summary>
    [TestMethod]
    public void TestLoadAvailableToolsShouldFirePropertyChangedWhenToolsAreUpdated()
    {
        string testFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _testFolderPath);

        _viewModel = new ToolListViewModel(testFolderPath);

        bool wasPropertyChangedFired = false;
        _viewModel.PropertyChanged += (sender, e) => {
            if (e.PropertyName == nameof(_viewModel.AvailableToolsList))
            {
                wasPropertyChangedFired = true;
            }
        };

        _viewModel.LoadAvailableTools(testFolderPath);

        Assert.IsTrue(wasPropertyChangedFired);
    }

    /// <summary>
    /// Verifies that LoadAvailableTools generates the expected trace messages during execution.
    /// </summary>
    [TestMethod]
    public void TestLoadAvailableToolsShouldGenerateExpectedTraceLines()
    {
        string testFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _testFolderPath);

        _viewModel = new ToolListViewModel(testFolderPath);

        _viewModel.LoadAvailableTools(testFolderPath);

        bool replacedOlderVersionMsg = _traceListener!.Messages.Any(msg => msg.Contains("[Updater] Replaced older version with new version for tool"));
        bool addedNewToolMsg = _traceListener.Messages.Any(msg => msg.Contains("[Updater] Added new tool"));
        bool successMsg = _traceListener.Messages.Any(msg => msg.Contains("Available Tools information updated successfully"));

        Thread.Sleep(1000);
        Trace.Flush();

        Assert.IsTrue(replacedOlderVersionMsg, "Expected trace message for replacing older version was not found.");
        Assert.IsTrue(addedNewToolMsg, "Expected trace message for adding new tool was not found.");
        Assert.IsTrue(successMsg, "Expected trace message for successful update was not found.");
    }
}
