/*******************************************************************************
 * Filename    = ICloud.cs
 *
 * Author      = Pranav Rao
 *
 * Product     = SECloud
 * 
 * Project     = Unnamed Software Project
 *
 * Description = Interface defining cloud operations for Azure Blob storage, 
 *               including upload, update, download, delete, listing, and JSON search functionalities.
 *******************************************************************************/

using SECloud.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SECloud.Interfaces
{
    /// <summary>
    /// Defines an interface for cloud storage operations, supporting file upload, update, download, 
    /// deletion, listing, and JSON file search capabilities.
    /// </summary>
    public interface ICloud
    {
        /// <summary>
        /// Asynchronously uploads a file to cloud storage.
        /// </summary>
        /// <param name="blobName">The name to assign to the blob in storage.</param>
        /// <param name="content">The content of the file to upload.</param>
        /// <param name="contentType">The MIME type of the content.</param>
        /// <returns>A <see cref="ServiceResponse{T}"/> with the URL or identifier of the uploaded blob.</returns>
        Task<ServiceResponse<string>> UploadAsync(string blobName, Stream content, string contentType);

        /// <summary>
        /// Asynchronously updates an existing blob in cloud storage.
        /// </summary>
        /// <param name="blobName">The name of the blob to update.</param>
        /// <param name="content">The updated content of the blob.</param>
        /// <param name="contentType">The MIME type of the content.</param>
        /// <returns>A <see cref="ServiceResponse{T}"/> indicating the success status and message.</returns>
        Task<ServiceResponse<string>> UpdateAsync(string blobName, Stream content, string contentType);

        /// <summary>
        /// Asynchronously downloads a blob from cloud storage.
        /// </summary>
        /// <param name="blobName">The name of the blob to download.</param>
        /// <returns>A <see cref="ServiceResponse{T}"/> containing the blob's content stream.</returns>
        Task<ServiceResponse<Stream>> DownloadAsync(string blobName);

        /// <summary>
        /// Asynchronously deletes a blob from cloud storage.
        /// </summary>
        /// <param name="blobName">The name of the blob to delete.</param>
        /// <returns>A <see cref="ServiceResponse{T}"/> indicating whether the deletion was successful.</returns>
        Task<ServiceResponse<bool>> DeleteAsync(string blobName);

        /// <summary>
        /// Asynchronously retrieves a list of all blob names in the cloud storage container.
        /// </summary>
        /// <returns>A <see cref="ServiceResponse{T}"/> containing a list of blob names.</returns>
        Task<ServiceResponse<List<string>>> ListBlobsAsync();

        /// <summary>
        /// Asynchronously searches JSON files in cloud storage for matches based on a specified key and value.
        /// </summary>
        /// <param name="searchkey">The key to search for in JSON files.</param>
        /// <param name="searchValue">The value to match for the specified key.</param>
        /// <returns>A <see cref="ServiceResponse{T}"/> containing search results, including matching files and their content.</returns>
        Task<ServiceResponse<JsonSearchResponse>> SearchJsonFilesAsync(string searchkey, string searchValue);
    }
}
