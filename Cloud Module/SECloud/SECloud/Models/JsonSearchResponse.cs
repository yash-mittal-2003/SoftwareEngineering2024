/*******************************************************************************
 * Filename    = JsonSearchResponse.cs
 *
 * Author      = Arnav Rajesh Kadu
 *
 * Product     = SECloud
 * 
 * Project     = Unnamed Software Project
 *
 * Description = Model class representing the response of a JSON search operation.
 *******************************************************************************/

namespace SECloud.Models
{
    /// <summary>
    /// Represents the response of a JSON search operation, containing the search criteria and the list of matches.
    /// </summary>
    public class JsonSearchResponse
    {
        /// <summary>
        /// The key used in the JSON search.
        /// </summary>
        public string? SearchKey { get; set; }

        /// <summary>
        /// The value associated with the search key in the JSON search.
        /// </summary>
        public string? SearchValue { get; set; }

        /// <summary>
        /// The count of matches found during the search.
        /// </summary>
        public int MatchCount { get; set; }

        /// <summary>
        /// The list of matched results found in the JSON search.
        /// </summary>
        public List<JsonSearchMatch>? Matches { get; set; }
    }
}
