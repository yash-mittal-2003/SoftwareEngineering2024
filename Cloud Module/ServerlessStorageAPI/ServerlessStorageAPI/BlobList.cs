/******************************************************************************
 * Filename    = ListBlobs.cs
 *
 * Author      = Arnav Rajesh Kadu
 *
 * Product     = Cloud
 * 
 * Project     = Unnamed Software Project
 *
 * Description = To list blobs in an Azure Blob Storage container
 *****************************************************************************/

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace ServerlessStorageAPI;

/// <summary>
/// Class responsible for listing blobs in an Azure Blob Storage container.
/// </summary>
public class BlobList
{
    private const string ConnectionStringValue = "AzureWebJobsStorage";

    /// <summary>
    /// Azure Function to list blobs in an Azure Blob Storage container.
    /// Trigger function with an HTTP GET request to the specified route (list/{team}).
    /// </summary>
    /// <param name="req">The HTTP request to list blobs.</param>
    /// <param name="team">Path parameter representing the team (used as the container name).</param>
    /// <param name="executionContext">Execution context for the function (provides logger and other context information).</param>
    /// <returns>An HTTP response with a list of blob names in the specified container.</returns>
    [Function("ListBlobs")] // Define the Azure Function named "ListBlobs".
    public static async Task<HttpResponseData> ListBlobs(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "list/{team}")] HttpRequestData req,
        string team,
        FunctionContext executionContext)
    {
        ILogger<BlobList> logger = executionContext.GetLogger<BlobList>();
        logger.LogInformation($"Listing blobs in the container: {team}");

        try
        {
            // Create a BlobContainerClient using the connection string and the team name as the container name.
            BlobContainerClient containerClient = new BlobContainerClient(
                Environment.GetEnvironmentVariable(ConnectionStringValue),
                team);

            // Check if the container exists
            if (!await containerClient.ExistsAsync())
            {
                logger.LogWarning($"Container {team} does not exist.");
                HttpResponseData notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync($"Unable to find team {team} on the Azure Portal.");
                return notFoundResponse;
            }

            var blobList = new List<string>();

            // List blobs in the container
            await foreach (Azure.Storage.Blobs.Models.BlobItem blobItem in containerClient.GetBlobsAsync())
            {
                blobList.Add(blobItem.Name);
            }

            // Create an OK response with the list of blob names.
            HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { Blobs = blobList });
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing blobs");
            // Create an Internal Server Error response with the exception message.
            HttpResponseData errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error listing blobs: {ex.Message}");
            return errorResponse;
        }
    }
}
