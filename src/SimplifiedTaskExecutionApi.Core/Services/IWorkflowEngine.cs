
using SimplifiedTaskExecutionApi.Core.Models;

namespace SimplifiedTaskExecutionApi.Core.Services;

/// <summary>
/// Interface for the workflow execution engine
/// </summary>
public interface IWorkflowEngine
{
    /// <summary>
    /// Execute a workflow
    /// </summary>
    /// <param name="workflow">The workflow to execute</param>
    /// <param name="executionId">Optional execution ID, if not provided a new one will be generated</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The workflow execution status</returns>
    Task<WorkflowStatus> ExecuteWorkflowAsync(Workflow workflow, string? executionId = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Process a workflow step
    /// </summary>
    /// <param name="step">The step to process</param>
    /// <param name="executionContext">Execution context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The step status</returns>
    Task<StepStatus> ProcessStepAsync(WorkflowStep step, object? executionContext = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cancel a workflow execution
    /// </summary>
    /// <param name="executionId">The execution ID</param>
    /// <returns>True if the execution was cancelled successfully</returns>
    Task<bool> CancelExecutionAsync(string executionId);
    
    /// <summary>
    /// Process any pending workflows
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of workflows processed</returns>
    Task<int> ProcessPendingWorkflowsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validate a workflow definition
    /// </summary>
    /// <param name="workflow">The workflow to validate</param>
    /// <returns>List of validation errors or an empty list if valid</returns>
    Task<List<string>> ValidateWorkflowAsync(Workflow workflow);
}
