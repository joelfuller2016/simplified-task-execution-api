
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SimplifiedTaskExecutionApi.Core.Models;

namespace SimplifiedTaskExecutionApi.Infrastructure.Executors;

/// <summary>
/// Executor for HTTP tasks (Process step type)
/// </summary>
public class HttpTaskExecutor
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpTaskExecutor> _logger;

    /// <summary>
    /// Constructor with dependencies
    /// </summary>
    public HttpTaskExecutor(IHttpClientFactory httpClientFactory, ILogger<HttpTaskExecutor> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Execute the HTTP task
    /// </summary>
    /// <param name="step">The step to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the HTTP operation</returns>
    public async Task<object?> ExecuteAsync(WorkflowStep step, CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint = step.GetEndpoint();
            if (string.IsNullOrEmpty(endpoint))
            {
                throw new InvalidOperationException("HTTP task requires an endpoint parameter");
            }

            var method = GetHttpMethod(step);
            var payload = GetPayload(step);
            var timeout = step.GetTimeout() ?? 60; // Default to 60 seconds
            var successCodes = step.GetSuccessResponseCodes();

            _logger.LogInformation("Executing HTTP {Method} request to {Endpoint}", method, endpoint);

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(timeout);

            using var request = CreateHttpRequest(endpoint, method, payload);
            
            // Add headers if specified
            if (step.Parameters.TryGetValue("Headers", out var headersObj) && 
                headersObj is Dictionary<string, object> headers)
            {
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value.ToString());
                }
            }

            using var response = await client.SendAsync(request, cancellationToken);

            _logger.LogInformation("HTTP request to {Endpoint} completed with status: {StatusCode}", 
                endpoint, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            
            // Check if the response is successful based on the configured success codes
            if (!successCodes.Contains((int)response.StatusCode))
            {
                throw new HttpRequestException(
                    $"HTTP request failed with status code {response.StatusCode}. Response: {content}");
            }

            // Try to parse as JSON if possible
            try
            {
                return JsonConvert.DeserializeObject(content);
            }
            catch
            {
                // If not valid JSON, return as string
                return content;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing HTTP task: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Create HTTP request message
    /// </summary>
    private HttpRequestMessage CreateHttpRequest(string endpoint, string method, object? payload)
    {
        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(endpoint),
            Method = new HttpMethod(method)
        };

        if (payload != null)
        {
            var content = JsonConvert.SerializeObject(payload);
            request.Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json");
        }

        return request;
    }

    /// <summary>
    /// Get the HTTP method from step parameters
    /// </summary>
    private string GetHttpMethod(WorkflowStep step)
    {
        if (step.Parameters.TryGetValue("Method", out var value) && value is string method)
        {
            return method.ToUpperInvariant();
        }

        return "GET"; // Default to GET
    }

    /// <summary>
    /// Get the payload from step parameters
    /// </summary>
    private object? GetPayload(WorkflowStep step)
    {
        if (step.Parameters.TryGetValue("Payload", out var payload))
        {
            return payload;
        }

        return null;
    }
}
