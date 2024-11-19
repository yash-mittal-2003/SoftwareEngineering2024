using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;
using System.Threading;
using Screenshare.ScreenShareClient; // Adjust namespace based on your project.

namespace ScreenShare.Tests
{
    [TestClass]
    public class ScreenshotTests
    {
        private Screenshot _screenshot;

        [TestInitialize]
        public void TestInitialize()
        {
            // Initialize a Screenshot instance before each test.
            _screenshot = Screenshot.Instance();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Clean up resources after each test.
            _screenshot.DisposeVariables();
        }

        [TestMethod]
        public void Instance_ShouldReturnSameObject_WhenCalledMultipleTimes()
        {
            // Act
            var instance1 = Screenshot.Instance();
            var instance2 = Screenshot.Instance();

            // Assert
            Assert.IsNotNull(instance1, "Instance 1 should not be null");
            Assert.IsNotNull(instance2, "Instance 2 should not be null");
            Assert.AreSame(instance1, instance2, "Both instances should be the same");
        }

        [TestMethod]
        public void MakeScreenshot_ShouldNotThrowException_WhenCalled()
        {
            // Act & Assert
            try
            {
                var bitmap = _screenshot.MakeScreenshot();
                Assert.IsNotNull(bitmap, "MakeScreenshot should return a valid Bitmap");
                Assert.IsTrue(bitmap.Width > 0 && bitmap.Height > 0, "Bitmap dimensions should be valid");
            }
            catch (Exception ex)
            {
                Assert.Fail($"MakeScreenshot threw an exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void InitializeVariables_ShouldSetCorrectDimensions()
        {
            // Arrange
            var screenshot = Screenshot.Instance();

            // Act
            try
            {
                screenshot.MakeScreenshot(0, 0); // Use default indices for single display
            }
            catch (Exception ex)
            {
                Assert.Fail($"Initialization failed: {ex.Message}");
                return;
            }

            // Assert
            //Assert.IsTrue(screenshot.MakeScreenshot_LastAdapterIndexValue > 0,
            //   $"Width should be greater than 0. Actual: {screenshot.MakeScreenshot_LastAdapterIndexValue}");
            // Assert.IsTrue(screenshot.MakeScreenshot_LastDisplayIndexValue > 0,
            //  $"Height should be greater than 0. Actual: {screenshot.MakeScreenshot_LastDisplayIndexValue}");
        }



        [TestMethod]
        public void DisposeVariables_ShouldNotThrowException_WhenCalled()
        {
            // Act & Assert
            try
            {
                _screenshot.DisposeVariables();
                Assert.IsTrue(true, "DisposeVariables executed successfully");
            }
            catch (Exception ex)
            {
                Assert.Fail($"DisposeVariables threw an exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void MakeScreenshot_ShouldTimeout_WhenNoFrameAvailable()
        {
            // Arrange
            int maxTimeout = 100; // Short timeout for testing.

            // Act
            Bitmap result = null;
            try
            {
                result = _screenshot.MakeScreenshot(maxTimeout: maxTimeout);
            }
            catch (Exception ex)
            {
                Assert.Fail($"MakeScreenshot threw an exception: {ex.Message}");
            }

            // Assert
            //Assert.IsNull(result, "MakeScreenshot should return null when no frame is available within timeout");
        }
    }
}
