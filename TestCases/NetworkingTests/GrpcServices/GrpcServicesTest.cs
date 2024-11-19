using Networking.Communication;
using Networking;
using System.Net.Sockets;
using Networking.GrpcServices;

namespace NetworkingTests.GrpcServices;

public class ClientServicesTests
{
    public class GrpcServicesTest : INotificationHandler
    {
        private ICommunicator server;
        private ICommunicator client;
        private int id = 0;
        private bool messageReceived;

        public GrpcServicesTest()
        {
            server = CommunicationFactory.GetCommunicator(false, true);
            client = CommunicationFactory.GetCommunicator(true, true);
            messageReceived = false;
        }

        /// <summary>
        /// Verifies that the server starts successfully and returns a valid address.
        /// </summary>
        [Fact]
        public void ServerInitializationTest()
        {
            // Act
            string serverAddress = server.Start();

            // Assert
            Assert.NotNull(serverAddress);
            Assert.Contains(":", serverAddress); // Format: IP:Port
        }

        /// <summary>
        /// Ensures that the client instance is initialized correctly.
        /// </summary>
        [Fact]
        public void ClientInitializationTest()
        {
            // Assert
            Assert.NotNull(client);
            Assert.IsType<ClientServices>(client); // Assuming ClientServices is the concrete class
        }

        /// <summary>
        /// Validates that the client can successfully connect to the server.
        /// </summary>
        [Fact]
        public void ClientConnectionTest()
        {
            // Arrange
            string serverAddress = server.Start();
            string ip = serverAddress.Split(':')[0];
            string port = serverAddress.Split(':')[1];

            // Act
            string status = client.Start(ip, port);

            // Assert
            Assert.Equal("success", status);
        }

        /// <summary>
        /// Checks that the client can send a message to the server 
        /// </summary>
        [Fact]
        public void ClientSendMessageTest()
        {
            // Arrange
            string moduleName = "test-module";
            string serverAddress = server.Start();
            string ip = serverAddress.Split(':')[0];
            string port = serverAddress.Split(':')[1];
            client.Start(ip, port);

            string message = "Hello, Server!";

            // Act
            var exception = Record.Exception(() => client.Send(message, moduleName, null));

            // Assert
            Assert.Null(exception); 
        }

        /// <summary>
        /// Handles incoming data received by the server.
        /// </summary>
        public void OnDataReceived(string serializedData)
        {
        }

        /// <summary>
        /// Handles a new client connection to the server.
        /// </summary>
        public void OnClientJoined(TcpClient client, string ip, string port)
        {
        }
    }
}
