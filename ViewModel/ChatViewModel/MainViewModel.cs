// MainViewModel.cs
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.DirectoryServices;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.Pkcs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Content;
//using static System.Net.Mime.MediaTypeNames;

//using Dashboard;

namespace ViewModel.ChatViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private  ChatClient _client;
        private  ChatServer _server;
        private string _chatHistory;
        private string _message;
        private ObservableCollection<string> _clientList = new();
        
        public ObservableCollection<ChatMessage> Messages { get; set; }
        public ObservableCollection<ChatMessage> SearchResults { get; set; }
        ObservableCollection<string> ClientUsernames { get; set; }

        public ObservableCollection<string> Clientte => _client.ClientListobs;

        public string dash_ServerIp;
        public string dash_ServerPort;

        public string Username { get; set; }
        public string cliendId { get; set; }

        public string UserProfileUrl { get; set; }
        public ICommand ConnectCommand { get; }
        public ICommand SendMessageCommand { get; }
        public ICommand SearchMessagesCommmand { get; }
        public ICommand EnterKeyCommand { get; }

        public event Action<ChatMessage> MessageAdded;
     

        private MainViewModel()
        {
            _server = new ChatServer();
            _client = new ChatClient();
            _client.LatestAction += SyncClientList;
            _client.MessageReceived += OnMessageReceived;
            EnterKeyCommand = new RelayCommand(OnEnterKeyPressed);

            ClientUsernames = new ObservableCollection<string>(); //new

            _client.ClientListUpdated += (s, list) =>
            {
                Console.WriteLine("ClientListUpdated event triggered with clients: " + string.Join(", ", list)); // Debug log
                ClientList.Clear();
                foreach (var clientId in list)
                {
                    ClientList.Add(clientId);
                }
            };
            Messages = new ObservableCollection<ChatMessage> {
             new ChatMessage("John", "Hello, how are you?", DateTime.Now.ToString("HH:mm"), true),
             new ChatMessage("fgh", "Hello, how are you?", DateTime.Now.ToString("HH:mm"), false),
             new ChatMessage("John", "Hello, how are you?", DateTime.Now.ToString("HH:mm"), true),
             new ChatMessage("fgh", "Hello, how are you?", DateTime.Now.ToString("HH:mm"), false),
        };
            //ConnectCommand = new RelayCommand(ConnectButton_Click);
            SendMessageCommand = new RelayCommand(SendButton_Click);
        }

        public void setUserDetails_client(string username, string userid, string UserProfileUrl)
        {

            _client = new ChatClient();
            _client.clientId = userid;
            _client.Username = username;
            _client.UserProfileUrl = UserProfileUrl;
            //_client._clientList = userlist;

            _client.ChatClient_start();

        }
        public void setUserDetails_server(string username, string UserProfileUrl)
        {
       
            _server = new ChatServer();
            _server.Start();

            //_client = new ChatClient();
            _client.clientId = "1";

            _client.Username = username;
            _client.UserProfileUrl = UserProfileUrl;

            _client.ChatClient_start();

        }

        private static MainViewModel ContentVMInstance;
        public static MainViewModel GetInstance
        {
            get
            {
                if (ContentVMInstance == null)
                {
                    ContentVMInstance = new MainViewModel();
                }
                return ContentVMInstance;
            }
        }

        public string ChatHistory
        {
            get => _chatHistory;
            set { _chatHistory = value; OnPropertyChanged(); }
        }

        public string Message
        {
            get => _message;
            set { _message = value; OnPropertyChanged(); }
        }
        public ObservableCollection<string> ClientList
        {
            get => _clientList;
            set { _clientList = value; OnPropertyChanged(); }
        }
        private bool _isNotFoundPopupOpen = false;
        public bool IsNotFoundPopupOpen
        {
            get => _isNotFoundPopupOpen;
            set
            {
                if (_isNotFoundPopupOpen != value)
                {
                    _isNotFoundPopupOpen = value;
                    OnPropertyChanged(nameof(IsNotFoundPopupOpen));
                }
            }
        }

        //connect button command
        //public string _username_text;
        //public string Username_Text
        //{
        //    get => _username_text;
        //    set
        //    {
        //        _username_text = value; OnPropertyChanged(nameof(Username_Text));
        //    }
        //}

        //----send button command
        public string _messagetextbox_text;
        public string MessageTextBox_Text
        {
            get => _messagetextbox_text;
            set
            {
                _messagetextbox_text = value; OnPropertyChanged(nameof(MessageTextBox_Text));
            }
        }

        //selected username
        public string _selectedusername;
        public string Selectedusername
        {
            get => _selectedusername;
            set
            {
                _selectedusername = value; OnPropertyChanged(nameof(Selectedusername));
            }
        }

        public event EventHandler RequestVariable;

        private string _recipientt;
        public string Recipientt
        {
            get => _recipientt;
            set
            {
                _recipientt = value;
                OnPropertyChanged(nameof(Recipientt));
            }
        }

        //selected emoji
        public string _emoji;
        public string Emoji
        {
            get => _emoji;
            set
            {
                _emoji = value; OnPropertyChanged(nameof(Emoji));
            }
        }




        public void DeleteMessage(ChatMessage message)
        {
            if (message != null)
            {
                message.IsDeleted = true;       // Notify UI about the deleted state
                message.Content = "[Message deleted]";
                message.Text = "[Message deleted]";
            }
        }




        private void SendButton_Click()
        {
            string message = MessageTextBox_Text;
            string recipient = Recipientt;

            string recipient_id = null;

            if (recipient != null && recipient != "Everyone")
            {
                recipient_id = _client.Client_dict.FirstOrDefault(x => x.Value == recipient).Key.ToString();
            }

            if (!string.IsNullOrWhiteSpace(message) && message != "  Type something...")
            {
                _client.SendMessage(message, recipient_id); // Send private if recipient selected
                MessageTextBox_Text = "  Type something...";

            }
        }

        private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OnEnterKeyPressed();
            }
        }
        private void OnEnterKeyPressed()
        {

            if (!string.IsNullOrWhiteSpace(MessageTextBox_Text))
            {
                SendButton_Click();
                MessageTextBox_Text = string.Empty;

            }
        }

        private void OnMessageReceived(object sender, string message)
        {
            Debug.WriteLine($"Message received: {message}");
            ChatHistory += message + Environment.NewLine;
            string[] parts = message.Split(new[] { " :.: " }, StringSplitOptions.None);
            if (parts.Length == 2)
            {
                string user = parts[0].Trim();          // senderUsername
                var msg = parts[1].Split('|');
                string messageContent = msg[0].Trim();
                bool isSent = false;
                //isSent = (user == Username_Text);
                isSent = (user == Username);

                bool messageExists = Messages.Any(m => m.User == user && m.Content == messageContent);

                if (!messageExists)
                {
                    //Application.Current.Dispatcher.Invoke(() =>
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        Messages.Add(new ChatMessage
                        (
                            user,
                            messageContent,
                            DateTime.Now.ToString("HH:mm"),
                            isSent
                        ));

                    });
                }
            }
        }
        

        private void SendMessage()
        {
            _client.SendMessage(Message);
           
            Message = string.Empty;
        }



        public void SearchMessages(string query)
        {
            if (SearchResults == null)
            {
                SearchResults = new ObservableCollection<ChatMessage>();
            }
            SearchResults.Clear();
            if (string.IsNullOrWhiteSpace(query))
                return;

            var lowerCaseQuery = query.ToLower();
            var foundMessages = Messages.Where(m => m != null && (m.Content).ToLower().Contains(lowerCaseQuery)).ToList();

            foreach (var message in Messages)
            {
                message.Content = message.Text;

                if (message.Content.ToLower().Contains(lowerCaseQuery) && message.Content != "[Message deleted]")
                {
                    var startIndex = message.Content.ToLower().IndexOf(lowerCaseQuery);
                    var highlightedText = message.Content.Substring(startIndex, lowerCaseQuery.Length);
                    var beforeText = message.Content.Substring(0, startIndex);
                    var afterText = message.Content.Substring(startIndex + lowerCaseQuery.Length);

                    // Assign highlighted properties
                    message.Content = beforeText;
                    message.HighlightedText = highlightedText;
                    message.HighlightedAfterText = afterText;
                    Debug.WriteLine($"Search Query: {query}");
                    Debug.WriteLine($"Found Messages: {foundMessages.Count}");


                }
                else
                {
                    message.Content = message.Text;
                    message.HighlightedText = string.Empty;
                    message.HighlightedAfterText = string.Empty;
                }
                SearchResults.Add(message);
            }
            //     IsNotFoundPopupOpen = !foundMessages.Any();
            OnPropertyChanged(nameof(SearchResults)); // Notify the view
        }


        public void BackToOriginalMessages()
        {
            foreach (var message in Messages)
            {
                message.Content = message.Text;
                message.HighlightedText = string.Empty;
                message.HighlightedAfterText = string.Empty;
            }
            OnPropertyChanged(nameof(Messages));
        }

        private void SyncClientList()
        {
            //Dispatcher.CurrentDispatcher.Invoke(() =>
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _client.ClientListobs.Clear();
                // Clear and update the copy
                Clientte.Clear();
                Clientte.Add("Everyone");
                foreach (var kvp in _client.Client_dict)
                {
                    _client.ClientListobs.Add(kvp.Value);
                    Clientte.Add(kvp.Value);
                }

            });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
