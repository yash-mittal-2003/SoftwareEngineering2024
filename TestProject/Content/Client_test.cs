using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Networking.Communication;

namespace ChatApplication.Tests
{
    public class ChatClientTests
    {
        private readonly Mock<ICommunicator> _mockCommunicator;
        private readonly ChatClient _chatClient;

        public ChatClientTests()
        {
            _mockCommunicator = new Mock<ICommunicator>();
            _chatClient = new ChatClient
            {
                Username = "TestUser"
            };

            // Inject mock communicator
            typeof(ChatClient).GetField("_communicator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                              ?.SetValue(_chatClient, _mockCommunicator.Object);
        }

        [Fact]
        public void Start_SuccessfulConnection_SendsConnectMessage()
        {
            // Arrange
            _mockCommunicator.Setup(c => c.Start(It.IsAny<string>(), It.IsAny<string>()))
                             .Returns("success");

            // Act
            _chatClient.Start("10.32.11.43", "5000");

            // Assert
            _mockCommunicator.Verify(c => c.Send(It.Is<string>(msg => msg.Contains("connect")), "ChatModule", null), Times.Once);
        }

        [Fact]
        public void SendMessage_PublicMessage_CallsCommunicatorSend()
        {
            // Arrange
            string message = "Hello, world!";
            string expectedFormat = "public|Hello, world!|TestUser||";

            // Act
            _chatClient.SendMessage(message);

            // Assert
            _mockCommunicator.Verify(c => c.Send(expectedFormat, "ChatModule", null), Times.Once);
        }



        [Fact]
        public void OnDataReceived_PrivateMessage_InvokesMessageReceived()
        {
            // Arrange
            string serializedData = "message|private|User1|Hello, TestUser";
            string receivedMessage = null;

            _chatClient.MessageReceived += (sender, message) => receivedMessage = message;

            // Act
            _chatClient.OnDataReceived(serializedData);

            // Assert
            Xunit.Assert.Equal("Private from User1 : Hello, TestUser", receivedMessage);
        }

        [Fact]
        public void OnDataReceived_PublicMessage_InvokesMessageReceived()
        {
            // Arrange
            string serializedData = "message|public|User1|Hello, everyone";
            string receivedMessage = null;

            _chatClient.MessageReceived += (sender, message) => receivedMessage = message;

            // Act
            _chatClient.OnDataReceived(serializedData);

            // Assert
            Xunit.Assert.Equal(serializedData, receivedMessage);
        }

        
    }
}
