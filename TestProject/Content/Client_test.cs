using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Networking.Communication;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace Content.Tests
{
    [TestClass]
    public class ChatClientTests
    {
        private Mock<ICommunicator> _mockCommunicator;
        private ChatClient _chatClient;

        [TestInitialize]
        public void Setup()
        {
            // Mock the communicator
            _mockCommunicator = new Mock<ICommunicator>();
            CommunicationFactory.SetCommunicatorMock(_mockCommunicator.Object);

            // Initialize ChatClient
            _chatClient = new ChatClient();
            _chatClient.Username = "TestUser";
            _chatClient.ClientId = "1";
        }






        [TestMethod]
        public void OnDataReceived_ShouldInvokeMessageReceivedEvent()
        {
            // Arrange
            string serializedData = "public|Hello World|User1|123";
            string receivedMessage = null;

            _chatClient.MessageReceived += (sender, message) => receivedMessage = message;

            // Act
            _chatClient.OnDataReceived(serializedData);

            // Assert
            Assert.AreEqual(serializedData, receivedMessage);
        }




        [TestMethod]
        public void OnDataReceived_ShouldIgnoreInvalidMessageFormat()
        {
            // Arrange
            string serializedData = "invalid|message";

            // Act
            _chatClient.OnDataReceived(serializedData);

            // Assert
            _mockCommunicator.Verify(comm => comm.Send(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }





        [TestMethod]
        public void Constructor_ShouldInitializeClientListObs()
        {
            // Assert
            Assert.IsNotNull(_chatClient._clientListobs);
            Assert.IsInstanceOfType(_chatClient._clientListobs, typeof(ObservableCollection<string>));
        }
    }

    // Helper class to mock CommunicationFactory
    public static class CommunicationFactory
    {
        private static ICommunicator _mockCommunicator;

        public static ICommunicator GetCommunicator(bool isMocked)
        {
            return _mockCommunicator;
        }

        public static void SetCommunicatorMock(ICommunicator mockCommunicator)
        {
            _mockCommunicator = mockCommunicator;
        }
    }
}
