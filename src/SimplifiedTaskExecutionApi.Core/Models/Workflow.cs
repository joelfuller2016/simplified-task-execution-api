
using System.Text.Json.Serialization;

namespace SimplifiedTaskExecutionApi.Core.Models;

/// <summary>
/// Represents a workflow to be executed by the system
/// </summary>
public class Workflow
{
    /// <summary>
    /// Unique identifier for the workflow
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Name of the workflow
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of the workflow (e.g., "FactotumTrigger")
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Dictionary of parameters for the workflow
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
    
    /// <summary>
    /// List of steps to execute as part of this workflow
    /// </summary>
    public List<WorkflowStep> Steps { get; set; } = new();

    /// <summary>
    /// Creation time of the workflow
    /// </summary>
    [JsonIgnore]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last modification time of the workflow
    /// </summary>
    [JsonIgnore]
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Maximum time to allow the workflow to run before timing out
    /// </summary>
    public TimeSpan? Timeout
    {
        get
        {
            if (Parameters.TryGetValue("RunSeconds", out var value) && 
                value is int seconds && seconds > 0)
            {
                return TimeSpan.FromSeconds(seconds);
            }
            
            return null;
        }
    }

    /// <summary>
    /// Get the endpoint path for this workflow
    /// </summary>
    public string? Endpoint
    {
        get
        {
            if (Parameters.TryGetValue("Endpoint", out var value) && 
                value is string endpoint)
            {
                return endpoint;
            }
            
            return null;
        }
    }

    /// <summary>
    /// CRON expression for scheduling the workflow
    /// </summary>
    public string? CronExpression
    {
        get
        {
            if (Parameters.TryGetValue("Cron", out var value) && 
                value is string cron)
            {
                return cron;
            }
            
            return null;
        }
    }
}
