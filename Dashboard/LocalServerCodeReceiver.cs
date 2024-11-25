using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Auth.OAuth2.Web;
using System;
using System.Collections.Specialized;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Dashboard;

/// <summary>
/// Local server code receiver class implementing ICodeReceiver interface.
/// Handles the redirection and reception of OAuth 2.0 authorization codes.
/// </summary>
public class LocalServerCodeReceiver : ICodeReceiver
{
    private readonly string redirectUri;

    /// <summary>
    /// Initializes a new instance of the LocalServerCodeReceiver class.
    /// </summary>
    /// <param name="redirectUri">The redirect URI for the OAuth 2.0 response.</param>
    public LocalServerCodeReceiver(string redirectUri)
    {
        if (!redirectUri.EndsWith("/"))
        {
            redirectUri += "/";
        }
        this.redirectUri = redirectUri;
    }

    /// <summary>
    /// Gets the redirect URI.
    /// </summary>
    public string RedirectUri => redirectUri;

    /// <summary>
    /// Receives the authorization code asynchronously.
    /// </summary>
    /// <param name="url">The authorization code request URL.</param>
    /// <param name="taskCancellationToken">The cancellation token.</param>
    /// <returns>The authorization code response URL.</returns>
    public async Task<AuthorizationCodeResponseUrl> ReceiveCodeAsync(AuthorizationCodeRequestUrl url, CancellationToken taskCancellationToken)
    {
        string authorizationUrl = url.Build().AbsoluteUri;

        using var listener = new HttpListener();
        listener.Prefixes.Add(RedirectUri);
        listener.Start();

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                FileName = authorizationUrl,
                UseShellExecute = true
            });

            // Wait for the authorization response with cancellation token
            HttpListenerContext context = await listener.GetContextAsync().ConfigureAwait(false);

            // Extract query parameters before sending response
            NameValueCollection queryString = context.Request.QueryString;
            LogQueryString(queryString);

            // Create response parameters dictionary
            var responseParameters = new System.Collections.Generic.Dictionary<string, string>();
            foreach (string key in queryString.AllKeys)
            {
                responseParameters[key] = queryString[key] ?? string.Empty;
            }

            // Validate response parameters
            if (queryString == null || !queryString.HasKeys())
            {
                throw new InvalidOperationException("No query string received.");
            }

            string code = queryString["code"];
            string error = queryString["error"];

            if (!string.IsNullOrEmpty(error))
            {
                throw new InvalidOperationException($"OAuth authorization error: {error}");
            }

            if (string.IsNullOrEmpty(code))
            {
                throw new InvalidOperationException("No authorization code found in the response.");
            }

            // Send response to browser with a hyperlink to close the window
            using (HttpListenerResponse response = context.Response)
            {
                string responseString = @"
                        <html>
                            <head>
                                <title>Authorization Complete</title>
                                <script type='text/javascript'>
                                    function closeWindow() {
                                        window.open('', '_self').close();
                                    }
                                </script>
                            </head>
                            <body>
                                Authorization complete. You can <a href='javascript:closeWindow()'>close</a> this window.
                            </body>
                        </html>";
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }

            return new AuthorizationCodeResponseUrl(responseParameters);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to receive authorization response: {ex.Message}", ex);
        }
        finally
        {
            listener.Stop();
        }
    }

    /// <summary>
    /// Logs the query string parameters.
    /// </summary>
    /// <param name="queryString">The query string parameters.</param>
    private void LogQueryString(NameValueCollection queryString)
    {
        if (queryString == null)
        {
            Console.WriteLine("Query string is null.");
            return;
        }

        Console.WriteLine("Full query string: " + queryString.ToString());

        Console.WriteLine("Received query string parameters:");
        foreach (string key in queryString.AllKeys)
        {
            Console.WriteLine($"{key}: {queryString[key]}");
        }
    }
}
