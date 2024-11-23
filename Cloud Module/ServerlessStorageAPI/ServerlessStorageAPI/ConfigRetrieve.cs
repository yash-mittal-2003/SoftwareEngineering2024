/******************************************************************************
 * Filename    = ConfigRetrieve.cs
 * 
 * Author      = Arnav Rajesh Kadu
 * 
 * Product     = Cloud
 * 
 * Project     = Unnamed Software Project
 * 
 * Description = Handles configuration requests, retrieving settings from an
 *               Azure Blob Storage file and providing HTTP responses based
 *               on the availability of the requested configuration settings.
 *****************************************************************************/

using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;

namespace ServerlessStorageAPI;

/// <summary>
/// Provides functionality to retrieve configuration settings for a given team.
/// </summary>
public class ConfigRetrieve
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<ConfigRetrieve> _logger;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigRetrieve"/> class with
    /// dependencies for blob service client, logging, and configuration.
    /// </summary>
    /// <param name="blobServiceClient">Azure Blob Service client.</param>
    /// <param name="logger">Logger instance for logging information and errors.</param>
    /// <param name="configuration">Configuration instance for accessing app settings.</param>
    public ConfigRetrieve(BlobServiceClient blobServiceClient, ILogger<ConfigRetrieve> logger, IConfiguration configuration)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Azure Function to retrieve specific configuration settings from a blob file.
    /// </summary>
    /// <param name="req">The HTTP request data.</param>
    /// <param name="team">The team name used as the blob container name.</param>
    /// <param name="setting">The specific configuration setting to retrieve.</param>
    /// <returns>An HTTP response containing the setting value or an error message.</returns>
    [Function("GetConfigSetting")]
    public async Task<HttpResponseData> GetConfigSetting(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "config/{team}/{setting}")] HttpRequestData req,
        string team,
        string setting)
    {
        try
        {
            _logger.LogInformation($"Function triggered. Team: {team}, Setting: {setting}");

            string? connectionString = _configuration["AzureWebJobsStorage"];
            _logger.LogInformation($"Connection string exists: {!string.IsNullOrEmpty(connectionString)}");

            _logger.LogInformation($"Attempting to get container client for team: {team}");
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(team);

            // Check if the container exists
            if (!await containerClient.ExistsAsync())
            {
                _logger.LogWarning($"Container {team} does not exist.");
                HttpResponseData notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync($"Unable to find team {team} on the Azure Portal.");
                return notFoundResponse;
            }

            _logger.LogInformation("Attempting to get blob client for configFile.json");
            BlobClient configBlobClient = containerClient.GetBlobClient("configFile.json");

            _logger.LogInformation("Checking if config blob exists...");
            Azure.Response<bool> blobExists = await configBlobClient.ExistsAsync();
            _logger.LogInformation($"Config blob exists: {blobExists.Value}");

            if (blobExists.Value)
            {
                _logger.LogInformation("Downloading blob content...");
                Response<Azure.Storage.Blobs.Models.BlobDownloadResult> configContent = await configBlobClient.DownloadContentAsync();
                string configJson = configContent.Value.Content.ToString();

                _logger.LogInformation($"Downloaded JSON content length: {configJson.Length}");
                _logger.LogInformation($"JSON Content: {configJson}");

                using JsonDocument doc = JsonDocument.Parse(configJson);
                _logger.LogInformation($"Attempting to find setting: {setting}");
                if (doc.RootElement.TryGetProperty(setting, out JsonElement configValue))
                {
                    _logger.LogInformation($"Setting found. Value: {configValue}");
                    HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
                    await response.WriteAsJsonAsync(new {
                        setting,
                        value = configValue.ToString()
                    });
                    return response;
                }
                else
                {
                    _logger.LogWarning($"Setting '{setting}' not found in config file");
                    HttpResponseData notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFoundResponse.WriteStringAsync($"Setting {setting} not found in configuration.");
                    return notFoundResponse;
                }
            }
            else
            {
                _logger.LogWarning($"Config file not found for team: {team}");
                HttpResponseData notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync($"Configuration file not found for team: {team}");
                return notFoundResponse;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing request. Team: {team}, Setting: {setting}");
            HttpResponseData errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error processing request: {ex.Message}");
            return errorResponse;
        }
    }
}
