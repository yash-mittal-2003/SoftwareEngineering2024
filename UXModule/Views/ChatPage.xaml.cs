using Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ViewModel.ChatViewModel;

namespace UXModule.Views
{
    /// <summary>
    /// Interaction logic for ChatPage.xaml
    /// </summary>
    public partial class ChatPage : Page
    {
        private ChatClient _client;
        private ViewModel.ChatViewModel.MainViewModel _viewModel;

        public ChatPage()
        {
            InitializeComponent();

            // Initialize ViewModel
            _viewModel = MainViewModel.GetInstance;
            DataContext = _viewModel;

            // Bind Messages to ListView
            MessagesListView.ItemsSource = _viewModel.Messages;

            // Subscribe to ViewModel events
            _viewModel.MessageAdded += OnMessageAdded;
        }

        // Update the client list in dropdown
        private void UpdateClientList(object sender, List<string> clientList)
        {
            Dispatcher.Invoke(() =>
            {
                ClientDropdown.ItemsSource = clientList;
                ClientDropdown.SelectedIndex = -1; // Ensure no default selection
            });
        }

        // Scroll to the latest message when added
        private void OnMessageAdded(ChatMessage message)
        {
            Dispatcher.Invoke(() =>
            {
                if (_viewModel.Messages.Any())
                {
                    MessagesListView.ScrollIntoView(_viewModel.Messages.Last());
                }
            });
        }

        // Handle focus for MessageTextBox
        private void MessageTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (MessageTextBox.Text == "  Type something...")
            {
                MessageTextBox.Text = string.Empty;
                MessageTextBox.FontStyle = FontStyles.Normal;
            }
        }

        // Handle focus for SearchTextBox
        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox.Text == " ")
            {
                SearchTextBox.Text = string.Empty;
                SearchTextBox.FontStyle = FontStyles.Normal;
                SearchTextBox.Background = Brushes.Teal;
            }
        }

        // Handle ESC key in SearchTextBox
        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                ClearSearch();
            }
        }

        // Trigger search animations and panel transitions
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            PerformSearchTransition();
        }

        // Reverse the search panel animation
        private void ReverseSearchTransition()
        {
            var fadeOutStoryboard = new Storyboard();
            var fadeOutAnimation = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromSeconds(0.5)
            };

            fadeOutAnimation.Completed += (s, args) =>
            {
                SearchPanel.Visibility = Visibility.Collapsed;
                TopDockPanel.Visibility = Visibility.Visible;
                TopDockPanel.Opacity = 0;
            };

            Storyboard.SetTarget(fadeOutAnimation, SearchPanel);
            Storyboard.SetTargetProperty(fadeOutAnimation, new PropertyPath("Opacity"));
            fadeOutStoryboard.Children.Add(fadeOutAnimation);

            var fadeInAnimation = new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromSeconds(0.5)
            };

            Storyboard.SetTarget(fadeInAnimation, TopDockPanel);
            Storyboard.SetTargetProperty(fadeInAnimation, new PropertyPath("Opacity"));
            fadeOutStoryboard.Children.Add(fadeInAnimation);

            fadeOutStoryboard.Begin();
        }

        // Clear search results
        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            ClearSearch();
        }

        // Clear search logic
        private void ClearSearch()
        {
            SearchTextBox.Clear();
            if (_viewModel.SearchResults != null)
            {
                _viewModel.SearchResults.Clear();
            }
            _viewModel.BackToOriginalMessages();
            MessagesListView.ItemsSource = _viewModel.Messages;
            MessagesListView.ScrollIntoView(_viewModel.Messages.Last());
        }

        // Return to the main screen from search
        private void BackFromSearchButton_Click(object sender, RoutedEventArgs e)
        {
            ClearSearch();
            ReverseSearchTransition();
        }

        // Handle dynamic search
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            PerformDynamicSearch();
        }

        // Perform dynamic search
        private void PerformDynamicSearch()
        {
            string query = SearchTextBox.Text;

            if (!string.IsNullOrWhiteSpace(query))
            {
                _viewModel.SearchMessages(query);
                MessagesListView.ItemsSource = _viewModel.SearchResults;
            }
            else
            {
                _viewModel.BackToOriginalMessages();
                MessagesListView.ItemsSource = _viewModel.Messages;
            }
        }

        // Handle Emoji Popup
        private void EmojiPopupButton_Click(object sender, RoutedEventArgs e)
        {
            EmojiPopup.IsOpen = !EmojiPopup.IsOpen;
        }

        // Add emoji to the message
        private void Emoji_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button emojiButton)
            {
                string emoji = emojiButton.Content.ToString();

                if (MessageTextBox.Text == "  Type something...")
                {
                    MessageTextBox.Text = string.Empty;
                    MessageTextBox.FontStyle = FontStyles.Normal;
                }

                MessageTextBox.Text += emoji;
                MessageTextBox.Focus();
                MessageTextBox.CaretIndex = MessageTextBox.Text.Length;
            }
        }

        // Handle options button click
        private void OptionsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.DataContext = button.DataContext;
                button.ContextMenu.IsOpen = true;
            }
        }

        // Handle delete message
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu)
            {
                var placementTarget = contextMenu.PlacementTarget as FrameworkElement;
                if (placementTarget?.DataContext is ChatMessage message)
                {
                    _viewModel.DeleteMessage(message);
                }
            }
        }

        // Perform search animation
        private void PerformSearchTransition()
        {
            SearchPanel.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#854F6C"));
            var fadeOutStoryboard = new Storyboard();
            var fadeOutAnimation = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromSeconds(0.5)
            };

            fadeOutAnimation.Completed += (s, args) =>
            {
                TopDockPanel.Visibility = Visibility.Collapsed;
                SearchPanel.Visibility = Visibility.Visible;
                SearchPanel.Opacity = 0;
                SearchTextBox.Focus();
            };

            Storyboard.SetTarget(fadeOutAnimation, TopDockPanel);
            Storyboard.SetTargetProperty(fadeOutAnimation, new PropertyPath("Opacity"));
            fadeOutStoryboard.Children.Add(fadeOutAnimation);

            var fadeInAnimation = new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromSeconds(0.5)
            };

            Storyboard.SetTarget(fadeInAnimation, SearchPanel);
            Storyboard.SetTargetProperty(fadeInAnimation, new PropertyPath("Opacity"));
            fadeOutStoryboard.Children.Add(fadeInAnimation);

            fadeOutStoryboard.Begin();
        }
    }
}
