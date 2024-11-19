
using Networking.Communication;
using Networking;
//using Dashboard;

using System.Collections.Generic;
using System.Text.Json;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Chat
{
    public class ChatClient : INotificationHandler
    {

        private ICommunicator _communicator = CommunicationFactory.GetCommunicator(true);

        public List<string> _clientList = new();
        public event EventHandler<string> MessageReceived;
        public event EventHandler<List<string>> ClientListUpdated;

        public Dictionary<int, string> Client_dict = new();
        public ObservableCollection<string> ClientListobs = new();
        public event PropertyChangedEventHandler PropertyChanged;
        public event Action LatestAction;
        public string clientIdCheck;

        public string Username { get; set; }
        public string clientId { get; set; }

        public string UserProfileUrl { get; set; }

        public void ChatClient_start()
        {
            _communicator.Subscribe("ChatModule", this, isHighPriority: true);
        }

        public void Start()
        {

                string messageType = "connect";
                string formattedMessage = $"{messageType}|{messageType}|{Username}|{messageType}|{messageType}";
                _communicator.Send(formattedMessage, "ChatModule", null);
        }

        public void SendMessage(string message, string recipientId = null)
        {
            string messageType = recipientId == null ? "public" : "private";
            if (recipientId != null)
            {
                int x = Int32.Parse(recipientId);
                x = x - 1;
                recipientId = x.ToString();
            }
            string formattedMessage = $"{messageType}|{message}|{Username}|{clientId}|{recipientId}";

            _communicator.Send(formattedMessage, "ChatModule", null);
            if (messageType == "private")
            {
                int x = Int32.Parse(clientIdCheck);
                x = x - 1;
                string recipee = x.ToString();

                string formattedMessage2 = $"{messageType}|{message}|{Username}|{clientId}|{recipee}";
                _communicator.Send(formattedMessage2, "ChatModule", null);
            }
        }

        public void OnDataReceived(string serializedData)
        {
            MessageReceived?.Invoke(this, serializedData);

            var dataParts = serializedData.Split('|');
            if (dataParts[0] == "clientlist")
            {
                // Update Client dictionary and ObservableCollection for the client list
                Client_dict = JsonSerializer.Deserialize<Dictionary<int, string>>(dataParts[1]);

                clientIdCheck = Client_dict.FirstOrDefault(x => x.Value == Username).Key.ToString();

                foreach (var kvp in Client_dict)
                {
                    Console.WriteLine($"Client_dict  {kvp.Value}");
                    //ClientListobs.Add();
                }

                //Dispatcher.CurrentDispatcher.Invoke(() =>
                System.Windows.Application.Current.Dispatcher.Invoke(() =>

                {
                    LatestAction?.Invoke();
                });

                // Notify any listeners that the ClientListobs has been updated
                OnPropertyChanged(nameof(ClientListobs));

            }
            else if (dataParts[1] == "private")
            {
                // Handle private message
                string messageContent = dataParts[3];
                MessageReceived?.Invoke(this, $"Private from {dataParts[2]} : {messageContent}");
            }
            else
            {
                // Handle public message
                MessageReceived?.Invoke(this, serializedData);
            }
        }
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
