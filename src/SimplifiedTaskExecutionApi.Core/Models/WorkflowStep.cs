
using System.Text.Json.Serialization;
using SimplifiedTaskExecutionApi.Core.Services;

namespace SimplifiedTaskExecutionApi.Core.Models;

/// <summary>
/// Represents a step in a workflow
/// </summary>
public class WorkflowStep
{
    /// <summary>
    /// Unique identifier for the step
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Name of the step
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of the step (e.g., Process, Parallel, Serial)
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Dictionary of parameters for the step
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
    
    /// <summary>
    /// List of child steps for container types (Parallel/Serial)
    /// </summary>
    public List<WorkflowStep>? Steps { get; set; }
    
    /// <summary>
    /// Reference to the parent workflow
    /// </summary>
    [JsonIgnore]
    public Workflow? Workflow { get; set; }

    /// <summary>
    /// Accept method for the visitor pattern to allow processing of different step types
    /// </summary>
    /// <param name="visitor">The visitor implementation</param>
    /// <param name="context">Optional execution context</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public virtual async Task AcceptAsync(IStepVisitor visitor, object? context = null)
    {
        switch (Type.ToLowerInvariant())
        {
            case "process":
                await visitor.VisitProcessStepAsync(this, context);
                break;
            case "parallel":
                await visitor.VisitParallelStepAsync(this, context);
                break;
            case "serial":
                await visitor.VisitSerialStepAsync(this, context);
                break;
            case "batch":
                await visitor.VisitBatchStepAsync(this, context);
                break;
            case "executable":
                await visitor.VisitExecutableStepAsync(this, context);
                break;
            default:
                await visitor.VisitUnknownStepAsync(this, context);
                break;
        }
    }

    /// <summary>
    /// Get timeout value in seconds
    /// </summary>
    public int? GetTimeout()
    {
        if (Parameters.TryGetValue("RunSeconds", out var value) && 
            value is int seconds && seconds > 0)
        {
            return seconds;
        }
        
        return null;
    }

    /// <summary>
    /// Get endpoint path for process steps
    /// </summary>
    public string? GetEndpoint()
    {
        if (Parameters.TryGetValue("Endpoint", out var value) && 
            value is string endpoint)
        {
            return endpoint;
        }
        
        return null;
    }

    /// <summary>
    /// Get success response codes for HTTP steps
    /// </summary>
    public int[] GetSuccessResponseCodes()
    {
        if (Parameters.TryGetValue("SuccessResponseCodes", out var value) && 
            value is object[] codes)
        {
            return codes.OfType<int>().ToArray();
        }
        
        return new[] { 200 }; // Default to 200 OK
    }
}
