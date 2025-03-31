
using SimplifiedTaskExecutionApi.Core.Models;

namespace SimplifiedTaskExecutionApi.Core.Repositories;

/// <summary>
/// Repository interface for workflow operations
/// </summary>
public interface IWorkflowRepository
{
    /// <summary>
    /// Save a workflow definition
    /// </summary>
    /// <param name="workflow">The workflow to save</param>
    /// <returns>The ID of the saved workflow</returns>
    Task<string> SaveWorkflowAsync(Workflow workflow);
    
    /// <summary>
    /// Get a workflow by ID
    /// </summary>
    /// <param name="id">The ID of the workflow</param>
    /// <returns>The workflow or null if not found</returns>
    Task<Workflow?> GetWorkflowAsync(string id);
    
    /// <summary>
    /// Get a workflow by endpoint path
    /// </summary>
    /// <param name="endpoint">The endpoint path</param>
    /// <returns>The workflow or null if not found</returns>
    Task<Workflow?> GetWorkflowByEndpointAsync(string endpoint);
    
    /// <summary>
    /// Get all workflows
    /// </summary>
    /// <returns>List of all workflows</returns>
    Task<IEnumerable<Workflow>> GetAllWorkflowsAsync();
    
    /// <summary>
    /// Delete a workflow
    /// </summary>
    /// <param name="id">The ID of the workflow to delete</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteWorkflowAsync(string id);
    
    /// <summary>
    /// Create a new workflow execution
    /// </summary>
    /// <param name="workflowId">The ID of the workflow</param>
    /// <returns>The workflow execution status</returns>
    Task<WorkflowStatus> CreateWorkflowExecutionAsync(string workflowId);
    
    /// <summary>
    /// Get a workflow execution status by ID
    /// </summary>
    /// <param name="executionId">The execution ID</param>
    /// <returns>The workflow status or null if not found</returns>
    Task<WorkflowStatus?> GetWorkflowStatusAsync(string executionId);
    
    /// <summary>
    /// Update a workflow execution status
    /// </summary>
    /// <param name="status">The updated workflow status</param>
    /// <returns>True if updated successfully</returns>
    Task<bool> UpdateWorkflowStatusAsync(WorkflowStatus status);
    
    /// <summary>
    /// Get execution history for a workflow
    /// </summary>
    /// <param name="workflowId">The workflow ID</param>
    /// <param name="limit">Maximum number of results</param>
    /// <returns>List of workflow execution statuses</returns>
    Task<IEnumerable<WorkflowStatus>> GetWorkflowHistoryAsync(string workflowId, int limit = 20);
    
    /// <summary>
    /// Get execution history by workflow name
    /// </summary>
    /// <param name="workflowName">The workflow name</param>
    /// <param name="limit">Maximum number of results</param>
    /// <returns>List of workflow execution statuses</returns>
    Task<IEnumerable<WorkflowStatus>> GetWorkflowHistoryByNameAsync(string workflowName, int limit = 20);
}
