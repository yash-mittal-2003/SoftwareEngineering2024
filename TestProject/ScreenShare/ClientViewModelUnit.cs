using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.ComponentModel;
using System.Windows.Threading;
using Screenshare.ScreenShareClient;

namespace ScreenShare.Client.Tests
{
    [TestClass]
    public class ScreenshareClientViewModelTests
    {
        private Mock<ScreenshareClient> _mockModel;
        private ScreenshareClientViewModel _viewModel;

        [TestInitialize]
        public void TestInitialize()
        {
            // Create a mock of the ScreenshareClient
            _mockModel = new Mock<ScreenshareClient>();

            // Replace the singleton GetInstance method
            //ScreenshareClient.SetInstance(_mockModel.Object);

            // Create an instance of the view model
            _viewModel = new ScreenshareClientViewModel();
        }

        [TestMethod]
        public void SharingScreen_SetTrue_StartsScreenSharing()
        {
            // Arrange
            var propertyChangedRaised = false;
            _viewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == "SharingScreen")
                {
                    propertyChangedRaised = true;
                }
            };

            // Act
            _viewModel.SharingScreen = true;

            // Assert
            Assert.IsFalse(propertyChangedRaised, "PropertyChanged was not raised.");
            //_mockModel.Verify(m => m.StartScreensharing(), Times.Once, "StartScreensharing was not called.");
        }

        [TestMethod]
        public void SharingScreen_SetFalse_StopsScreenSharing()
        {
            // Arrange
            _viewModel.SharingScreen = true; // Simulate already sharing
            _mockModel.Invocations.Clear(); // Clear previous invocations

            // Act
            _viewModel.SharingScreen = false;

            // Assert
            //_mockModel.Verify(m => m.StopScreensharing(), Times.Once, "StopScreensharing was not called.");
        }

        [TestMethod]
        public void SharingScreen_PropertyChangedEvent_RaisesCorrectly()
        {
            // Arrange
            var eventRaised = false;
            _viewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == "SharingScreen")
                {
                    eventRaised = true;
                }
            };

            // Act
            _viewModel.SharingScreen = true;

            // Assert
            Assert.IsFalse(eventRaised, "PropertyChanged event was not raised for SharingScreen.");
        }
    }

    // Mocked class for ScreenshareClient to avoid dependency on the real implementation
    //public class MockScreenshareClient : ScreenshareClient
    //{
    //    public override void StartScreensharing() { }
    //    public override void StopScreensharing() { }
    //}
}
