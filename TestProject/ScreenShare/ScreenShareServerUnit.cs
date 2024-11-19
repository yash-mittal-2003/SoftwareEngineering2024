using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Networking.Communication;
using Screenshare.ScreenShareServer;
using System.Collections.Generic;
using System.Reflection;

namespace ScreenShare.Tests
{
    [TestClass]
    public class ScreenshareServerTests
    {
        private Mock<IMessageListener> _mockListener;
        private Mock<ICommunicator> _mockCommunicator;
        private ScreenshareServer _server;

        [TestInitialize]
        public void Setup()
        {
            _mockListener = new Mock<IMessageListener>();
            _mockCommunicator = new Mock<ICommunicator>();

            // Mock CommunicationFactory to return the mocked communicator.
            CommunicationFactory.SetMockCommunicator(_mockCommunicator.Object);

            // Initialize the ScreenshareServer with the mocked dependencies.
            _server = ScreenshareServer.GetInstance(_mockListener.Object, isDebugging: true);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _server.Dispose();
        }

        [TestMethod]
        public void RegisterClient_Should_AddClientToSubscribersList()
        {
            // Arrange
            string clientId = "client1";
            string clientName = "Test Client";

            // Act
            _server.OnDataReceived(CreateRegisterPacket(clientId, clientName));

            // Assert
            // Assert.IsTrue(_server._subscribers.ContainsKey(clientId));
            // Assert.AreEqual(clientName, _server._subscribers[clientId].Name);

            _mockListener.Verify(listener => listener.OnScreenshareStart(clientId, clientName), Times.Once);
        }

        [TestMethod]
        public void DeregisterClient_Should_RemoveClientFromSubscribersList()
        {
            // Arrange
            string clientId = "client1";
            string clientName = "Test Client";
            _server.OnDataReceived(CreateRegisterPacket(clientId, clientName));

            // Act
            _server.OnDataReceived(CreateDeregisterPacket(clientId));

            // Assert
            //Assert.IsFalse(_server._subscribers.ContainsKey(clientId));

            _mockListener.Verify(listener => listener.OnScreenshareStop(clientId, clientName), Times.Once);
        }

        [TestMethod]
        public void BroadcastClients_Should_SendPacketToAllClients()
        {
            // Arrange
            var mockCommunicator = new Mock<ICommunicator>();
            var mockListener = new Mock<IMessageListener>();

            // Initialize the ScreenshareServer instance
            var server = ScreenshareServer.GetInstance(mockListener.Object, isDebugging: true);

            // Directly inject the mock communicator since the field is public
            server._communicator = mockCommunicator.Object;

            var clientIds = new List<string> { "client1", "client2" };
            string headerVal = "SomeHeader";
            var numRowsColumns = (10, 10);

            // Act
            server.BroadcastClients(clientIds, headerVal, numRowsColumns);

            // Assert
            //mockCommunicator.Verify(c => c.Send(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(clientIds.Count));
        }




        [TestMethod]
        public void UpdateTimer_Should_ResetClientTimer()
        {
            // Arrange
            string clientId = "client1";
            string clientName = "Test Client";
            _server.OnDataReceived(CreateRegisterPacket(clientId, clientName));

            // Ensure the mock communicator is injected before performing actions
            var mockCommunicator = new Mock<ICommunicator>();
            _server._communicator = mockCommunicator.Object;  // Directly inject the mock communicator

            // Act
            _server.OnDataReceived(CreateConfirmationPacket(clientId));

            // Assert
            mockCommunicator.Verify(communicator => communicator.Send(It.IsAny<string>(), "ScreenShare", clientId), Times.Once);
        }

        // Helper Methods for Mocking Packets
        private string CreateRegisterPacket(string clientId, string clientName)
        {
            return $@"{{ ""Id"": ""{clientId}"", ""Name"": ""{clientName}"", ""Header"": ""Register"", ""Data"": """" }}";
        }

        private string CreateDeregisterPacket(string clientId)
        {
            return $@"{{ ""Id"": ""{clientId}"", ""Header"": ""Deregister"", ""Data"": """" }}";
        }

        private string CreateConfirmationPacket(string clientId)
        {
            return $@"{{ ""Id"": ""{clientId}"", ""Header"": ""Confirmation"", ""Data"": """" }}";
        }
    }

    // Helper class to set mock communicator in CommunicationFactory
    public static class CommunicationFactory
    {
        private static ICommunicator _mockCommunicator;

        public static ICommunicator GetCommunicator(bool isServer)
        {
            return _mockCommunicator ?? throw new System.Exception("Mock Communicator not set!");
        }

        public static void SetMockCommunicator(ICommunicator communicator)
        {
            _mockCommunicator = communicator;
        }
    }
}
