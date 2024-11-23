/*******************************************************************************
 * Filename    = ServiceResponse.cs
 *
 * Author      = Arnav Rajesh Kadu
 *
 * Product     = SECloud
 * 
 * Project     = Unnamed Software Project
 *
 * Description = Generic response model for returning data and status of service operations.
 *******************************************************************************/

namespace SECloud.Models
{
    /// <summary>
    /// A generic response model used for returning the success status, message, and data of a service operation.
    /// </summary>
    /// <typeparam name="T">The type of data being returned in the response.</typeparam>
    public class ServiceResponse<T>
    {
        /// <summary>
        /// Indicates whether the service operation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Provides a message detailing the outcome of the service operation.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the data associated with the service operation, if any.
        /// </summary>
        public T? Data { get; set; }
    }
}
