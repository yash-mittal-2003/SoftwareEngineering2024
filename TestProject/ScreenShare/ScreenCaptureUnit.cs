using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;
using System.Threading;
using Screenshare.ScreenShareClient;


namespace ScreenShare.Client.Tests
{
    [TestClass]
    public class ScreenCapturerTests
    {
        public ScreenCapturer _screenCapturer;

        [TestInitialize]
        public void TestInitialize()
        {
            _screenCapturer = new ScreenCapturer();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Ensuring StopCapture is called only after StartCapture
            try
            {
                _screenCapturer.StopCapture();
            }
            catch (Exception ex)
            {
                Assert.Fail($"StopCapture threw an exception: {ex.Message}");
            }
        }

        [TestMethod]
        public void StartCapture_ShouldEnqueueFrames()
        {
            // Arrange
            int initialQueueLength = _screenCapturer.GetCapturedFrameLength();

            // Act
            _screenCapturer.StartCapture();
            Thread.Sleep(1000); // Allow time for frames to be captured
            int finalQueueLength = _screenCapturer.GetCapturedFrameLength();

            // Assert
            Assert.IsTrue(finalQueueLength > initialQueueLength, "Frames were not enqueued during capture.");
        }

        [TestMethod]
        public void StopCapture_ShouldClearQueueAndStopTask()
        {
            // Arrange
            _screenCapturer.StartCapture();
            Thread.Sleep(1000); // Allow time for frames to be captured
            Assert.IsTrue(_screenCapturer.GetCapturedFrameLength() > 0, "Queue should not be empty before stopping.");

            // Act
            _screenCapturer.StopCapture();

            // Assert
            Assert.AreEqual(0, _screenCapturer.GetCapturedFrameLength(), "Queue was not cleared after stopping capture.");
        }

        [TestMethod]
        public void GetImage_ShouldReturnBitmap()
        {
            // Arrange
            bool cancellationToken = false;
            _screenCapturer.StartCapture();
            Thread.Sleep(1000); // Allow time for at least one frame to be captured

            // Act
            Bitmap? image = _screenCapturer.GetImage(ref cancellationToken);

            // Assert
            Assert.IsNotNull(image, "GetImage did not return a valid bitmap.");
            Assert.AreEqual(typeof(Bitmap), image.GetType(), "Returned object is not a Bitmap.");
        }





        [TestMethod]
        public void GetCapturedFrameLength_ShouldReflectQueueSize()
        {
            // Arrange
            _screenCapturer.StartCapture();
            Thread.Sleep(1000); // Allow time for frames to be captured
            int queueLengthAfterCapture = _screenCapturer.GetCapturedFrameLength();

            // Act
            _screenCapturer.StopCapture();
            int queueLengthAfterStop = _screenCapturer.GetCapturedFrameLength();

            // Assert
            Assert.IsTrue(queueLengthAfterCapture > 0, "Captured frame queue length should be greater than 0 during capture.");
            Assert.AreEqual(0, queueLengthAfterStop, "Captured frame queue should be empty after stopping capture.");
        }
    }
}
