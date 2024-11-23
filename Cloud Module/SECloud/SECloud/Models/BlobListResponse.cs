/*******************************************************************************
 * Filename    = BlobListResponse.cs
 *
 * Author      = Arnav Rajesh Kadu
 *
 * Product     = SECloud
 * 
 * Project     = Unnamed Software Project
 *
 * Description = Model class representing a response for listing blobs in a container.
 *******************************************************************************/

using System.Collections.Generic;

namespace SECloud.Models
{
    /// <summary>
    /// Represents a response model containing a list of blob names within a specific container.
    /// </summary>
    public class BlobListResponse
    {
        /// <summary>
        /// Gets or sets the list of blob names found in the container.
        /// </summary>
        public List<string>? Blobs { get; set; }
    }
}
