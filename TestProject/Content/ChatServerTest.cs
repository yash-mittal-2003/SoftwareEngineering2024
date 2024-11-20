using Moq;
using Networking.Communication;
using NUnit.Framework;
using System.Net.Sockets;
using System.Reflection;

namespace ChatApplication.Tests
{
    [TestFixture]
    public class ChatServerTests
    {
        private Mock<ICommunicator> _mockCommunicator;
        private ChatServer _chatServer;

        [SetUp]
        public void SetUp()
        {
            // Mock the ICommunicator
            _mockCommunicator = new Mock<ICommunicator>();
            // Create the ChatServer instance using the default constructor
            _chatServer = new ChatServer();

            // Use reflection to set the private _communicator field with the mock
            var communicatorField = typeof(ChatServer).GetField("_communicator", BindingFlags.NonPublic | BindingFlags.Instance);
            communicatorField.SetValue(_chatServer, _mockCommunicator.Object);
        }

        

        [Test]
        public void OnDataReceived_ShouldHandlePrivateMessage_WhenMessageTypeIsPrivate()
        {
            // Arrange
            var data = "private|Hello|User1|0|1";
            _mockCommunicator.Setup(c => c.Send(It.IsAny<string>(), "ChatModule", "1"));

            // Act
            _chatServer.OnDataReceived(data);

            // Assert
            _mockCommunicator.Verify(c => c.Send(It.Is<string>(s => s.Contains("User1 :.: Hello")), "ChatModule", "1"), Times.Once);
        }

        [Test]
        public void OnDataReceived_ShouldHandleBroadcastMessage_WhenMessageTypeIsNotPrivate()
        {
            // Arrange
            var data = "broadcast|Hello everyone|User1|0";
            _mockCommunicator.Setup(c => c.Send(It.IsAny<string>(), "ChatModule", null));

            // Act
            _chatServer.OnDataReceived(data);

            // Assert
            _mockCommunicator.Verify(c => c.Send(It.Is<string>(s => s.Contains("User1 :.: Hello everyone")), "ChatModule", null), Times.Once);
        }

        [Test]
        public void OnDataReceived_ShouldHandleConnectMessage_WhenMessageTypeIsConnect()
        {
            // Arrange
            var data = "connect|unused|User1|0";
            //_chatServer.clientId = "0"; // Mock the client ID

            // Act
            _chatServer.OnDataReceived(data);

            // Assert
            NUnit.Framework.Assert.That(_chatServer._clientUsernames.ContainsKey(0));
            NUnit.Framework.Assert.That(_chatServer._clientUsernames[0], Is.EqualTo("User1"));

            _mockCommunicator.Verify(c => c.Send(It.Is<string>(s => s.StartsWith("clientlist|")), "ChatModule", null), Times.Once);
        }

        [Test]
        public void OnDataReceived_ShouldIgnoreInvalidData_WhenDataIsIncomplete()
        {
            // Arrange
            var invalidData = "invalid|data";

            // Act
            _chatServer.OnDataReceived(invalidData);

            // Assert
            _mockCommunicator.Verify(c => c.Send(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void Stop_ShouldStopCommunicator()
        {
            // Act
            _chatServer.Stop();

            // Assert
            _mockCommunicator.Verify(c => c.Stop(), Times.Once);
        }

    }
}
