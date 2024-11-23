/*******************************************************************************
 * Filename    = JsonSearchMatch.cs
 *
 * Author      = Arnav Rajesh Kadu
 *
 * Product     = SECloud
 * 
 * Project     = Unnamed Software Project
 *
 * Description = Model class representing a single match in a JSON search result.
 *******************************************************************************/

using System.Text.Json;

namespace SECloud.Models
{
    /// <summary>
    /// Represents a match found in a JSON search, containing the file name and content that matched the search criteria.
    /// </summary>
    public class JsonSearchMatch
    {
        /// <summary>
        /// The name of the file where the match was found.
        /// </summary>
        public string? FileName { get; set; }

        /// <summary>
        /// The content within the file that matched the search criteria.
        /// </summary>
        public JsonElement Content { get; set; }
    }
}
