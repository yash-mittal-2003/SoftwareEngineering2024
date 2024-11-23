/******************************************************************************
 * Filename    = FileUpdate.cs
 *
 * Author      = Arnav Rajesh Kadu
 *
 * Product     = Cloud
 * 
 * Project     = Unnamed Software Project
 *
 * Description = Updates or creates files in Azure Blob Storage based on HTTP PUT requests
 *****************************************************************************/

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;

namespace ServerlessStorageAPI;

/// <summary>
/// Class responsible for handling file updates or creation in Azure Blob Storage.
/// </summary>
public class FileUpdate
{
    // Constant to hold the environment variable name for the Azure Storage connection string
    private const string ConnectionStringName = "AzureWebJobsStorage";

    /// <summary>
    /// Azure Function to update an existing file or create a new file in Azure Blob Storage.
    /// Triggered by an HTTP PUT request at the specified route (put/{team}/{filename}).
    /// </summary>
    /// <param name="req">The HTTP request containing the file data to upload or update.</param>
    /// <param name="team">Path parameter representing the team (used as the container name).</param>
    /// <param name="filename">Path parameter representing the file name to be created or updated.</param>
    /// <param name="executionContext">Execution context for the function (provides logger and other context information).</param>
    /// <returns>An HTTP response indicating the result of the file upload or update operation.</returns>
    [Function("UpdateFile")]
    public static async Task<HttpResponseData> UpdateFile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "put/{team}/{filename}")] HttpRequestData req,
        string team,
        string filename,
        FunctionContext executionContext)
    {
        // Retrieve logger from the execution context for logging information and errors
        ILogger<FileUpdate> logger = executionContext.GetLogger<FileUpdate>();
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

            // Access the container for the specified team
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

            // Check if the blob (file) exists in the container
            bool blobExists = await blobClient.ExistsAsync();

            // Read the request body and copy it to a memory stream for blob upload
            using var stream = new MemoryStream();
            await req.Body.CopyToAsync(stream);
            stream.Position = 0; // Reset stream position for reading

            // Determine the content type for the blob, defaulting to "application/octet-stream" if none is specified
            string contentType = req.Headers.TryGetValues("Content-Type", out IEnumerable<string>? contentTypeValues)
                ? contentTypeValues.FirstOrDefault() ?? "application/octet-stream"
                : "application/octet-stream";

            // Upload the file stream to the blob in Azure Blob Storage
            if (blobExists)
            {
                // Update the existing blob with new content
                await blobClient.UploadAsync(stream, new BlobUploadOptions {
                    HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
                });
                logger.LogInformation($"Successfully updated file {filename} in container {team}");
            }
            else
            {
                // Create a new blob if it does not exist
                await blobClient.UploadAsync(stream, new BlobUploadOptions {
                    HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
                });
                logger.LogInformation($"Successfully created file {filename} in container {team}");
            }

            // Return a success response indicating that the file was uploaded or updated
            response.StatusCode = HttpStatusCode.OK;
            await response.WriteStringAsync($"File {filename} uploaded/updated successfully");
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
            logger.LogError($"Error uploading file: {ex.Message}");
            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteStringAsync("An error occurred while uploading the file");
            return response;
        }
    }
}
