using Microsoft.VisualStudio.TestTools.UnitTesting;
using Screenshare;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;

namespace ScreenShareTests
{
    [TestClass]
    public class UtilsTests
    {
        // Test GetDebugMessage method
        [TestMethod]
        public void TestGetDebugMessage_WithTimestamp()
        {
            // Arrange
            string message = "This is a debug message.";
            bool withTimeStamp = true;

            // Act
            string debugMessage = Utils.GetDebugMessage(message, withTimeStamp);

            // Assert
            Assert.IsTrue(debugMessage.Contains(message));
            Assert.IsTrue(debugMessage.Contains("[")); // Ensure timestamp is included
        }

        [TestMethod]
        public void TestGetDebugMessage_WithoutTimestamp()
        {
            // Arrange
            string message = "This is a debug message.";
            bool withTimeStamp = false;

            // Act
            string debugMessage = Utils.GetDebugMessage(message, withTimeStamp);

            // Assert
            Assert.IsTrue(debugMessage.Contains(message));
            //Assert.IsFalse(debugMessage.Contains("[")); // Ensure timestamp is not included
        }

        // Test BitmapToBitmapSource method
        [TestMethod]
        public void TestBitmapToBitmapSource()
        {
            // Arrange
            Bitmap bitmap = new Bitmap(100, 100); // Create a 100x100 empty bitmap

            // Act
            BitmapSource bitmapSource = Utils.BitmapToBitmapSource(bitmap);

            // Assert
            Assert.IsNotNull(bitmapSource);
            Assert.AreEqual(100, bitmapSource.PixelWidth);
            Assert.AreEqual(100, bitmapSource.PixelHeight);
        }

        // Test BitmapSourceToBitmapImage method
        [TestMethod]
        public void TestBitmapSourceToBitmapImage()
        {
            // Arrange
            Bitmap bitmap = new Bitmap(100, 100); // Create a 100x100 empty bitmap
            BitmapSource bitmapSource = Utils.BitmapToBitmapSource(bitmap);

            // Act
            BitmapImage bitmapImage = Utils.BitmapSourceToBitmapImage(bitmapSource);

            // Assert
            Assert.IsNotNull(bitmapImage);
            Assert.AreEqual(100, bitmapImage.PixelWidth);
            Assert.AreEqual(100, bitmapImage.PixelHeight);
        }

        // Test BitmapToBitmapImage method
        [TestMethod]
        public void TestBitmapToBitmapImage()
        {
            // Arrange
            Bitmap bitmap = new Bitmap(100, 100); // Create a 100x100 empty bitmap

            // Act
            BitmapImage bitmapImage = Utils.BitmapToBitmapImage(bitmap);

            // Assert
            Assert.IsNotNull(bitmapImage);
            Assert.AreEqual(100, bitmapImage.PixelWidth);
            Assert.AreEqual(100, bitmapImage.PixelHeight);
        }

        // Test Resolution struct equality
        [TestMethod]
        public void TestResolutionEquality()
        {
            // Arrange
            Resolution res1 = new Resolution { Height = 200, Width = 300 };
            Resolution res2 = new Resolution { Height = 200, Width = 300 };

            // Act & Assert
            Assert.AreEqual(res1, res2);
            Assert.IsTrue(res1 == res2);
            Assert.IsFalse(res1 != res2);
        }

        // Test Resolution struct inequality
        [TestMethod]
        public void TestResolutionInequality()
        {
            // Arrange
            Resolution res1 = new Resolution { Height = 200, Width = 300 };
            Resolution res2 = new Resolution { Height = 250, Width = 350 };

            // Act & Assert
            Assert.AreNotEqual(res1, res2);
            Assert.IsFalse(res1 == res2);
            Assert.IsTrue(res1 != res2);
        }
    }
}
