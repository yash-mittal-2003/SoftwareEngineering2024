/******************************************************************************
* Filename    = TestToolAssemblyLoader.cs
*
* Author      = Garima Ranjan 
*
* Product     = Updater
* 
* Project     = Lab Monitoring Software
*
* Description = Unit tests for ToolAssemblyLoader.cs
*****************************************************************************/
using System.Diagnostics;
using Updater;

namespace TestsUpdater;

/// <summary>
/// Test class for testing the ToolAssemblyLoader class.
/// </summary>
[TestClass]
public class TestToolAssemblyLoader
{
    private readonly string _emptyTestFolderPath = @"EmptyTestingFolder";
    private readonly string _nonExistingFolder = @"DoesNotExistTestingFolder";
    private readonly string _testFolderPath = @"../../../TestsUpdater/TestingFolder";
    private readonly string _corruptedDllFolderPath = @"CorruptedDllTestingFolder";
    private ToolAssemblyLoader? _loader;

    /// <summary>
    /// Set up the testing environment by ensuring the empty test folder is created.
    /// </summary>
    [TestInitialize]
    public void SetUp()
    {
        // Ensure the test directory exists and is clean
        if (!Directory.Exists(_emptyTestFolderPath))
        {
            Directory.CreateDirectory(_emptyTestFolderPath);
        }

        if (!Directory.Exists(_corruptedDllFolderPath))
        {
            Directory.CreateDirectory(_corruptedDllFolderPath);
        }
    }

    /// <summary>
    /// Deletes a file with retry logic to handle any IOExceptions.
    /// </summary>
    private void DeleteFileWithRetry(string filePath)
    {
        const int maxRetries = 3;
        const int delay = 100; // 100 ms

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    // Ensure file is not read-only before attempting deletion
                    File.SetAttributes(filePath, FileAttributes.Normal);
                    File.Delete(filePath);
                }
                break; // Break the loop if deletion is successful
            }
            catch (IOException)
            {
                if (attempt == maxRetries)
                {
                    throw; // Re-throw after max retries
                }
                Thread.Sleep(delay); // Wait before retrying
            }
        }
    }

    /// <summary>
    /// Deletes a directory with retry logic to handle any IOExceptions.
    /// </summary>
    private void DeleteDirectoryWithRetry(string directoryPath)
    {
        const int maxRetries = 3;
        const int delay = 100; // 100 ms

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                if (Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, true); // Delete directory and contents
                }
                break; // Break the loop if deletion is successful
            }
            catch (IOException)
            {
                if (attempt == maxRetries)
                {
                    throw; // Re-throw after max retries
                }
                Thread.Sleep(delay); // Wait before retrying
            }
        }
    }

    /// <summary>
    /// Clean up any files or directories created during the tests, with retry logic.
    /// </summary>
    [TestCleanup]
    public void CleanUp()
    {
        try
        {
            Trace.WriteLine("Cleaning up test files.");

            // Clean up the files and directories with retry logic
            DeleteFileWithRetry(_emptyTestFolderPath);
            DeleteFileWithRetry(_corruptedDllFolderPath);

            // Remove any directories if they exist
            DeleteDirectoryWithRetry(_emptyTestFolderPath);
            DeleteDirectoryWithRetry(_corruptedDllFolderPath);
        }
        catch (IOException ex)
        {
            Trace.WriteLine($"Cleanup failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Test case to verify that an empty folder returns an empty dictionary when loading tools.
    /// </summary>
    [TestMethod]
    public void TestLoadToolsFromFolderEmptyFolderReturnsEmptyDictionary()
    {
        if (Directory.Exists(_nonExistingFolder))
        {
            Directory.Delete(_nonExistingFolder, true);
        }
        _loader = new ToolAssemblyLoader();
        Dictionary<string, List<string>> result = _loader.LoadToolsFromFolder(_nonExistingFolder);
        Assert.AreEqual(0, result.Count, "Expected empty dictionary for an empty folder.");
    }

    /// <summary>
    /// Test case to verify that non-DLL files are ignored when loading tools.
    /// </summary>
    [TestMethod]
    public void TestLoadToolsFromFolderIgnoresNonDllFiles()
    {
        _loader = new ToolAssemblyLoader();
        File.WriteAllText(Path.Combine(_emptyTestFolderPath, "test.txt"), "This is a test file.");
        Dictionary<string, List<string>> result = _loader.LoadToolsFromFolder(_emptyTestFolderPath);
        Assert.AreEqual(0, result.Count, "Expected empty dictionary when no DLL files are present.");
    }

    /// <summary>
    /// Test case to verify that valid DLL files are loaded correctly and return expected tool properties.
    /// </summary>
    [TestMethod]
    public void TestLoadToolsFromFolderValidDllWithIToolReturnsToolProperties()
    {
        _loader = new ToolAssemblyLoader();
        string lastUpdatedDate = DateTime.Today.ToString("yyyy-MM-dd");

        // Constructing the full path to the TestingFolder
        string testFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _testFolderPath);
        _ = Path.Combine(testFolderPath, "ValidTool.dll");

        // Load tools from the folder
        Dictionary<string, List<string>> result = _loader.LoadToolsFromFolder(testFolderPath);

        // Verifying that the keys exist and check their values
        Assert.IsTrue(result.TryGetValue("Id", out List<string>? ids), "Key 'Id' not found.");
        Assert.AreEqual("4", ids.FirstOrDefault(), "Expected Id was not found.");

        Assert.IsTrue(result.TryGetValue("Name", out List<string>? names), "Key 'Name' not found.");
        Assert.AreEqual("OtherExampleAnalyzer.OtherExample", names.FirstOrDefault(), "Expected Name was not found.");

        Assert.IsTrue(result.TryGetValue("Description", out List<string>? descriptions), "Key 'Description' not found.");
        Assert.AreEqual("OtherExample Description", descriptions.FirstOrDefault(), "Expected Description was not found.");

        Assert.IsTrue(result.TryGetValue("Version", out List<string>? versions), "Key 'Version' not found.");
        Assert.AreEqual("1.0.0", versions.FirstOrDefault(), "Expected Version was not found.");

        Assert.IsTrue(result.TryGetValue("IsDeprecated", out List<string>? isDeprecations), "Key 'IsDeprecated' not found.");
        Assert.AreEqual("True", isDeprecations.FirstOrDefault(), "Expected IsDeprecated value was not found.");

        Assert.IsTrue(result.TryGetValue("CreatorName", out List<string>? creatorNames), "Key 'CreatorName' not found.");
        Assert.AreEqual("OtherExample Creator", creatorNames.FirstOrDefault(), "Expected CreatorName was not found.");

        Assert.IsTrue(result.TryGetValue("LastUpdated", out List<string>? lastUpdatedDates), "Key 'LastUpdated' not found.");
        Assert.AreEqual(lastUpdatedDate, lastUpdatedDates.FirstOrDefault(), "Expected LastUpdated was not found.");

        Assert.IsTrue(result.TryGetValue("LastModified", out List<string>? lastModifiedDates), "Key 'LastModified' not found.");
        Assert.AreEqual("2023-11-10", lastModifiedDates.FirstOrDefault(), "Expected LastModified was not found.");

        Assert.IsTrue(result.TryGetValue("CreatorEmail", out List<string>? creatorEmails), "Key 'CreatorEmail' not found.");
        Assert.AreEqual("creatorcca@example.com", creatorEmails.FirstOrDefault(), "Expected CreatorEmail was not found.");
    }
    /// <summary>
    /// Test case to verify that an exception is thrown when attempting to load an invalid DLL file.
    /// </summary>
    [TestMethod]
    public void TestLoadToolsFromFolderHandlesInvalidDllFiles()
    {
        _loader = new ToolAssemblyLoader();
        string testFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../TestsUpdater/CopyTestFolder");
        _ = Path.Combine(testFolderPath, "InvalidDLL.dll");

        // Attempting to load tools from the folder containing the invalid DLL
        Dictionary<string, List<string>> result = _loader.LoadToolsFromFolder(testFolderPath);
        Assert.AreEqual(1, result["Id"].Count, "Invalid DLL files should not populate the toolPropertyMap.");
    }

    /// <summary>
    /// Test case to verify that corrupted DLL files are handled properly and don't cause a crash.
    /// </summary>
    [TestMethod]
    public void TestLoadToolsFromFolderHandlesCorruptedDllFiles()
    {
        _loader = new ToolAssemblyLoader();
        string corruptedTestFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _corruptedDllFolderPath);

        // Simulate a corrupted DLL file by creating an empty file with a .dll extension
        string corruptedDllPath = Path.Combine(corruptedTestFolderPath, "corrupted.dll");
        File.WriteAllText(corruptedDllPath, "This is not a valid DLL.");

        // Load tools from the folder containing the corrupted DLL
        Dictionary<string, List<string>> result = _loader.LoadToolsFromFolder(corruptedTestFolderPath);

        // Verify that the result is an empty dictionary, indicating that the corrupted DLL was ignored
        Assert.AreEqual(0, result.Count, "Expected empty dictionary after encountering a corrupted DLL.");
    }
}
