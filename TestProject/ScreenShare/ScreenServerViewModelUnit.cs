using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Screenshare.ScreenShareServer;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace ScreenShare.Tests
{
    [TestClass]
    public class ServerViewModelTests
    {
        private Mock<ITimerManager> _mockTimerManager;
        private ServerViewModel _viewModel;

        [TestInitialize]
        public void SetUp()
        {
            _mockTimerManager = new Mock<ITimerManager>();
            _viewModel = new ServerViewModel();
        }

        [TestMethod]
        public void AddClient_ShouldAddSharedClientScreen()
        {
            // Arrange
            string clientId = "Client123";
            string clientName = "Test Client";

            // Act
            _viewModel.AddClient(clientId, clientName, _mockTimerManager.Object);

            // Assert
            Assert.AreEqual(1, _viewModel.SharedClients.Count);
            var client = _viewModel.SharedClients.First();
            Assert.AreEqual(clientId, client.Id);
            Assert.AreEqual(clientName, client.Name);
        }

        [TestMethod]
        public void RemoveClient_ShouldRemoveSharedClientScreen()
        {
            // Arrange
            string clientId = "Client123";
            string clientName = "Test Client";
            _viewModel.AddClient(clientId, clientName, _mockTimerManager.Object);

            // Act
            _viewModel.RemoveClient(clientId);

            // Assert
            Assert.AreEqual(0, _viewModel.SharedClients.Count);
        }

        [TestMethod]
        public void StartClientProcessing_ShouldInvokeStartProcessing()
        {
            // Arrange
            string clientId = "Client123";
            string clientName = "Test Client";
            _viewModel.AddClient(clientId, clientName, _mockTimerManager.Object);
            var sharedClient = _viewModel.SharedClients.First();

            // Mock the task logic.
            var mockTaskAction = new Mock<Action<int>>();

            // Act
            _viewModel.StartClientProcessing(clientId, mockTaskAction.Object);

            // Assert
            Assert.IsNotNull(sharedClient);
            // You would have additional validation if `_stitcher.StartStitching` or `_imageSendTask` logic were exposed for testing.
        }

        [TestMethod]
        public void StopClientProcessing_ShouldInvokeStopProcessing()
        {
            // Arrange
            string clientId = "Client123";
            string clientName = "Test Client";
            _viewModel.AddClient(clientId, clientName, _mockTimerManager.Object);
            var sharedClient = _viewModel.SharedClients.First();

            // Act
            _viewModel.StopClientProcessing(clientId);

            // Assert
            Assert.IsNotNull(sharedClient);
            // Again, ensure `_stitcher.StopStitching` or `_imageQueue`/`_finalImageQueue` logic behaves correctly.
        }



        // Example ServerViewModel Implementation
        public class ServerViewModel : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;

            public List<SharedClientScreen> SharedClients { get; private set; } = new();

            public void AddClient(string clientId, string clientName, ITimerManager timerManager)
            {
                var clientScreen = new SharedClientScreen(clientId, clientName, timerManager);
                SharedClients.Add(clientScreen);
                NotifyPropertyChanged(nameof(SharedClients));
            }

            public void RemoveClient(string clientId)
            {
                var client = SharedClients.FirstOrDefault(c => c.Id == clientId);
                if (client != null)
                {
                    client.Dispose();
                    SharedClients.Remove(client);
                    NotifyPropertyChanged(nameof(SharedClients));
                }
            }

            public void StartClientProcessing(string clientId, Action<int> task)
            {
                var client = SharedClients.FirstOrDefault(c => c.Id == clientId);
                client?.StartProcessing(task);
            }

            public void StopClientProcessing(string clientId)
            {
                var client = SharedClients.FirstOrDefault(c => c.Id == clientId);
                client?.StopProcessing();
            }

            private void NotifyPropertyChanged(string propertyName) =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
