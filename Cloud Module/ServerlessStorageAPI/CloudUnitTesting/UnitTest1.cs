/******************************************************************************
* Filename    = UnitTest.cs
*
* Author      = Pranav Guruprasad Rao
*
* Product     = Cloud
* 
* Project     = Unnamed Software Project
*
* Description = Unit Tests for Cloud
*****************************************************************************/

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using NUnit.Framework;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using System.Net.Http;
using System.Reflection.Metadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using SECloud.Services;
using SECloud.Models;
using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CloudUnitTesting;

/// <summary>
/// Unit Tests for Cloud Module
/// </summary>
public class Tests
{
    public required HttpClient _httpClient;
    public required ILogger<CloudService> _logger;
    public string _team = "testblobcontainer";
    public string _sasToken = "";
    private const string BaseUrl = "https://secloudapp-2024.azurewebsites.net/api/";

    [SetUp]
    /// <summary>
    /// Setup of the HTTP Client to the Azure Function
    /// </summary>
    public void Setup()
    {
        ServiceProvider serviceProvider = new ServiceCollection()
            .AddLogging(builder => {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            })
            .AddHttpClient()
            .BuildServiceProvider();
        _logger = serviceProvider.GetRequiredService<ILogger<CloudService>>();
        _httpClient = new HttpClient();
    }

    [TearDown]
    /// <summary>
    /// 
    /// </summary>
    public void Teardown()
    {
        _httpClient?.Dispose();
    }

    [Test]
    /// <summary>
    /// To test if we are uploading a Null Content File
    /// </summary>
    public async Task UploadAsyncNullContentReturnsFailure()
    {
        // Arrange
        var cloudService = new CloudService(BaseUrl, _team, _sasToken, _httpClient, _logger);
        string blobName = "test.txt";
        string contentType = "text/plain";

        // Act
        ServiceResponse<string> response = await cloudService.UploadAsync(blobName, Stream.Null, contentType);

        Assert.Multiple(() => {
            // Assert
            Assert.That(response.Success, Is.False);
            Assert.That(response.Message, Is.EqualTo("The content stream is empty."));
        });
    }

    [Test]
    public async Task ExceptionNullBaseUrl()
    {
        // Arrange
        Assert.Throws<ArgumentNullException>(() => new CloudService(null, _team, _sasToken, _httpClient, _logger));
    }

    [Test]
    public async Task ExceptionEmptyContainerName()
    {
        // Arrange
        Assert.Throws<ArgumentNullException>(() => new CloudService(BaseUrl, " ", _sasToken, _httpClient, _logger));
    }

    [Test]
    public async Task ExceptionNullContainerName()
    {
        // Arrange
        Assert.Throws<ArgumentNullException>(() => new CloudService(BaseUrl, null, _sasToken, _httpClient, _logger));
    }

    [Test]
    public async Task ExceptionEmptyBaseUrl()
    {
        // Arrange
        Assert.Throws<ArgumentNullException>(() => new CloudService(" ", _team, _sasToken, _httpClient, _logger));
    }

    [Test]
    public async Task ExceptionNullSasToken()
    {
        // Arrange
        Assert.Throws<ArgumentException>(() => new CloudService(BaseUrl, _team, null, _httpClient, _logger));
    }

    [Test]
    public async Task ExceptionNullHttpClient()
    {
        // Arrange
        Assert.Throws<ArgumentNullException>(() => new CloudService(BaseUrl, _team, _sasToken, null, _logger));
    }

    [Test]
    public async Task ExceptionNullLogger()
    {
        // Arrange
        Assert.Throws<ArgumentNullException>(() => new CloudService(BaseUrl, _team, _sasToken, _httpClient, null));
    }

    [Test]
    /// <summary>
    /// To test if we are uploading content with zero content length
    /// </summary>
    public async Task UploadAsyncZeroContentLengthReturnsFailure()
    {
        // Arrange
        var cloudService = new CloudService(BaseUrl, _team, _sasToken, _httpClient, _logger);
        string blobName = "test.txt";
        string contentType = "text/plain";
        using var emptyStream = new MemoryStream();

        // Act
        ServiceResponse<string> response = await cloudService.UploadAsync(blobName, emptyStream, contentType);

        Assert.Multiple(() => {
            // Assert
            Assert.That(response.Success, Is.False);
            Assert.That(response.Message, Is.EqualTo("The content stream is empty."));
        });
    }

    [Test]
    /// <summary>
    /// To test if we are uploading file with no extension
    /// </summary>
    public async Task UploadAsyncBlobNameWithoutExtensionReturnsFailure()
    {
        // Arrange
        var cloudService = new CloudService(BaseUrl, _team, _sasToken, _httpClient, _logger);
        string blobName = "testfile"; // No extension
        string contentType = "text/plain";
        using var contentStream = new MemoryStream(Encoding.UTF8.GetBytes("Sample content"));

        // Act
        ServiceResponse<string> response = await cloudService.UploadAsync(blobName, contentStream, contentType);

        // Assert
        Assert.That(response.Success, Is.False);
    }

    [Test]
    /// <summary>
    /// To test if we are uploading file with incorrect extension
    /// </summary>
    public async Task UploadAsyncBlobNameWithUpperCaseExtensionReturnsFailure()
    {
        // Arrange
        var cloudService = new CloudService(BaseUrl, _team, _sasToken, _httpClient, _logger);
        string blobName = "test.TXT";
        string contentType = "text/plain";
        using var contentStream = new MemoryStream(Encoding.UTF8.GetBytes("Sample content"));

        // Act
        ServiceResponse<string> response = await cloudService.UploadAsync(blobName, contentStream, contentType);

        // Assert
        Assert.That(response.Success, Is.False);
    }

    [Test]
    /// <summary>
    /// To test if we are uploading the content with correct extension
    /// </summary>
    public async Task UploadAsyncBlobNameWithValidExtensionReturnsSuccess()
    {
        // Arrange
        var cloudService = new CloudService(BaseUrl, _team, _sasToken, _httpClient, _logger);
        string blobName = "test.txt";
        string contentType = "text/plain";
        using var contentStream = new MemoryStream(Encoding.UTF8.GetBytes("Sample content"));

        // Act
        ServiceResponse<string> response = await cloudService.UploadAsync(blobName, contentStream, contentType);

        // Assert
        Assert.That(response.Success, Is.True); // Ensure additional logic is set up for upload testing
    }

    [Test]
    /// <summary>
    /// To test if we are uploading file with unsupported content type
    /// </summary>
    public async Task UploadFailsForUnsupportedContentType()
    {
        _logger.LogInformation("Testing upload with unsupported content type.");
        var cloudService = new CloudService(
            BaseUrl,
            _team,
            _sasToken,
            _httpClient,
            _logger
        );

        string blobName = "unsupported-file.xyz";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Test content"));

        ServiceResponse<string> response = await cloudService.UploadAsync(blobName, stream, "application/unknown");

        Assert.Multiple(() => {
            Assert.That(response.Success, Is.False);
            Assert.That(response.Message, Is.EqualTo("Unsupported content type: application/unknown"));
        });
    }

    [Test]
    /// <summary>
    /// To test if we are uploading file with incorrect content type
    /// </summary>
    public async Task InvalidContentTypeFileUploadTest()
    {
        _logger.LogInformation("Uploading File with Invalid Content Type.");
        var cloudService = new CloudService(
            BaseUrl,
            _team,
            _sasToken,
            _httpClient,
            _logger
        );

        string content = "This is a test file content";
        string fileName = $"test-{Guid.NewGuid()}.txt";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        ServiceResponse<string> response = await cloudService.UploadAsync(fileName, stream, "image/jpeg");
        Assert.That(response.Success, Is.EqualTo(false));
    }

    [Test]
    /// <summary>
    /// To test if we are uploading file with invalid file section
    /// </summary>
    public async Task InvalidFileSectionFileUploadTest()
    {
        _logger.LogInformation("Uploading File with Invalid File Section.");

        // Define the incorrect content type
        var content = new StringContent("This is test content", Encoding.UTF8, "application/json");

        // Send the request using HttpClient.PostAsync()
        HttpResponseMessage response = await _httpClient.PostAsync($"{BaseUrl}/upload/{_team}", content);

        // Assert: Ensure the upload was unsuccessful with BadRequest
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.BadRequest));
        string responseContent = await response.Content.ReadAsStringAsync();
        Assert.That(responseContent, Is.EqualTo("Invalid content type. Expecting multipart/form-data."));
    }

    [Test]
    /// <summary>
    /// To test for upload failure
    /// </summary>
    public async Task UploadFailsWithErrorMessage()
    {
        _logger.LogInformation("Testing upload failure with error message.");

        // Mocking HttpClient to simulate a failed HTTP response
        var httpClientMock = new HttpClient(new HttpMessageHandlerMock((request, cancellationToken) => {
            var responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest) {
                ReasonPhrase = "Bad Request"
            };
            return Task.FromResult(responseMessage);
        }));

        var cloudService = new CloudService(
            BaseUrl,
            _team,
            _sasToken,
            httpClientMock,
            _logger
        );

        string blobName = "test-failure.txt";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Test content"));

        ServiceResponse<string> response = await cloudService.UploadAsync(blobName, stream, "text/plain");
        _logger.LogInformation("Success is {Success} and message is {Message}", response.Success, response.Message);

        Assert.That(response.Success, Is.False);
    }

    [Test]
    /// <summary>
    /// To test if we are uploading file while handling exception gracefully
    /// </summary>
    public async Task UploadHandlesExceptionGracefully()
    {
        _logger.LogInformation("Testing upload when an exception is thrown.");

        // Mocking HttpClient to throw an exception
        var httpClientMock = new HttpClient(new HttpMessageHandlerMock((request, cancellationToken) => {
            throw new HttpRequestException("Mocked exception");
        }));

        var cloudService = new CloudService(
            BaseUrl,
            _team,
            _sasToken,
            httpClientMock,
            _logger
        );

        string blobName = "test-exception.txt";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Test content"));

        ServiceResponse<string> response = await cloudService.UploadAsync(blobName, stream, "text/plain");

        Assert.Multiple(() => {
            Assert.That(response.Success, Is.False);
            Assert.That(response.Message, Does.Contain("An error occurred: Mocked exception"));
        });
    }

    [Test]
    /// <summary>
    /// To test if we are uploading large file
    /// </summary>
    public async Task LargeFileUploadTest()
    {
        _logger.LogInformation("Uploading Large File.");
        var cloudService = new CloudService(
            BaseUrl,
            _team,
            _sasToken,
            _httpClient,
            _logger
        );

        int size = 5 * 1024 * 1024; // 5MB
        byte[] buffer = new byte[size];
        new Random().NextBytes(buffer);

        string fileName = $"test-{Guid.NewGuid()}.bin";
        using var stream = new MemoryStream(buffer);
        ServiceResponse<string> response = await cloudService.UploadAsync(fileName, stream, "application/octet-stream");

        Assert.That(response.Success, Is.EqualTo(true));
    }

    [Test]
    /// <summary>
    /// To test if we are uploading image file
    /// </summary>
    public async Task ImageFileUploadTest()
    {
        _logger.LogInformation("Uploading Image File.");
        var cloudService = new CloudService(
            BaseUrl,
            _team,
            _sasToken,
            _httpClient,
            _logger
        );

        // Download the image from the provided URL
        string imageUrl = "https://picsum.photos/id/1/200/300";
        using var httpClient = new HttpClient();
        byte[] imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
        using var stream = new MemoryStream(imageBytes);

        // Upload the downloaded image
        ServiceResponse<string> response = await cloudService.UploadAsync("image.jpg", stream, "image/jpg");
        Assert.That(response.Success, Is.EqualTo(true));
    }

    [Test]
    /// <summary>
    /// To test if we are uploading empty file
    /// </summary>
    public async Task EmptyFileUploadTest()
    {
        _logger.LogInformation("Uploading Empty File.");
        var cloudService = new CloudService(
            BaseUrl,
            _team,
            _sasToken,
            _httpClient,
            _logger
        );

        string fileName = $"test-{Guid.NewGuid()}.txt";
        File.CreateText(fileName).Close();
        using var stream = new MemoryStream();
        ServiceResponse<string> response = await cloudService.UploadAsync(fileName, stream, "text/plain");

        Assert.That(response.Success, Is.EqualTo(false));
    }

    [Test]
    /// <summary>
    /// To test if we are uploading file with empty name
    /// </summary>
    public async Task EmptyFileNameUploadTest()
    {
        _logger.LogInformation("Uploading File with no file name.");
        var cloudService = new CloudService(
            BaseUrl,
            _team,
            _sasToken,
            _httpClient,
            _logger
        );

        string content = "This is a test file content";
        string fileName = "";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        ServiceResponse<string> response = await cloudService.UploadAsync(fileName, stream, "text/plain");

        Assert.That(response.Success, Is.EqualTo(false));
    }

    [Test]
    /// <summary>
    /// To test for proper file
    /// </summary>
    public async Task SuccessfulFileUploadTest()
    {
        _logger.LogInformation("Uploading Regular File.");
        var cloudService = new CloudService(
            BaseUrl,
            _team,
            _sasToken,
            _httpClient,
            _logger
        );

        string content = "This is a test file content";
        string fileName = $"test-1.txt";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        ServiceResponse<string> response = await cloudService.UploadAsync(fileName, stream, "text/plain");

        Assert.That(response.Success, Is.EqualTo(true));
    }

    [Test]
    /// <summary>
    /// To test if we can upload files concurrently
    /// </summary>
    public async Task ConcurrentFileUploadTest()
    {
        _logger.LogInformation("Uploading files concurrently.");
        _logger.LogInformation("Uploading Regular File.");
        var cloudService = new CloudService(
            BaseUrl,
            _team,
            _sasToken,
            _httpClient,
            _logger
        );

        var tasks = new List<Task<ServiceResponse<string>>>();
        for (int i = 0; i < 5; i++)
        {
            string content = $"Concurrent test file {i}";
            string fileName = $"concurrent-test-{i}-{Guid.NewGuid()}.txt";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

            tasks.Add(cloudService.UploadAsync(fileName, stream, "text/plain"));
        }
        ServiceResponse<string>[] results = await Task.WhenAll(tasks);

        foreach (ServiceResponse<string> result in results)
        {
            _logger.LogInformation("test result: {Message}", result.Message);
            Assert.That(result.Success, Is.EqualTo(true));
        }

    }

    [Test]
    /// <summary>
    /// To test if we are downloading file with invalid name
    /// </summary>
    public async Task InvalidFileNameDownloadTest()
    {
        _logger.LogInformation("Downloading File with invalid file name.");
        var cloudService = new CloudService(
            BaseUrl,
            _team,
            _sasToken,
            _httpClient,
            _logger
        );

        string blobName = $"{Guid.NewGuid()}";
        ServiceResponse<System.IO.Stream> downloadResponse = await cloudService.DownloadAsync(blobName);

        Assert.That(downloadResponse.Success, Is.EqualTo(false));
    }

    [Test]
    /// <summary>
    /// To test if we are downloading non existent file
    /// </summary>
    public async Task NonExistentFileDownloadTest()
    {
        _logger.LogInformation("Downloading Non-existent File.");
        var cloudService = new CloudService(
            BaseUrl,
            _team,
            _sasToken,
            _httpClient,
            _logger
        );

        string blobName = $"{Guid.NewGuid()}.txt";
        ServiceResponse<System.IO.Stream> downloadResponse = await cloudService.DownloadAsync(blobName);
        Assert.That(downloadResponse.Success, Is.EqualTo(false));
    }

    [Test]
    /// <summary>
    /// To test if we can download proper file
    /// </summary>
    public async Task SuccessfulFileDownloadTest()
    {
        _logger.LogInformation("Downloading Regular File.");
        var cloudService = new CloudService(
            BaseUrl,
            _team,
            _sasToken,
            _httpClient,
            _logger
        );

        string content = "This is a test file content";
        string blobName = $"test-1.txt";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        ServiceResponse<string> response = await cloudService.UploadAsync(blobName, stream, "text/plain");

        ServiceResponse<System.IO.Stream> downloadResponse = await cloudService.DownloadAsync(blobName);
        Assert.That(downloadResponse.Success, Is.EqualTo(true));
    }

    [Test]
    /// <summary>
    /// To test if we can successfully retrieve configuration setting
    /// </summary>
    public async Task SuccessfulConfigFileRetrievalTest()
    {
        _logger.LogInformation("Getting Correct Config Setting.");
        var cloudService = new CloudService(
            BaseUrl,
            _team,
            _sasToken,
            _httpClient,
            _logger
        );

        string setting = "Theme";
        ServiceResponse<string> response = await cloudService.RetrieveConfigAsync(setting);
        Assert.That(response.Success, Is.EqualTo(true));
    }

    [Test]
    /// <summary>
    /// To test for incorrect setting
    /// </summary>
    public async Task NonExistentConfigFileRetrievalTest()
    {
        _logger.LogInformation("Testing Config Retrieval with non existent config file.");
        var cloudService = new CloudService(
            BaseUrl,
            _team,
            _sasToken,
            _httpClient,
            _logger
        );

        string setting = "Value";
        ServiceResponse<string> response = await cloudService.RetrieveConfigAsync(setting);
        Assert.That(response.Success, Is.EqualTo(false));
    }

    [Test]
    /// <summary>
    /// To test for incorrect setting in an invalid file
    /// </summary>
    public async Task NonExistentSettingConfigFileRetrievalTest()
    {
        _logger.LogInformation("Testing Config Retrieval with non existent config setting.");
        var cloudService = new CloudService(
            BaseUrl,
            _team,
            _sasToken,
            _httpClient,
            _logger
        );

        string setting = $"{Guid.NewGuid()}";
        ServiceResponse<string> response = await cloudService.RetrieveConfigAsync(setting);
        Assert.That(response.Success, Is.EqualTo(false));
    }

    [Test]
    /// <summary>
    /// To test for successful file update
    /// </summary>
    public async Task SuccessfulFileUpdateTest()
    {
        _logger.LogInformation("Updating Regular File.");
        var cloudService = new CloudService(
            BaseUrl,
            _team,
            _sasToken,
            _httpClient,
            _logger
        );

        string contentType = "text/plain";

        string content = "This is the updated version of the test file content";
        string fileName = $"test-1.txt";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        ServiceResponse<string> response = await cloudService.UploadAsync(fileName, stream, "text/plain");

        content = "This is the updated version of the test file content";
        var updatedStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        response = await cloudService.UpdateAsync(fileName, updatedStream, contentType);
        Assert.That(response.Success, Is.EqualTo(true));
    }

    [Test]
    /// <summary>
    /// To test for file update in an invalid container
    /// </summary>
    public async Task InvalidContainerNameUpdateTest()
    {
        _logger.LogInformation("Updating Regular File.");
        var cloudService = new CloudService(
            BaseUrl,
            "wrong_team",
            _sasToken,
            _httpClient,
            _logger
        );

        string contentType = "text/plain";

        string content = "This is the updated version of the test file content";
        string fileName = $"{Guid.NewGuid()}.txt";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        ServiceResponse<string> response = await cloudService.UpdateAsync(fileName, stream, contentType);
        Assert.That(response.Success, Is.EqualTo(false));
    }

    [Test]
    /// <summary>
    /// To test for successful file delete
    /// </summary>
    public async Task SuccessfulFileDeleteTest()
    {
        _logger.LogInformation("Deleting Regular File.");
        var cloudService = new CloudService(
            BaseUrl,
            _team,
            _sasToken,
            _httpClient,
            _logger
        );

        string content = "TO BE DELETED";
        string fileName = $"test-2.txt";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        ServiceResponse<string> response = await cloudService.UploadAsync(fileName, stream, "text/plain");

        if (response.Success == true)
        {
            ServiceResponse<bool> deleteResponse = await cloudService.DeleteAsync(fileName);
            Assert.That(deleteResponse.Success, Is.EqualTo(true));
        }
    }

    [Test]
    /// <summary>
    /// To test for file delete with invalid name
    /// </summary>
    public async Task InvalidFileNameDeleteTest()
    {
        _logger.LogInformation("Deleting Regular File.");
        var cloudService = new CloudService(
            BaseUrl,
            _team,
            _sasToken,
            _httpClient,
            _logger
        );

        string blobName = $"{Guid.NewGuid()}";
        ServiceResponse<bool> response = await cloudService.DeleteAsync(blobName);
        Assert.That(response.Success, Is.EqualTo(false));
    }

    [Test]
    /// <summary>
    /// To test for successful listing blobs in a container
    /// </summary>
    public async Task ListBlobsTest()
    {
        _logger.LogInformation("Listing Blobs in the container.");
        var cloudService = new CloudService(
            BaseUrl,
            _team,
            _sasToken,
            _httpClient,
            _logger
        );

        ServiceResponse<System.Collections.Generic.List<string>> response = await cloudService.ListBlobsAsync();
        Assert.That(response.Success, Is.EqualTo(true));
    }

    [Test]
    /// <summary>
    /// To test for listing blobs in an invalid container
    /// </summary>
    public async Task InvalidContainerListBlobsTest()
    {
        _logger.LogInformation("Listing Blobs in the container.");
        var cloudService = new CloudService(
            BaseUrl,
            "loplo",
            _sasToken,
            _httpClient,
            _logger
        );

        ServiceResponse<System.Collections.Generic.List<string>> response = await cloudService.ListBlobsAsync();
        Assert.That(response.Success, Is.EqualTo(false));
    }

    [Test]
    /// <summary>
    /// To test for null response in blob listing
    /// </summary>
    public async Task ListBlobsAsyncBlobListResponseIsNullReturnsEmptyList()
    {
        // Arrange: Mock HttpClient to return a successful response with null content
        var httpClientMock = new HttpClient(new HttpMessageHandlerMock((request, cancellationToken) => {
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent("null", Encoding.UTF8, "application/json")
            };
            return Task.FromResult(responseMessage);
        }));

        var cloudService = new CloudService(BaseUrl, _team, _sasToken, httpClientMock, _logger);

        // Act: Call the method
        ServiceResponse<List<string>> response = await cloudService.ListBlobsAsync();

        Assert.Multiple(() => {
            // Assert
            Assert.That(response.Success, Is.True);
            Assert.That(response.Data, Is.Null); // Null because blobListResponse.Blobs was null
        });
    }

    [Test]
    /// <summary>
    /// To test for listing blobs in an empty container
    /// </summary>
    public async Task EmptyContainerListBlobsTest()
    {
        _logger.LogInformation("Listing Blobs in the container.");
        var cloudService = new CloudService(
            BaseUrl,
            "wrong-container",
            _sasToken,
            _httpClient,
            _logger
        );

        ServiceResponse<System.Collections.Generic.List<string>> response = await cloudService.ListBlobsAsync();
        Assert.That(response.Data, Is.Empty);
    }

    [Test]
    /// <summary>
    /// To test for successful JSON search
    /// </summary>
    public async Task SuccessfulJsonSearchTest()
    {
        _logger.LogInformation("Search for JSON files in the container");
        var cloudService = new CloudService(
            BaseUrl,
            _team,
            _sasToken,
            _httpClient,
            _logger
        );

        WeatherForecast weather = new WeatherForecast {
            Date = DateTime.Now,
            TemperatureCelsius = 32,
            Summary = "Sunny"
        };
        string fileName = "WeatherForecast.json";
        await using (FileStream createStream = File.Create(fileName))
        {
            await JsonSerializer.SerializeAsync(createStream, weather);
        }

        // Open the file for reading to prepare for upload
        await using FileStream uploadStream = File.OpenRead(fileName);

        // Upload the file to the cloud with "application/json" as the content type
        ServiceResponse<string> upload_response = await cloudService.UploadAsync("weather.json", uploadStream, "application/json");

        // Now, search the uploaded JSON file in the cloud
        string key = "TemperatureCelsius";
        string value = "32";
        ServiceResponse<SECloud.Models.JsonSearchResponse> response = await cloudService.SearchJsonFilesAsync(key, value);

        // Assert: Check that the search was successful
        Assert.That(response.Success, Is.EqualTo(true));
    }

    [Test]
    /// <summary>
    /// To test for JSON search with empty search key
    /// </summary>
    public async Task EmptySearchKeyJsonSearchTest()
    {
        _logger.LogInformation("Search for JSON files in the container");
        var cloudService = new CloudService(
            BaseUrl,
            _team,
            _sasToken,
            _httpClient,
            _logger
        );

        // Now, search the uploaded JSON file in the cloud
        string key = "";
        string value = "32";
        ServiceResponse<SECloud.Models.JsonSearchResponse> response = await cloudService.SearchJsonFilesAsync(key, value);

        // Assert: Check that the search was successful
        Assert.That(response.Success, Is.EqualTo(false));
    }

    [Test]
    /// <summary>
    /// To test for JSON search in an invalid container name
    /// </summary>
    public async Task InvalidContainerNameJsonSearchTest()
    {
        _logger.LogInformation("Search for JSON files in the container");
        var cloudService = new CloudService(
            BaseUrl,
            "wrong-container1",
            _sasToken,
            _httpClient,
            _logger
        );

        string content = "This is a test file content";
        string fileName = $"test-1.txt";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        ServiceResponse<string> response = await cloudService.UploadAsync(fileName, stream, "application/json");

        string content1 = "This is a test file content";
        string fileName1 = $"test-2.txt";
        using var stream1 = new MemoryStream(Encoding.UTF8.GetBytes(content1));
        ServiceResponse<string> response1 = await cloudService.UploadAsync(fileName1, stream1, "application/json");

        string content2 = "This is a test file content";
        string fileName2 = $"test-3.txt";
        using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(content2));
        ServiceResponse<string> response2 = await cloudService.UploadAsync(fileName2, stream2, "application/json");

        // Now, search the uploaded JSON file in the cloud
        string key = "";
        string value = "32";
        ServiceResponse<JsonSearchResponse> response3 = await cloudService.SearchJsonFilesAsync(key, value);
        _logger.LogInformation("{Message}", response3.Message.ToString());
        // Assert: Check that the search was successful
        Assert.That(response3.Success, Is.EqualTo(false));
    }

    [Test]
    /// <summary>
    /// To test for unsuccessful JSON search
    /// </summary>
    public async Task SearchJsonFilesAsyncFailsWithErrorMessage()
    {
        _logger.LogInformation("Testing JSON search failure scenario.");

        // Mocking HttpClient to simulate a failed HTTP response
        var httpClientMock = new HttpClient(new HttpMessageHandlerMock((request, cancellationToken) => {
            var responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest) {
                ReasonPhrase = "Bad Request"
            };
            return Task.FromResult(responseMessage);
        }));

        var cloudService = new CloudService(
            BaseUrl,
            _team,
            _sasToken,
            httpClientMock,
            _logger
        );

        string searchKey = "testKey";
        string searchValue = "testValue";

        // Act: Call the method
        ServiceResponse<JsonSearchResponse> response = await cloudService.SearchJsonFilesAsync(searchKey, searchValue);

        Assert.Multiple(() => {
            // Assert: Verify the response
            Assert.That(response.Success, Is.False);
            Assert.That(response.Message, Does.Contain("Search failed: Bad Request"));
        });
    }

    [Test]
    /// <summary>
    /// To test for valid response with null content
    /// </summary>
    public async Task SearchJsonFilesAsyncSearchResponseIsNullReturnsSuccessWithZeroMatches()
    {
        // Arrange: Mock HttpClient to return a valid response with null content
        var httpClientMock = new HttpClient(new HttpMessageHandlerMock((request, cancellationToken) => {
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent("null", Encoding.UTF8, "application/json")
            };
            return Task.FromResult(responseMessage);
        }));

        var cloudService = new CloudService(BaseUrl, _team, _sasToken, httpClientMock, _logger);

        // Act: Call the method
        ServiceResponse<JsonSearchResponse> response = await cloudService.SearchJsonFilesAsync("key", "value");

        Assert.Multiple(() => {
            // Assert
            Assert.That(response.Success, Is.True);
            Assert.That(response.Data, Is.Null);
        });
    }

}

/// <summary>
/// Mock HTTP Message Handler
/// </summary>
public class HttpMessageHandlerMock : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

    public HttpMessageHandlerMock(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return _handler(request, cancellationToken);
    }
}

/// <summary>
/// Mock Class for JSON Search
/// </summary>
public class WeatherForecast
{
    public DateTimeOffset Date { get; set; }
    public int TemperatureCelsius { get; set; }
    public string? Summary { get; set; }
}
