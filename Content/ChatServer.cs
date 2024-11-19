
using Networking.Communication;
using Networking;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text.Json;

namespace Content
{
    public class ChatServer : INotificationHandler
    {
        private ICommunicator _communicator = CommunicationFactory.GetCommunicator(false);
        public readonly Dictionary<int, string> _clientUsernames = new(); // Maps clientId to username

        public event EventHandler ClientUsernamesUpdated; //new

        public string Username { get; set; }
        public string clientId { get; set; }

        public void Start()
        {
            _communicator.Subscribe("ChatModule", this, isHighPriority: true);
        }



        public void OnDataReceived(string serializedData)
        {
            var dataParts = serializedData.Split('|');
            if (dataParts.Length < 3) return;

            string messageType = dataParts[0];
            string messageContent = dataParts[1];
            string senderUsername = dataParts[2];
            string senderId = dataParts[3];
            string recipientId = dataParts.Length > 4 ? dataParts[4] : null;

            if (messageType == "connect")
            {

                int _clientId = int.Parse(clientId);
                _clientUsernames[_clientId] = senderUsername;
                string tp = "";

                tp = JsonSerializer.Serialize(_clientUsernames);

                string formattedMessage = $"clientlist|{tp}";


                // Serialise the list and send.
                _communicator.Send(formattedMessage, "ChatModule", destination: null);
            }

            if (messageType == "private")
            {
                // Send private message to specified recipient only

                messageContent = $"[PRIVATE] : {messageContent}";

                //_communicator.Send($"private|{messageContent}|{senderUsername}", "ChatModule", recipientId);
                _communicator.Send($"{senderUsername} :.: {messageContent} |private|{senderUsername}|{messageContent} ", "ChatModule", recipientId);
            }
            else
            {
                _communicator.Send($"{senderUsername} :.: {messageContent} |abc", "ChatModule", destination: null);
            }

        }

    }
}