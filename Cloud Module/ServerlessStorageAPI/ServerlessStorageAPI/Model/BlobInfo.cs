/******************************************************************************
 * Filename    = BlobInfo.cs
 *
 * Author      = Arnav Rajesh Kadu
 *
 * Product     = Cloud
 * 
 * Project     = Unnamed Software Project
 *
 * Description = Represents information about a blob in Azure Blob Storage,
 *               including properties like name, content type, size, last modified
 *               date, and URI.
 *****************************************************************************/

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace ServerlessStorageAPI.Model;

/// <summary>
/// Class that represents information about a blob in Azure Blob Storage.
/// </summary>
public class BlobInfo
{
    /// <summary>
    /// Gets or sets the name of the blob.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the MIME type of the blob content.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Gets or sets the size of the blob in bytes.
    /// </summary>
    public long? Size { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the blob was last modified.
    /// </summary>
    public DateTimeOffset? LastModified { get; set; }

    /// <summary>
    /// Gets or sets the URI of the blob.
    /// </summary>
    public string? Uri { get; set; }
}
