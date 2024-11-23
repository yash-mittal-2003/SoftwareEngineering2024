/******************************************************************************
 * Filename    = FileDelete.cs
 *
 * Author      = Pranav Guruprasad Rao
 *
 * Product     = Cloud
 * 
 * Project     = Unnamed Software Project
 *
 * Description = Deletes files from Azure Blob Storage based on HTTP DELETE requests
 *****************************************************************************/

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;

namespace ServerlessStorageAPI;

/// <summary>
/// Class responsible for handling file deletions in Azure Blob Storage.
/// </summary>
public class FileDelete
{
    // Constant to hold the environment variable name for the Azure Storage connection string
    private const string ConnectionStringName = "AzureWebJobsStorage";

    /// <summary>
    /// Azure Function to delete a file from Azure Blob Storage.
    /// Triggered by an HTTP DELETE request at the specified route (delete/{team}/{filename}).
    /// </summary>
    /// <param name="req">The HTTP request to initiate the deletion.</param>
    /// <param name="team">Path parameter representing the team (used as the container name).</param>
    /// <param name="filename">Path parameter representing the file name to be deleted.</param>
    /// <param name="executionContext">Execution context for the function (provides logger and other context information).</param>
    /// <returns>An HTTP response indicating the result of the file deletion operation.</returns>
    [Function("DeleteFile")]
    public static async Task<HttpResponseData> DeleteFile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "delete/{team}/{filename}")] HttpRequestData req,
        string team,
        string filename,
        FunctionContext executionContext)
    {
        // Retrieve logger from the execution context for logging information and errors
        ILogger<FileDelete> logger = executionContext.GetLogger<FileDelete>();
        HttpResponseData response = req.CreateResponse();

        try
        {
            // Retrieve the connection string from environment variables
            string? connectionString = Environment.GetEnvironmentVariable(ConnectionStringName);
            if (string.IsNullOrEmpty(connectionString))
            {
                logger.LogError("Storage connection string not found");
                response.StatusCode = HttpStatusCode.InternalServerError;
                await response.WriteStringAsync("Storage configuration error");
                return response;
            }

            // Initialize the BlobServiceClient using the connection string
            var blobServiceClient = new BlobServiceClient(connectionString);

            // Access the container for the specified team (assumed to be the container name)
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(team.ToLowerInvariant());

            // Check if the container exists
            if (!await containerClient.ExistsAsync())
            {
                logger.LogWarning($"Container {team} does not exist.");
                HttpResponseData notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync($"Unable to find team {team} on the Azure Portal.");
                return notFoundResponse;
            }

            // Retrieve the BlobClient for the specified file in the team container
            BlobClient blobClient = containerClient.GetBlobClient(filename);

            // Check if the specified blob (file) exists in the container
            if (!await blobClient.ExistsAsync())
            {
                logger.LogWarning($"File {filename} not found in container {team}");
                response.StatusCode = HttpStatusCode.NotFound;
                await response.WriteStringAsync($"File {filename} not found");
                return response;
            }

            // Delete the blob (file) from Azure Blob Storage
            await blobClient.DeleteAsync();

            // Log and return a success response indicating that the file was deleted
            logger.LogInformation($"Successfully deleted file {filename} from container {team}");
            response.StatusCode = HttpStatusCode.OK;
            await response.WriteStringAsync($"File {filename} deleted successfully");
            return response;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            // Handle the case where the specified container does not exist
            logger.LogWarning($"Container {team} not found: {ex.Message}");
            response.StatusCode = HttpStatusCode.NotFound;
            await response.WriteStringAsync($"Team container {team} not found");
            return response;
        }
        catch (Exception ex)
        {
            // Log and return an error response in case of an unexpected exception
            logger.LogError($"Error deleting file: {ex.Message}");
            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteStringAsync("An error occurred while deleting the file");
            return response;
        }
    }
}
