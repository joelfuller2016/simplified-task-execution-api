
using SimplifiedTaskExecutionApi.Core.Models;

namespace SimplifiedTaskExecutionApi.Core.Services;

/// <summary>
/// Main service interface for workflow operations
/// </summary>
public interface IWorkflowService
{
    /// <summary>
    /// Register a workflow definition
    /// </summary>
    /// <param name="workflow">The workflow to register</param>
    /// <returns>The ID of the registered workflow</returns>
    Task<string> RegisterWorkflowAsync(Workflow workflow);
    
    /// <summary>
    /// Submit a workflow for execution
    /// </summary>
    /// <param name="workflow">The workflow to execute</param>
    /// <returns>The execution ID for tracking</returns>
    Task<string> SubmitWorkflowAsync(Workflow workflow);
    
    /// <summary>
    /// Get the status of a workflow execution
    /// </summary>
    /// <param name="executionId">The execution ID</param>
    /// <returns>The status or null if not found</returns>
    Task<WorkflowStatus?> GetWorkflowStatusAsync(string executionId);
    
    /// <summary>
    /// Cancel a workflow execution
    /// </summary>
    /// <param name="executionId">The execution ID</param>
    /// <returns>True if cancelled successfully</returns>
    Task<bool> CancelWorkflowAsync(string executionId);
    
    /// <summary>
    /// Get the execution history for a workflow
    /// </summary>
    /// <param name="workflowId">The workflow ID</param>
    /// <param name="limit">Maximum number of results</param>
    /// <returns>List of execution statuses</returns>
    Task<IEnumerable<WorkflowStatus>> GetWorkflowHistoryAsync(string workflowId, int limit = 20);
    
    /// <summary>
    /// Get the execution history for a workflow by name
    /// </summary>
    /// <param name="workflowName">The workflow name</param>
    /// <param name="limit">Maximum number of results</param>
    /// <returns>List of execution statuses</returns>
    Task<IEnumerable<WorkflowStatus>> GetWorkflowHistoryByNameAsync(string workflowName, int limit = 20);
    
    /// <summary>
    /// Trigger a workflow by endpoint
    /// </summary>
    /// <param name="endpoint">The endpoint path</param>
    /// <returns>The execution ID or null if no workflow found</returns>
    Task<string?> TriggerWorkflowByEndpointAsync(string endpoint);
    
    /// <summary>
    /// Get all registered workflows
    /// </summary>
    /// <returns>List of all workflows</returns>
    Task<IEnumerable<Workflow>> GetAllWorkflowsAsync();
    
    /// <summary>
    /// Get a workflow by ID
    /// </summary>
    /// <param name="workflowId">The workflow ID</param>
    /// <returns>The workflow or null if not found</returns>
    Task<Workflow?> GetWorkflowAsync(string workflowId);
    
    /// <summary>
    /// Delete a workflow
    /// </summary>
    /// <param name="workflowId">The workflow ID</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteWorkflowAsync(string workflowId);
}
