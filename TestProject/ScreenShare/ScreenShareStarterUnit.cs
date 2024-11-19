using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using Networking.Communication;
using System.Collections.Generic;
using System.Text.Json;
using System.Timers;
using Screenshare;
using Screenshare.ScreenShareClient;
using TimersTimer = System.Timers.Timer;
using System.Reflection;


namespace ScreenShare.Client.Tests
{
    [TestClass]
    public class ScreenshareClientTests
    {
        private Mock<ICommunicator> _mockCommunicator;
        private Mock<ScreenCapturer> _mockCapturer;
        private Mock<ScreenProcessor> _mockProcessor;
        private Mock<ScreenshareClientViewModel> _mockViewModel;
        private ScreenshareClient _client;

        [TestInitialize]
        public void Setup()
        {
            _mockCommunicator = new Mock<ICommunicator>();
            _mockCapturer = new Mock<ScreenCapturer>();
            _mockProcessor = new Mock<ScreenProcessor>(_mockCapturer.Object);
            _mockViewModel = new Mock<ScreenshareClientViewModel>();
            _client = ScreenshareClient.GetInstance(_mockViewModel.Object, isDebugging: true);
        }

        [TestMethod]
        public void FindIp_ReturnsValidIpAddress()
        {
            // Act
            string ip = _client.findIp();

            // Assert
            Assert.IsNotNull(ip);
            Assert.IsTrue(ip.Split('.').Length == 4); // Simple check for IPv4 format
        }

        [TestMethod]
        public void OnTimeout_StopsScreenSharingAndUpdatesViewModel()
        {
            // Arrange
            // Arrange
            bool sharingScreen = true; // Default value
            _mockViewModel.SetupProperty(vm => vm.SharingScreen, sharingScreen);

            // Act
            _client.OnTimeOut();

            // Assert
            _mockViewModel.VerifySet(vm => vm.SharingScreen = false, Times.Once);

        }

        [TestMethod]
        public void GetInstance_ReturnsSingletonInstance()
        {
            // Act
            var client1 = ScreenshareClient.GetInstance(_mockViewModel.Object, isDebugging: true);
            var client2 = ScreenshareClient.GetInstance();

            // Assert
            Assert.AreEqual(client1, client2);
        }

        [TestMethod]
        public void SetUserDetails_SetsNameAndId()
        {
            // Act
            _client.SetUserDetails("TestUser", "192.168.1.1");

            // Assert
            Assert.AreEqual("TestUser", _client.GetType().GetField("_name", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(_client));
            Assert.AreEqual("192.168.1.1", _client.GetType().GetField("_id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(_client));
        }



        [TestMethod]
        public void UpdateTimer_ResetsTimer()
        {
            // Arrange
            var timerField = _client.GetType().GetField("_timer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var timer = (System.Timers.Timer)timerField.GetValue(_client);
            double initialInterval = timer.Interval;

            // Act
            _client.UpdateTimer();

            // Assert
            Assert.AreEqual(ScreenshareClient.Timeout, timer.Interval);
        }



    }
}
