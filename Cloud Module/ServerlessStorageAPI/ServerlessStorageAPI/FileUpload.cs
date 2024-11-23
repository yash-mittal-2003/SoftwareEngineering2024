/******************************************************************************
* Filename    = FileUpload.cs
*
* Author      = Arnav Rajesh Kadu
*
* Product     = Cloud
* 
* Project     = Unnamed Software Project
*
* Description = To upload files from local to cloud
*****************************************************************************/

using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using System;
using System.Net;
using System.Net.Http.Headers;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;

namespace ServerlessStorageAPI;

/// <summary>
/// Class responsible for handling file uploads to Azure Blob Storage.
/// </summary>
public class FileUpload
{
    private const string ConnectionStringValue = "AzureWebJobsStorage";

    /// <summary>
    /// Azure Function to upload files to an Azure Blob Storage container.
    /// Trigger function with an HTTP POST request to the specified route (upload/{team}).
    /// </summary>
    /// <param name="req">The HTTP request that contains the file data to upload.</param>
    /// <param name="team">Path parameter representing the team (used as the container name).</param>
    /// <param name="executionContext">Execution context for the function (provides logger and other context information).</param>
    /// <returns>An HTTP response indicating the result of the file upload operation.</returns>
    [Function("UploadFile")] // Define the Azure Function named "UploadFile".
    public static async Task<HttpResponseData> UploadFile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "upload/{team}")] HttpRequestData req,
        string team,
        FunctionContext executionContext)
    {
        ILogger<FileUpload> logger = executionContext.GetLogger<FileUpload>();
        logger.LogInformation($"Uploading a file to the container: {team}");

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

            // Check if the request contains a valid "multipart/form-data" content type.
            if (!req.Headers.TryGetValues("Content-Type", out IEnumerable<string>? contentTypes) || !contentTypes.First().Contains("multipart/form-data"))
            {
                logger.LogWarning("Invalid content type received");
                // Create a Bad Request response for invalid content type.
                HttpResponseData badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteStringAsync("Invalid content type. Expecting multipart/form-data.");
                return badRequestResponse;
            }

            // Extract the boundary from the Content-Type header to parse the multipart form data.
            string boundary = MultipartRequestHelper.GetBoundary(contentTypes.First());
            // Create a MultipartReader to read the multipart form data.
            var reader = new MultipartReader(boundary, req.Body);
            MultipartSection? section;

            // Read each section of the multipart form data.
            while ((section = await reader.ReadNextSectionAsync()) != null)
            {
                // Check if the section contains valid file data (with a content disposition header).
                // Ensure the disposition type is "form-data".
                // Ensure the section contains a file (has a file name).
                if (ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out ContentDispositionHeaderValue? contentDisposition) &&
                    contentDisposition != null &&
                    contentDisposition.DispositionType.Equals("form-data") &&
                    contentDisposition.FileName != null)
                {
                    // Clean the filename
                    string? fileName = contentDisposition.FileName
                        ?.Trim('"')
                        ?.Replace("\\", "")
                        ?.Trim();

                    if (string.IsNullOrEmpty(fileName))
                    {
                        logger.LogWarning("Empty filename detected");
                        continue;
                    }

                    logger.LogInformation($"Processing file: {fileName}");

                    BlobClient blobClient = containerClient.GetBlobClient(fileName);

                    // Upload the file's content to the blob.
                    using (Stream stream = section.Body)
                    {
                        await blobClient.UploadAsync(stream, true);
                        logger.LogInformation($"File {fileName} uploaded successfully");
                    }

                    // Create an OK response indicating the file was successfully uploaded.
                    HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
                    await response.WriteStringAsync($"File {fileName} uploaded successfully to {team} container.");
                    return response;
                }
            }
            logger.LogWarning("No valid file section found in request");
            // Create a Bad Request response indicating no valid file was provided.
            HttpResponseData noFileResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await noFileResponse.WriteStringAsync("No valid file provided.");
            return noFileResponse;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading file");
            // Create an Internal Server Error response with the exception message.
            HttpResponseData errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error uploading file: {ex.Message}");
            return errorResponse;
        }
    }
}

/// <summary>
/// Helper class for handling multipart form data requests.
/// </summary>
public static class MultipartRequestHelper
{
    /// <summary>
    /// Extracts the boundary string from the Content-Type header for parsing multipart form data.
    /// </summary>
    /// <param name="contentType">The Content-Type header value.</param>
    /// <returns>The boundary string used to separate parts of the multipart form data.</returns>
    public static string GetBoundary(string contentType)
    {
        string[] elements = contentType.Split(' ');
        // Find the part of the header that contains the boundary.
        string? boundaryElement = elements.FirstOrDefault(entry => entry.StartsWith("boundary="));
        // Extract the boundary value, removing any surrounding quotes.
        if (!string.IsNullOrEmpty(boundaryElement))
        {
            return boundaryElement["boundary=".Length..].Trim('"');
        }
        else
        {
            throw new ArgumentException("Content-Type header does not contain a valid boundary.");
        }
    }
}
