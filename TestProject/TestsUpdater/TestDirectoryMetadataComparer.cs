/******************************************************************************
* Filename    = TestDirectoryMetadataComparer.cs
*
* Author      = Garima Ranjan & Amithabh A.
*
* Product     = Updater
* 
* Project     = Lab Monitoring Software
*
* Description = Unit Tests for DirectoryMetadataComparer.cs
*****************************************************************************/

using Updater;

namespace TestsUpdater;

[TestClass]
public class TestDirectoryMetadataComparer
{
    /// <summary>
    /// Sample metadata set A for testing.
    /// </summary>
    private List<FileMetadata>? _metadataA;

    /// <summary>
    /// Sample metadata set B for testing.
    /// </summary>
    private List<FileMetadata>? _metadataB;

    /// <summary>
    /// Sets up sample file metadata for testing.
    /// This method runs before each test case.
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
        // Prepare sample metadata for two directories
        _metadataA = new List<FileMetadata>
        {
            new() { FileName = "file1.txt", FileHash = "hash1" },
            new() { FileName = "file2.txt", FileHash = "hash2" },
            new() { FileName = "file5.txt", FileHash = "hash5" },
        };

        _metadataB = new List<FileMetadata>
        {
            new() { FileName = "file2.txt", FileHash = "hash2" },
            new() { FileName = "file3.txt", FileHash = "hash3" },
            new() { FileName = "file4.txt", FileHash = "hash1" },
        };
    }

    /// <summary>
    /// Verifies that CompareMetadata identifies differences correctly between metadata sets A and B.
    /// </summary>
    [TestMethod]
    public void TestCompareMetadataShouldIdentifyDifferences()
    {
        // Arrange: Create an instance of the comparer
        var comparer = new DirectoryMetadataComparer(_metadataA!, _metadataB!);

        // Act: Retrieve differences between the two metadata sets
        List<MetadataDifference> differences = comparer.Differences;

        // Assert: Verify differences match expectations
        Assert.AreEqual(1, differences.First(d => d.Key == "-1").Value.Count, "Expected 1 file missing in A.");
        Assert.AreEqual(1, differences.First(d => d.Key == "0").Value.Count, "Expected 1 file common between A and B.");
        Assert.AreEqual(1, differences.First(d => d.Key == "1").Value.Count, "Expected 1 file missing in B.");
    }

    /// <summary>
    /// Verifies that CheckForRenamesAndMissingFiles identifies files missing in metadata set A.
    /// </summary>
    [TestMethod]
    public void TestCheckForRenamesAndMissingFilesShouldIdentifyMissingFilesInA()
    {
        // Arrange: Create an instance of the comparer
        var comparer = new DirectoryMetadataComparer(_metadataA!, _metadataB!);

        // Act: Retrieve unique client-side files
        List<string> uniqueClientFiles = comparer.UniqueClientFiles;

        // Assert: Verify missing files in A are identified correctly
        Assert.AreEqual(1, uniqueClientFiles.Count, "Expected 1 file missing in A.");
        Assert.AreEqual("file3.txt", uniqueClientFiles.First(), "Expected file3.txt to be identified as missing.");
    }

    /// <summary>
    /// Verifies that CheckForOnlyInAFiles identifies files missing in metadata set B.
    /// </summary>
    [TestMethod]
    public void TestCheckForOnlyInAFilesShouldIdentifyMissingFilesInB()
    {
        // Arrange: Create an instance of the comparer
        var comparer = new DirectoryMetadataComparer(_metadataA!, _metadataB!);

        // Act: Retrieve unique server-side files
        List<string> uniqueServerFiles = comparer.UniqueServerFiles;

        // Assert: Verify missing files in B are identified correctly
        Assert.AreEqual(1, uniqueServerFiles.Count, "Expected 1 file missing in B.");
        Assert.AreEqual("file5.txt", uniqueServerFiles.First(), "Expected file5.txt to be identified as missing.");
    }

    /// <summary>
    /// Verifies that CheckForSameNameDifferentHash identifies files with hash mismatches.
    /// </summary>
    [TestMethod]
    public void TestCheckForSameNameDifferentHashShouldIdentifyFileHashMismatch()
    {
        // Arrange: Create an instance of the comparer
        var comparer = new DirectoryMetadataComparer(_metadataA!, _metadataB!);

        // Act: Retrieve files with hash mismatches
        List<string> invalidSyncUpFiles = comparer.InvalidSyncUpFiles ?? new List<string>();

        // Assert: Verify that no files have hash mismatches
        Assert.AreEqual(0, invalidSyncUpFiles.Count, "Expected no hash mismatches.");
    }

    /// <summary>
    /// Verifies that ValidateSync returns true when no invalid files exist.
    /// </summary>
    [TestMethod]
    public void TestValidateSyncShouldReturnTrueWhenNoInvalidFilesExist()
    {
        // Arrange: Create an instance of the comparer
        var comparer = new DirectoryMetadataComparer(_metadataA!, _metadataB!);

        // Act: Check if synchronization is valid
        bool canSync = comparer.ValidateSync();

        // Assert: Verify synchronization is allowed
        Assert.IsTrue(canSync, "Expected synchronization to be valid.");
    }

    /// <summary>
    /// Verifies that ValidateSync returns false when invalid files exist.
    /// </summary>
    [TestMethod]
    public void TestValidateSyncShouldReturnFalseWhenInvalidFilesExist()
    {
        // Arrange: Modify metadataB to introduce invalid files
        var metadataBInvalidFiles = new List<FileMetadata>
        {
            new() { FileName = "file1.txt", FileHash = "hash1" },
            new() { FileName = "file2.txt", FileHash = "hash2" },
            new() { FileName = "file5.txt", FileHash = "hash7" }
        };
        var comparer = new DirectoryMetadataComparer(_metadataA!, metadataBInvalidFiles);

        // Act: Check if synchronization is valid
        bool canSync = comparer.ValidateSync();

        // Assert: Verify synchronization is disallowed
        Assert.IsFalse(canSync, "Expected synchronization to be invalid due to hash mismatches.");
    }
}
