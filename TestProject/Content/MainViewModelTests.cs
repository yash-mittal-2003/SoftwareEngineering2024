using Moq;
using Xunit;
using System.Collections.ObjectModel;
using System.Linq;
using System.ComponentModel;
using System;
using ModuleChat;

namespace ChatApplication.Tests
{
    public class MainViewModelTests
    {
        // Mocking ChatClient dependency
        private readonly Mock<ChatClient> _mockChatClient;
        private readonly MainViewModel _viewModel;

        public MainViewModelTests()
        {
            _mockChatClient = new Mock<ChatClient>();
            _viewModel = new MainViewModel();
        }

        [Fact]
        public void Constructor_InitializesCorrectly()
        {
            Xunit.Assert.NotNull(_viewModel.Messages);
            Xunit.Assert.NotNull(_viewModel.Clientte);
        }

        
        [Fact]
        public void BackToOriginalMessages_RestoresOriginalContent()
        {
            // Arrange
            var message = new ChatMessage("TestUser", "Hello World", DateTime.Now.ToString("HH:mm"), true);
            message.HighlightedText = "Hello";
            _viewModel.Messages.Add(message);

            // Act
            _viewModel.BackToOriginalMessages();

            // Xunit.Assert
            Xunit.Assert.Equal("Hello World", message.Content);
            Xunit.Assert.Empty(message.HighlightedText);
        }

        [Fact]
        public void DeleteMessage_MessageIsDeleted_UpdatesMessageContent()
        {
            // Arrange
            var message = new ChatMessage("TestUser", "Hello World", DateTime.Now.ToString("HH:mm"), true);
            _viewModel.Messages.Add(message);

            // Act
            _viewModel.DeleteMessage(message);

            // Xunit.Assert
            Xunit.Assert.Equal("[Message deleted]", message.Content);
            Xunit.Assert.True(message.IsDeleted);
        }

        [Fact]
        public void SendButton_Click_SendsMessage()
        {
            // Arrange
            _viewModel.MessageTextBox_Text = "Test message";
            _viewModel.Recipientt = "Everyone";

            // Act
            _viewModel.SendMessageCommand.Execute(null);

            // Assert
            Xunit.Assert.Equal("  Type something...", _viewModel.MessageTextBox_Text);
        }

        [Fact]
        public void SearchMessages_HandlesEmptyQuery_Gracefully()
        {
            // Arrange
            _viewModel.Messages.Add(new ChatMessage("User1", "Hello World", DateTime.Now.ToString("HH:mm"), true));

            // Act
            _viewModel.SearchMessages(string.Empty);

            // Assert
            Xunit.Assert.Empty(_viewModel.SearchResults);
        }
    }
}
