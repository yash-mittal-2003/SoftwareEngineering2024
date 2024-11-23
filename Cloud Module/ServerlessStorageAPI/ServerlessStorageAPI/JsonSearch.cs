/******************************************************************************
 * Filename    = JsonSearch.cs
 *
 * Author      = Arnav Rajesh Kadu
 *
 * Product     = Cloud
 * 
 * Project     = Unnamed Software Project
 *
 * Description = Searches JSON files in Azure Blob Storage for specified key-value pairs
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
using System.Collections.Generic;

namespace ServerlessStorageAPI;

/// <summary>
/// JSON Search Class for searching through blob containers to locate JSON files
/// containing specified key-value pairs.
/// </summary>
public class JsonSearch
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<JsonSearch> _logger;

    /// <summary>
    /// Initializes a new instance of the JsonSearch class.
    /// </summary>
    /// <param name="blobServiceClient">Client to interact with Azure Blob Storage.</param>
    /// <param name="logger">Logger to record log messages.</param>
    /// <param name="configuration">Configuration for accessing settings.</param>
    public JsonSearch(BlobServiceClient blobServiceClient, ILogger<JsonSearch> logger)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;
    }

    /// <summary>
    /// Azure Function to search for specified key-value pairs across all JSON files in a container.
    /// Triggered by an HTTP GET request.
    /// </summary>
    /// <param name="req">The HTTP request initiating the search.</param>
    /// <param name="team">Name of the container to search in.</param>
    /// <returns>A response containing matching JSON files and their content.</returns>
    [Function("SearchJsonFiles")]
    public async Task<HttpResponseData> SearchJsonFiles(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "search/{team}")] HttpRequestData req,
        string team)
    {
        try
        {
            _logger.LogInformation($"Search function triggered for container: {team}");

            // Retrieve search parameters from the query string
            System.Collections.Specialized.NameValueCollection queryDictionary = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            string? searchKey = queryDictionary["key"];
            string? searchValue = queryDictionary["value"];

            // Redundant
            if (string.IsNullOrEmpty(searchKey) || string.IsNullOrEmpty(searchValue))
            {
                HttpResponseData badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("Both 'key' and 'value' query parameters are required");
                return badRequest;
            }

            // Access the specified blob container
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(team);

            // Check if the container exists
            if (!await containerClient.ExistsAsync())
            {
                _logger.LogWarning($"Container {team} does not exist.");
                HttpResponseData notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync($"Unable to find team {team} on the Azure Portal.");
                return notFoundResponse;
            }

            var matchingFiles = new List<object>();

            // Enumerate through all blobs in the container
            await foreach (Azure.Storage.Blobs.Models.BlobItem blobItem in containerClient.GetBlobsAsync())
            {
                if (!blobItem.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                BlobClient blobClient = containerClient.GetBlobClient(blobItem.Name);

                try
                {
                    // Download and parse JSON content from the blob
                    Response<Azure.Storage.Blobs.Models.BlobDownloadResult> content = await blobClient.DownloadContentAsync();
                    string jsonContent = content.Value.Content.ToString();
                    using JsonDocument doc = JsonDocument.Parse(jsonContent);

                    // Search for specified key-value pair in JSON document
                    if (SearchJsonElement(doc.RootElement, searchKey, searchValue))
                    {
                        matchingFiles.Add(new {
                            fileName = blobItem.Name,
                            content = JsonDocument.Parse(jsonContent).RootElement
                        });
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning($"Error parsing JSON in file {blobItem.Name}: {ex.Message}");
                    continue;
                }
            }

            // Create and return response with matching files
            HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new {
                searchKey,
                searchValue,
                matchCount = matchingFiles.Count,
                matches = matchingFiles
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing search request");
            HttpResponseData errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error processing request: {ex.Message}");
            return errorResponse;
        }
    }

    /// <summary>
    /// Recursively searches through JSON elements for a specified key-value pair.
    /// </summary>
    /// <param name="element">The JSON element to search within.</param>
    /// <param name="searchKey">The key to search for.</param>
    /// <param name="searchValue">The value associated with the key.</param>
    /// <returns>True if the key-value pair is found; otherwise, false.</returns>
    private static bool SearchJsonElement(JsonElement element, string searchKey, string searchValue)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    if (property.Name.Equals(searchKey, StringComparison.OrdinalIgnoreCase) &&
                        property.Value.ToString().Equals(searchValue, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    if (SearchJsonElement(property.Value, searchKey, searchValue))
                    {
                        return true;
                    }
                }
                break;

            case JsonValueKind.Array:
                foreach (JsonElement arrayElement in element.EnumerateArray())
                {
                    if (SearchJsonElement(arrayElement, searchKey, searchValue))
                    {
                        return true;
                    }
                }
                break;
        }

        return false;
    }
}
