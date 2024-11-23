/******************************************************************************
 * Filename    = Program.cs
 *
 * Author      = Arnav Rajesh Kadu
 *
 * Product     = Cloud
 * 
 * Project     = Unnamed Software Project
 *
 * Description = Entry point for setting up the Serverless Storage API application.
 *               Configures dependency injection, logging, and Azure Blob Storage
 *               client.
 *****************************************************************************/

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using ServerlessStorageAPI;

public class Program
{
    /// <summary>
    /// Entry point for the Serverless Storage API application.
    /// Configures services, logging, and BlobServiceClient setup.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    public static void Main()
    {
        IHost host = new HostBuilder()
            .ConfigureFunctionsWebApplication()
            .ConfigureServices((context, services) => {
                services.AddApplicationInsightsTelemetryWorkerService();
                services.ConfigureFunctionsApplicationInsights();

                // Register configuration for dependency injection
                services.AddSingleton<IConfiguration>(context.Configuration);

                // Add logging
                services.AddLogging();

                // Register BlobServiceClient with connection string from configuration
                services.AddSingleton(sp => {
                    IConfiguration configuration = sp.GetRequiredService<IConfiguration>();
                    string? connectionString = configuration["AzureWebJobsStorage"];
                    if (string.IsNullOrEmpty(connectionString))
                    {
                        throw new InvalidOperationException("AzureWebJobsStorage connection string is not configured.");
                    }
                    return new BlobServiceClient(connectionString);
                });

                // Register ConfigRetrieve service
                services.AddScoped<ConfigRetrieve>();
            })
            .Build();

        host.Run();
    }
}
