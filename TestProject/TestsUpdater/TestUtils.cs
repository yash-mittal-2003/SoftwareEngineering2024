using System.Text;
using System.Diagnostics;
using Updater;

namespace TestsUpdater;

[TestClass]
public class TestUtils
{
    private readonly string _testFilePath = Path.GetTempFileName();
    private readonly string _testTextFilePath = Path.GetTempFileName();
    private readonly string _base64TestData = Convert.ToBase64String(Encoding.UTF8.GetBytes("test binary content"));

    [TestMethod]
    public void TestReadBinaryFileShouldReturnNullWhenFileDoesNotExist()
    {
        string nonExistentFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        string? result = Utils.ReadBinaryFile(nonExistentFilePath);

        Assert.IsNull(result, "Expected result to be null when the file doesn't exist.");
    }

    [TestMethod]
    public void TestReadBinaryFileShouldReturnBase64StringWhenFileExists()
    {
        // Arrange
        File.WriteAllBytes(_testFilePath, Encoding.UTF8.GetBytes("test binary content"));

        // Act
        string? result = Utils.ReadBinaryFile(_testFilePath);

        // Assert
        Assert.IsNotNull(result, "Expected a result when the file exists.");
        Assert.AreEqual(_base64TestData, result, "Expected the base64 content to match.");
    }

    [TestMethod]
    public void TestWriteToFileFromBinaryShouldWriteFileWhenValidBase64String()
    {
        // Act
        bool result = Utils.WriteToFileFromBinary(_testFilePath, _base64TestData);

        // Assert
        Assert.IsTrue(result, "Expected the file writing to succeed.");

        // Verify file content
        string content = File.ReadAllText(_testFilePath);
        Assert.AreEqual("test binary content", content, "Expected the written content to match.");
    }

    [TestMethod]
    public void TestWriteToFileFromBinaryShouldWriteTextWhenNotBase64()
    {
        // Act
        bool result = Utils.WriteToFileFromBinary(_testTextFilePath, "This is a regular text.");

        // Assert
        Assert.IsTrue(result, "Expected the text writing to succeed.");

        // Verify file content
        string content = File.ReadAllText(_testTextFilePath);
        Assert.AreEqual("This is a regular text.", content, "Expected the written text to match.");
    }

    [TestMethod]
    public void TestSerializeObjectShouldReturnSerializedStringWhenObjectIsValid()
    {
        // Arrange
        var testObject = new FileMetadata { FileName = "test.txt", FileHash = "abcdef123456" };

        // Act
        string? result = Utils.SerializeObject(testObject);

        // Assert
        Assert.IsNotNull(result, "Expected the object to serialize successfully.");
        Assert.IsTrue(result.Contains("</FileMetadata>"), "Expected serialized string to contain FileMetadata XML element.");
    }

    [TestMethod]
    public void TestDeserializeObjectShouldReturnObjectWhenSerializedDataIsValid()
    {
        // Arrange
        var testObject = new FileMetadata { FileName = "test.txt", FileHash = "abcdef123456" };
        string? serializedData = Utils.SerializeObject(testObject);

        // Act
        if (serializedData != null)
        {
            FileMetadata deserializedObject = Utils.DeserializeObject<FileMetadata>(serializedData);

            // Assert
            Assert.IsNotNull(deserializedObject, "Expected the object to deserialize successfully.");
            Assert.AreEqual(testObject.FileName, deserializedObject.FileName, "Expected file names to match.");
            Assert.AreEqual(testObject.FileHash, deserializedObject.FileHash, "Expected file hashes to match.");
        }
        else
        {
            Assert.Fail("Serialized data is null");
        }
    }

    [TestMethod]
    public void TestSerializedMetadataPacketShouldReturnValidPacketWhenCalled()
    {
        // Arrange
        string toolsDirectory = Path.Combine(Path.GetTempPath(), "ToolsDirectory");
        Directory.CreateDirectory(toolsDirectory);

        // Create a dummy file
        string filePath = Path.Combine(toolsDirectory, "testfile.txt");
        File.WriteAllText(filePath, "dummy content");

        // Act
        string? result = Utils.SerializedMetadataPacket();

        // Assert
        Assert.IsNotNull(result, "Expected the serialized metadata packet to be generated.");
        Assert.IsTrue(result.Contains("</DataPacket>"), "Expected serialized string to contain DataPacket XML element.");
    }

    [TestMethod]
    public void TestSerializedSyncUpPacketShouldReturnValidSyncUpPacketWhenCalled()
    {
        string sampleClientId = "2";
        // Act
        string? result = Utils.SerializedSyncUpPacket(sampleClientId);

        // Assert
        Assert.IsNotNull(result, "Expected the serialized sync-up packet to be generated.");
        Assert.IsTrue(result.Contains("</DataPacket>"), "Expected serialized string to contain DataPacket XML element.");
        Assert.IsTrue(result.Contains("<PacketType>SyncUp</PacketType>"), "Expected the packet type to be SyncUp.");
    }

    [TestMethod]
    public void TestConstructorShouldLogMessageWhenDirectoryDoesNotExist()
    {
        // Arrange
        string nonExistentDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        using var stringWriter = new System.IO.StringWriter();
        // Add a `trace` listener to capture the log message
        Trace.Listeners.Add(new TextWriterTraceListener(stringWriter));

        // Act
        var generator = new DirectoryMetadataGenerator(nonExistentDirectory);

        // Assert
        string output = stringWriter.ToString();
        Assert.IsTrue(output.Contains("Directory does not exist"), "Log message not found.");
    }

    [TestMethod]
    public void TestSerializeObjectShouldLogErrorWhenSerializationFails()
    {
        // Arrange
        var invalidObject = new { InvalidProperty = new object() }; // This can be an invalid type for the serializer

        using var stringWriter = new System.IO.StringWriter();
        // Add a `trace` listener to capture the log message
        Trace.Listeners.Add(new TextWriterTraceListener(stringWriter));

        // Act
        string? result = Utils.SerializeObject(invalidObject);

        // Assert
        string output = stringWriter.ToString();
        Assert.IsTrue(output.Contains("Exception caught in Serializer.Serialize()"), "Error message not logged.");
        Assert.IsNull(result, "Expected serialization to fail.");
    }

    [TestMethod]
    public void TestWriteToFileFromBinaryShouldLogErrorAndReturnFalseWhenExceptionOccurs()
    {
        // Arrange: Set up a path to a file in a non-existent directory
        string invalidFilePath = Path.Combine(Path.GetTempPath(), "nonexistentDir", "testFile.bin");

        using var stringWriter = new System.IO.StringWriter();
        Trace.Listeners.Add(new TextWriterTraceListener(stringWriter));  // Capture logs

        // Act: Try writing to the file path that doesn't exist
        bool result = Utils.WriteToFileFromBinary(invalidFilePath, _base64TestData);

        Assert.IsFalse(result, "Expected the method to return false when an error occurs.");

        // Check the log output to ensure the error is logged
        string output = stringWriter.ToString();
        Assert.IsTrue(output.Contains("An error occurred while writing to the file"), "Error message not logged correctly.");
    }

    private void DeleteFileWithRetry(string filePath)
    {
        const int maxRetries = 4;
        const int delay = 100; // 100 ms

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                break;
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

    private void DeleteDirectoryWithRetry(string directoryPath)
    {
        const int maxRetries = 4;
        const int delay = 100; // 100 ms

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                if (Directory.Exists(directoryPath))
                {
                    Directory.Delete(directoryPath, true);
                }
                break;
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

    [TestCleanup]
    public void Cleanup()
    {
        try
        {
            Trace.WriteLine($"Cleaning up test files.");
            DeleteFileWithRetry(_testFilePath);
            DeleteFileWithRetry(_testTextFilePath);
            DeleteDirectoryWithRetry(Path.Combine(Path.GetTempPath(), "ToolsDirectory"));
        }
        catch (IOException ex)
        {
            Trace.WriteLine($"Cleanup failed: {ex.Message}");
            throw;
        }
    }
}
