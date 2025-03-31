
namespace SimplifiedTaskExecutionApi.Core.Models;

/// <summary>
/// Status of a workflow execution
/// </summary>
public enum ExecutionState
{
    /// <summary>
    /// Workflow or step has been created but not started
    /// </summary>
    Pending,
    
    /// <summary>
    /// Workflow or step is currently executing
    /// </summary>
    Running,
    
    /// <summary>
    /// Workflow or step has completed successfully
    /// </summary>
    Completed,
    
    /// <summary>
    /// Workflow or step has failed
    /// </summary>
    Failed,
    
    /// <summary>
    /// Workflow or step has been cancelled
    /// </summary>
    Cancelled,
    
    /// <summary>
    /// Workflow or step was skipped due to a dependency failure
    /// </summary>
    Skipped,
    
    /// <summary>
    /// Workflow or step timed out
    /// </summary>
    TimedOut
}

/// <summary>
/// Represents the execution status of a workflow
/// </summary>
public class WorkflowStatus
{
    /// <summary>
    /// Unique execution identifier
    /// </summary>
    public string ExecutionId { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// The ID of the workflow being executed
    /// </summary>
    public string WorkflowId { get; set; } = string.Empty;
    
    /// <summary>
    /// The name of the workflow being executed
    /// </summary>
    public string WorkflowName { get; set; } = string.Empty;
    
    /// <summary>
    /// Current state of the workflow execution
    /// </summary>
    public ExecutionState State { get; set; } = ExecutionState.Pending;
    
    /// <summary>
    /// Start time of the execution
    /// </summary>
    public DateTime? StartTime { get; set; }
    
    /// <summary>
    /// End time of the execution
    /// </summary>
    public DateTime? EndTime { get; set; }
    
    /// <summary>
    /// Duration of the execution (if completed)
    /// </summary>
    public TimeSpan? Duration => EndTime.HasValue && StartTime.HasValue 
        ? EndTime.Value - StartTime.Value 
        : null;
    
    /// <summary>
    /// Error message (if execution failed)
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Status of each step in the workflow
    /// </summary>
    public List<StepStatus> Steps { get; set; } = new();
}

/// <summary>
/// Represents the execution status of a workflow step
/// </summary>
public class StepStatus
{
    /// <summary>
    /// Step identifier
    /// </summary>
    public string StepId { get; set; } = string.Empty;
    
    /// <summary>
    /// Step name
    /// </summary>
    public string StepName { get; set; } = string.Empty;
    
    /// <summary>
    /// Step type
    /// </summary>
    public string StepType { get; set; } = string.Empty;
    
    /// <summary>
    /// Current state of the step execution
    /// </summary>
    public ExecutionState State { get; set; } = ExecutionState.Pending;
    
    /// <summary>
    /// Start time of the step execution
    /// </summary>
    public DateTime? StartTime { get; set; }
    
    /// <summary>
    /// End time of the step execution
    /// </summary>
    public DateTime? EndTime { get; set; }
    
    /// <summary>
    /// Result of the step execution (if applicable)
    /// </summary>
    public object? Result { get; set; }
    
    /// <summary>
    /// Error message (if step failed)
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Child steps (for parallel/serial steps)
    /// </summary>
    public List<StepStatus>? ChildSteps { get; set; }
    
    /// <summary>
    /// Number of retry attempts (if applicable)
    /// </summary>
    public int RetryCount { get; set; }
}
