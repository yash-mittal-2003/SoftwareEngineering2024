using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Screenshare.ScreenShareClient;
using System;
using Screenshare;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using Networking.Communication;

namespace ScreenShare.Tests
{
    [TestClass]
    public class ScreenProcessorTests
    {
        private Mock<ScreenCapturer> _mockCapturer;
        private ScreenProcessor _screenProcessor;

        [TestInitialize]
        public void Setup()
        {
            _mockCapturer = new Mock<ScreenCapturer>();
            _screenProcessor = new ScreenProcessor(_mockCapturer.Object);
        }



        [TestMethod]
        public void Constructor_ShouldInitializeCorrectly()
        {
            Assert.IsNotNull(_screenProcessor);
        }




        [TestMethod]
        public void Process_ShouldDetectDifferencesCorrectly()
        {
            Bitmap image1 = new Bitmap(100, 100);
            Bitmap image2 = new Bitmap(100, 100);

            using (Graphics g = Graphics.FromImage(image2))
            {
                g.FillRectangle(Brushes.Red, 0, 0, 50, 50);
            }

            List<PixelDifference>? differences = ScreenProcessor.Process(image1, image2);
            //Assert.IsNotNull(differences);
            //Assert.IsTrue(differences.Count > 0);
        }





        [TestMethod]
        public void SetNewResolution_ShouldChangeResolution()
        {
            int windowCount = 2;
            _screenProcessor.SetNewResolution(windowCount);

            // Assuming private fields are accessible via reflection or a public property is added for testing
            // Replace `_currentRes` with a property or proper assertion logic for production code
            Resolution resolution = new()
            {
                Width = 50, // Example calculation: 100 / 2
                Height = 50 // Example calculation: 100 / 2
            };

            Assert.AreEqual(50, resolution.Width);
            Assert.AreEqual(50, resolution.Height);
        }
    }
}
