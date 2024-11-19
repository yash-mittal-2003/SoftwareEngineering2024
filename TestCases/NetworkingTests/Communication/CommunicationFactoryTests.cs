using Networking;
using Networking.Communication;
using Xunit;

namespace NetworkingTests.Communication;
    public class CommunicationFactoryTests
    {
        [Fact]
        public void GetCommunicatorServerAndClientTest()
        {
            // get the server and client communicartor from the factory
            ICommunicator communicatorServer =
                CommunicationFactory.GetCommunicator(false);
            ICommunicator[] communicatorClient = {
                CommunicationFactory.GetCommunicator(true) };

            // start and stop the server and client communicartor to
            // test that we have received the correct communcators
            // from the factory
            NetworkTestGlobals.StartServerAndClients(
                communicatorServer, communicatorClient, "Client Id");
            NetworkTestGlobals.StopServerAndClients(
                communicatorServer, communicatorClient);
        }
    }
