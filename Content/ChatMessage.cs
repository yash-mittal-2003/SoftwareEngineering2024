using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content
{

    public class ChatMessage : INotifyPropertyChanged
    {
        private string _content;
        private bool _isDeleted;

        public string User { get; set; }
        public string Time { get; set; }
        public bool IsSentByUser { get; set; }
        public string? HighlightedText { get; set; }
        public string? HighlightedAfterText { get; set; }

        public string Content
        {
            get => _content;
            set
            {
                if (_content != value)
                {
                    _content = value;
                    OnPropertyChanged(nameof(Content));
                }
            }
        }

        public bool IsDeleted
        {
            get => _isDeleted;
            set
            {
                if (_isDeleted != value)
                {
                    _isDeleted = value;
                    OnPropertyChanged(nameof(IsDeleted));
                }
            }
        }

        public string Text { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ChatMessage(string user, string content, string time, bool isSentByUser)
        {
            User = user;
            Content = content;
            Time = time;
            IsSentByUser = isSentByUser;
            Text = content;
            IsDeleted = false;
        }
    }


}