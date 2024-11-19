using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Screenshare.ScreenShareServer;
using System;
using Screenshare;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ScreenShare.Tests
{
    [TestClass]
    public class ScreenStitcherTests
    {


        [TestMethod]
        public void DecompressByteArray_ValidCompressedData_ShouldReturnDecompressedData()
        {
            // Arrange
            byte[] originalData = System.Text.Encoding.UTF8.GetBytes("Test Data");
            using MemoryStream compressedStream = new();
            using (var compressor = new System.IO.Compression.DeflateStream(compressedStream, System.IO.Compression.CompressionMode.Compress))
            {
                compressor.Write(originalData, 0, originalData.Length);
            }
            byte[] compressedData = compressedStream.ToArray();

            // Act
            byte[] decompressedData = ScreenStitcher.DecompressByteArray(compressedData);

            // Assert
            string decompressedString = System.Text.Encoding.UTF8.GetString(decompressedData);
            Assert.AreEqual("Test Data", decompressedString);
        }


    }

    // Mock for SharedClientScreen
    public class MockSharedClientScreen
    {
        public string Log { get; private set; } = string.Empty;

        public int TaskId => 1;

        public (string, List<PixelDifference>)? GetImage(int taskId)
        {
            return ("MockImageData", new List<PixelDifference>());
        }

        public void PutFinalImage(Bitmap image, int taskId)
        {
            Log += "Image processed and stored.";
        }
    }
}
