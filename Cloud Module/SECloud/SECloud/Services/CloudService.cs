/******************************************************************************
 * Filename    = CloudService.cs
 *
 * Author      = Arnav Rajesh Kadu
 *
 * Product     = SECloud
 * 
 * Project     = Unnamed Software Project
 *
 * Description = Provides cloud storage operations implementation for Azure Blob 
 *              Storage through HTTP requests to serverless functions
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SECloud.Interfaces;
using SECloud.Models;

namespace SECloud.Services;

/// <summary>
/// Service class implementing cloud storage operations through HTTP requests.
/// Handles blob storage operations like upload, download, delete, list, and search.
/// </summary>
public class CloudService : ICloud
{
    // Private fields for HTTP client configuration and logging
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _team;
    private readonly string _sasToken;
    private readonly ILogger<CloudService> _logger;

    /// <summary>
    /// Initializes a new instance of the CloudService class.
    /// </summary>
    /// <param name="baseUrl">Base URL for the cloud storage API endpoints.</param>
    /// <param name="team">Team identifier used for container isolation.</param>
    /// <param name="sasToken">Shared Access Signature token for authentication.</param>
    /// <param name="httpClient">HTTP client for making API requests.</param>
    /// <param name="logger">Logger instance for service diagnostics.</param>
    public CloudService(
        string baseUrl,
        string team,
        string sasToken,
        HttpClient httpClient,
        ILogger<CloudService> logger)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new ArgumentNullException(nameof(baseUrl), "Base URL cannot be null or empty.");
        }

        if (string.IsNullOrWhiteSpace(team))
        {
            throw new ArgumentNullException(nameof(team), "Team (container name) cannot be null or empty");
        }

        if (sasToken == null || sasToken.StartsWith('?'))
        {
            throw new ArgumentException("SAS token is invalid. Its not required to start with '?'.",nameof(sasToken));
        }
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient), "HTTP client cannot be null.");
        _baseUrl = baseUrl;
        _team = team;
        _sasToken = sasToken;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger), "Logger cannot be null.");

        _logger.LogInformation("CloudService initialized for team: {Team}", team);
    }

    /// <summary>
    /// Constructs the complete request URL with SAS token for a given endpoint.
    /// </summary>
    /// <param name="endpoint">API endpoint to be accessed.</param>
    /// <returns>Complete URL with SAS token appended.</returns>
    private string GetRequestUrl(string endpoint)
    {
        var url = $"{_baseUrl}/{endpoint}?{_sasToken}";
        _logger.LogDebug("Generated request URL for endpoint: {Endpoint}", endpoint);
        return url;
    }

    /// <summary>
    /// Uploads a blob to cloud storage using multipart form data.
    /// </summary>
    /// <param name="blobName">Name of the blob to be uploaded.</param>
    /// <param name="content">Stream containing the blob content.</param>
    /// <param name="contentType">Content type of the blob.</param>
    /// <returns>Service response containing upload operation result.</returns>
    public async Task<ServiceResponse<string>> UploadAsync(string blobName, Stream content, string contentType)
    {
        _logger.LogDebug("Inside UploadAsync");

        // Define valid content types and their corresponding extensions
        var contentTypeDict = new Dictionary<string, string>
        {
            { "image/jpeg", "jpeg" },
            { "image/jpg", "jpg" },
            { "image/png", "png" },
            { "text/plain", "txt" },
            { "application/json", "json" },
            { "text/html", "html" },
            {"application/octet-stream", "bin" }
        };

        // Validate content type and extension
        if (!contentTypeDict.TryGetValue(contentType, out var expectedExtension))
        {
            _logger.LogError("Unsupported content type: {ContentType}", contentType);
            return new ServiceResponse<string>
            {
                Success = false,
                Message = $"Unsupported content type: {contentType}"
            };
        }

        // Ensure the content is valid
        if (content == null || content.Length == 0)
        {
            _logger.LogError("Cannot upload an empty file");
            return new ServiceResponse<string>
            {
                Success = false,
                Message = "The content stream is empty."
            };
        }

        // Extract file extension from blob name
        var actualExtension = Path.GetExtension(blobName)?.TrimStart('.');
        if (actualExtension == null || !actualExtension.Equals(actualExtension.ToLowerInvariant(), StringComparison.Ordinal))
        {
            _logger.LogError("File extensions must be in lowercase only.");
            return new ServiceResponse<string>
            {
                Success = false,
                Message = "File extension is in invalid format."
            };
        }
    
        if (actualExtension != expectedExtension)
        {
            _logger.LogError("Invalid file extension for blob: {BlobName}. Expected: {Expected}, Found: {Actual}", blobName, expectedExtension, actualExtension);
            return new ServiceResponse<string>
            {
                Success = false,
                Message = $"File extension mismatch. Expected: {expectedExtension}, Found: {actualExtension}"
            };
        }

        _logger.LogInformation("Starting upload for blob: {BlobName}, ContentType: {ContentType}", blobName, contentType);

        // Construct the upload request URL
        var requestUrl = GetRequestUrl($"upload/{_team}");

        try
        {
            // Create multipart form data content
            using var multipartContent = new MultipartFormDataContent();
            using var streamContent = new StreamContent(content);

            // Set the content type for the stream
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

            // Add the file content to the multipart form data
            multipartContent.Add(streamContent, "file", blobName);

            _logger.LogDebug("Sending upload request for blob: {BlobName}", blobName);

            // Send the upload request
            var response = await _httpClient.PostAsync(requestUrl, multipartContent);

            // Handle the response
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Successfully uploaded blob: {BlobName}", blobName);
                return new ServiceResponse<string>
                {
                    Success = true,
                    Data = responseContent,
                    Message = $"Successfully uploaded {blobName}"
                };
            }

            // Log and return an error if upload fails
            var errorMessage = $"Upload failed: {response.StatusCode} - {response.ReasonPhrase}";
            _logger.LogError("Upload failed for blob: {BlobName}. Status: {StatusCode}, Reason: {Reason}",
                blobName, response.StatusCode, response.ReasonPhrase);

            return new ServiceResponse<string>
            {
                Success = false,
                Message = errorMessage
            };
        }
        catch (Exception ex)
        {
            // Handle exceptions
            _logger.LogError(ex, "Exception occurred while uploading blob: {BlobName}", blobName);
            return new ServiceResponse<string>
            {
                Success = false,
                Message = $"An error occurred: {ex.Message}"
            };
        }
    }


    /// <summary>
    /// Downloads a blob from cloud storage.
    /// </summary>
    /// <param name="blobName">Name of the blob to download.</param>
    /// <returns>Service response containing the downloaded blob stream.</returns>
    public async Task<ServiceResponse<Stream>> DownloadAsync(string blobName)
    {
        _logger.LogInformation("Starting download for blob: {BlobName}", blobName);

        /*try
        {*/
        var requestUrl = GetRequestUrl($"download/{_team}/{blobName}");
        var response = await _httpClient.GetAsync(requestUrl);

        if (response.IsSuccessStatusCode)
        {
            var stream = await response.Content.ReadAsStreamAsync();
            _logger.LogInformation("Successfully downloaded blob: {BlobName}", blobName);
            return new ServiceResponse<Stream> { Success = true, Data = stream };
        }

        _logger.LogError("Download failed for blob: {BlobName}. Status: {StatusCode}, Reason: {Reason}",
            blobName, response.StatusCode, response.ReasonPhrase);
        return new ServiceResponse<Stream> { Success = false, Message = $"Download failed: {response.ReasonPhrase}" };
        
    }

    /// <summary>
    /// Deletes a blob from cloud storage.
    /// </summary>
    /// <param name="blobName">Name of the blob to delete.</param>
    /// <returns>Service response indicating deletion success or failure.</returns>
    public async Task<ServiceResponse<bool>> DeleteAsync(string blobName)
    {
        _logger.LogInformation("Starting delete operation for blob: {BlobName}", blobName);
        /*
        try
        {*/
        var requestUrl = GetRequestUrl($"delete/{_team}/{blobName}");
        var response = await _httpClient.DeleteAsync(requestUrl);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successfully deleted blob: {BlobName}", blobName);
        }
        else
        {
            _logger.LogError("Delete failed for blob: {BlobName}. Status: {StatusCode}, Reason: {Reason}",
                blobName, response.StatusCode, response.ReasonPhrase);
        }

        return new ServiceResponse<bool>
        {
            Success = response.IsSuccessStatusCode,
            Data = response.IsSuccessStatusCode,
            Message = response.IsSuccessStatusCode ? $"Delete succeeded: {response.ReasonPhrase}" : $"Delete failed: {response.ReasonPhrase}"
        };
        
    }

    /// <summary>
    /// Lists all blobs in the team's container.
    /// </summary>
    /// <returns>Service response containing list of blob names.</returns>
    public async Task<ServiceResponse<List<string>>> ListBlobsAsync()
    {
        _logger.LogInformation("Starting list operation for blobs");
        /*
        try
        {*/
        // Adjust request URL to call Azure Function without prefix
        var requestUrl = GetRequestUrl($"list/{_team}");
        var response = await _httpClient.GetAsync(requestUrl);

        if (response.IsSuccessStatusCode)
        {
            // Deserialize response into BlobListResponse and get the list of blob names
            var blobListResponse = await response.Content.ReadFromJsonAsync<BlobListResponse>();
            var blobs = blobListResponse?.Blobs;

            _logger.LogInformation("Successfully listed blobs. Found {Count} blobs", blobs?.Count ?? 0);
            return new ServiceResponse<List<string>> { Success = true, Data = blobs };
        }

        _logger.LogError("List operation failed. Status: {StatusCode}, Reason: {Reason}",
            response.StatusCode, response.ReasonPhrase);
        return new ServiceResponse<List<string>> { Success = false, Message = $"List failed: {response.ReasonPhrase}" };
        
    }

    /// <summary>
    /// Retrieves configuration settings from cloud storage.
    /// </summary>
    /// <param name="setting">Name of the configuration setting to retrieve.</param>
    /// <returns>Service response containing the configuration value.</returns>
    public async Task<ServiceResponse<string>> RetrieveConfigAsync(string setting)
    {
        _logger.LogInformation("Starting config retrieval for setting: {Setting}", setting);

        /*try
        {*/
        var requestUrl = GetRequestUrl($"config/{_team}/{setting}");
        var response = await _httpClient.GetAsync(requestUrl);

        if (response.IsSuccessStatusCode)
        {
            var config = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Successfully retrieved config for setting: {Setting}", setting);
            return new ServiceResponse<string> { Success = true, Data = config };
        }

        _logger.LogError("Config retrieval failed for setting: {Setting}. Status: {StatusCode}, Reason: {Reason}",
            setting, response.StatusCode, response.ReasonPhrase);
        return new ServiceResponse<string> { Success = false, Message = $"Config retrieval failed: {response.ReasonPhrase}" };
        
    }

    /// <summary>
    /// Updates an existing blob in cloud storage.
    /// </summary>
    /// <param name="blobName">Name of the blob to update.</param>
    /// <param name="content">New content stream for the blob.</param>
    /// <param name="contentType">Content type of the updated blob.</param>
    /// <returns>Service response containing update operation result.</returns>
    public async Task<ServiceResponse<string>> UpdateAsync(string blobName, Stream content, string contentType)
    {
        _logger.LogInformation("Starting update operation for blob: {BlobName}, ContentType: {ContentType}", blobName, contentType);
        /*
        try
        {*/
        var requestUrl = GetRequestUrl($"put/{_team}/{blobName}");

        // Create HTTP content from the stream
        using var streamContent = new StreamContent(content);

        // Set the content type header to match Azure Function's expectations
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

        // Send PUT request
        var response = await _httpClient.PutAsync(requestUrl, streamContent);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Successfully updated blob: {BlobName}", blobName);
            return new ServiceResponse<string>
            {
                Success = true,
                Data = responseContent,
                Message = $"Successfully updated {blobName}"
            };
        }

        var errorMessage = $"Update failed: {response.StatusCode} - {response.ReasonPhrase}";
        _logger.LogError("Update failed for blob: {BlobName}. Status: {StatusCode}, Reason: {Reason}",
            blobName, response.StatusCode, response.ReasonPhrase);

        return new ServiceResponse<string>
        {
            Success = false,
            Message = errorMessage
        };
        
    }

    /// <summary>
    /// Searches JSON files in cloud storage for specific key-value pairs.
    /// </summary>
    /// <param name="searchKey">JSON key to search for.</param>
    /// <param name="searchValue">Value to match against the specified key.</param>
    /// <returns>Service response containing search results with matching files.</returns>
    public async Task<ServiceResponse<JsonSearchResponse>> SearchJsonFilesAsync(string searchKey, string searchValue)
    {
        _logger.LogInformation("Starting JSON search with key: {SearchKey}, value: {SearchValue}", searchKey, searchValue);

        /*try
        {*/
        if (string.IsNullOrEmpty(searchKey) || string.IsNullOrEmpty(searchValue))
        {
            return new ServiceResponse<JsonSearchResponse>
            {
                Success = false,
                Message = "Both searchKey and searchValue are required"
            };
        }

        var requestUrl = GetRequestUrl($"search/{_team}") + $"&key={Uri.EscapeDataString(searchKey)}&value={Uri.EscapeDataString(searchValue)}";
        var response = await _httpClient.GetAsync(requestUrl);

        if (response.IsSuccessStatusCode)
        {
            var searchResponse = await response.Content.ReadFromJsonAsync<JsonSearchResponse>();
            _logger.LogInformation("Successfully completed JSON search. Found {MatchCount} matches", searchResponse?.MatchCount ?? 0);

            return new ServiceResponse<JsonSearchResponse>
            {
                Success = true,
                Data = searchResponse,
                Message = $"Search completed successfully. Found {searchResponse?.MatchCount ?? 0} matches."
            };
        }

        _logger.LogError("JSON search failed. Status: {StatusCode}, Reason: {Reason}",
            response.StatusCode, response.ReasonPhrase);

        return new ServiceResponse<JsonSearchResponse>
        {
            Success = false,
            Message = $"Search failed: {response.ReasonPhrase}"
        };
        
    }
}