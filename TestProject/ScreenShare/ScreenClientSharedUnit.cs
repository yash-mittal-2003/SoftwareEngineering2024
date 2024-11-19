using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Screenshare.ScreenShareServer;
using System.Collections.Generic;
using System.Drawing;
using Screenshare;
namespace ScreenShare.Tests
{
    [TestClass]
    public class SharedClientScreenTests
    {
        [TestMethod]
        public void Constructor_ValidParameters_ShouldInitializeCorrectly()
        {
            // Arrange
            var mockServer = new Mock<ITimerManager>();
            string clientId = "client123";
            string clientName = "Test Client";

            // Act
            var clientScreen = new SharedClientScreen(clientId, clientName, mockServer.Object);

            // Assert
            Assert.AreEqual(clientId, clientScreen.Id);
            Assert.AreEqual(clientName, clientScreen.Name);
            Assert.IsFalse(clientScreen.Pinned);
            Assert.AreEqual(0, clientScreen.TileHeight);
            Assert.AreEqual(0, clientScreen.TileWidth);
            Assert.IsNull(clientScreen.CurrentImage);
        }


        [TestMethod]
        public void StopProcessing_ShouldClearQueuesAndStopStitching()
        {
            // Arrange
            var mockServer = new Mock<ITimerManager>();
            var clientScreen = new SharedClientScreen("client123", "Test Client", mockServer.Object);
            clientScreen.StartProcessing(taskId => { });

            // Act
            clientScreen.StopProcessing();

            // Assert
            Assert.IsNull(clientScreen.CurrentImage);
            Assert.AreEqual(0, clientScreen.TileHeight);
            Assert.AreEqual(0, clientScreen.TileWidth);
        }


        [TestMethod]
        public void PutImage_And_GetImage_ShouldWorkCorrectly()
        {
            // Arrange
            var mockServer = new Mock<ITimerManager>();
            var clientScreen = new SharedClientScreen("client123", "Test Client", mockServer.Object);
            clientScreen.StartProcessing(taskId => { });
            var pixelDifferences = new List<PixelDifference>
            {
                new PixelDifference(1, 1, 0, 0, 0, 255)
            };
            string image = "TestImage";

            // Act
            clientScreen.PutImage(image, clientScreen.TaskId, pixelDifferences);
            var result = clientScreen.GetImage(clientScreen.TaskId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(image, result?.Item1);
            Assert.AreEqual(pixelDifferences, result?.Item2);
        }

        [TestMethod]
        public void PutFinalImage_And_GetFinalImage_ShouldWorkCorrectly()
        {
            // Arrange
            var mockServer = new Mock<ITimerManager>();
            var clientScreen = new SharedClientScreen("client123", "Test Client", mockServer.Object);
            clientScreen.StartProcessing(taskId => { });
            var bitmap = new Bitmap(100, 100);

            // Act
            clientScreen.PutFinalImage(bitmap, clientScreen.TaskId);
            var result = clientScreen.GetFinalImage(clientScreen.TaskId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(bitmap, result);
        }
    }
}
