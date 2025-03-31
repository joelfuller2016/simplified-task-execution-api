
using Microsoft.Extensions.Logging;
using SimplifiedTaskExecutionApi.Core.Models;
using SimplifiedTaskExecutionApi.Core.Repositories;
using SimplifiedTaskExecutionApi.Core.Services;

namespace SimplifiedTaskExecutionApi.Infrastructure.Services;

/// <summary>
/// Implementation of the workflow service
/// </summary>
public class WorkflowService : IWorkflowService
{
    private readonly IWorkflowRepository _repository;
    private readonly IWorkflowEngine _engine;
    private readonly ILogger<WorkflowService> _logger;

    /// <summary>
    /// Constructor with dependencies
    /// </summary>
    public WorkflowService(
        IWorkflowRepository repository,
        IWorkflowEngine engine,
        ILogger<WorkflowService> logger)
    {
        _repository = repository;
        _engine = engine;
        _logger = logger;
    }

    /// <summary>
    /// Register a workflow definition
    /// </summary>
    public async Task<string> RegisterWorkflowAsync(Workflow workflow)
    {
        _logger.LogInformation("Registering workflow: {Name}", workflow.Name);
        
        // Validate the workflow
        var validationErrors = await _engine.ValidateWorkflowAsync(workflow);
        if (validationErrors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Workflow validation failed: {string.Join(", ", validationErrors)}");
        }

        // Check if a workflow with the same endpoint already exists
        if (workflow.Endpoint != null)
        {
            var existingWorkflow = await _repository.GetWorkflowByEndpointAsync(workflow.Endpoint);
            if (existingWorkflow != null && existingWorkflow.Id != workflow.Id)
            {
                throw new InvalidOperationException(
                    $"A workflow with endpoint '{workflow.Endpoint}' already exists");
            }
        }

        // Save the workflow
        var workflowId = await _repository.SaveWorkflowAsync(workflow);
        
        _logger.LogInformation("Workflow registered: {Name} ({Id})", workflow.Name, workflowId);
        
        return workflowId;
    }

    /// <summary>
    /// Submit a workflow for execution
    /// </summary>
    public async Task<string> SubmitWorkflowAsync(Workflow workflow)
    {
        _logger.LogInformation("Submitting workflow for execution: {Name}", workflow.Name);
        
        // Register the workflow if it's new
        var workflowId = await RegisterWorkflowAsync(workflow);
        
        // Create an execution
        var status = await _repository.CreateWorkflowExecutionAsync(workflowId);
        
        // Start executing the workflow in the background
        _ = Task.Run(async () =>
        {
            try
            {
                await _engine.ExecuteWorkflowAsync(workflow, status.ExecutionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing workflow: {Name} ({Id})", 
                    workflow.Name, status.ExecutionId);
            }
        });
        
        _logger.LogInformation("Workflow submitted for execution: {Name} ({Id})", 
            workflow.Name, status.ExecutionId);
        
        return status.ExecutionId;
    }

    /// <summary>
    /// Cancel a workflow execution
    /// </summary>
    public async Task<bool> CancelWorkflowAsync(string executionId)
    {
        _logger.LogInformation("Cancelling workflow execution: {Id}", executionId);
        
        var cancelled = await _engine.CancelExecutionAsync(executionId);
        
        if (cancelled)
        {
            _logger.LogInformation("Workflow execution cancelled: {Id}", executionId);
        }
        else
        {
            _logger.LogWarning("Could not cancel workflow execution: {Id}", executionId);
        }
        
        return cancelled;
    }

    /// <summary>
    /// Delete a workflow
    /// </summary>
    public async Task<bool> DeleteWorkflowAsync(string workflowId)
    {
        _logger.LogInformation("Deleting workflow: {Id}", workflowId);
        
        var deleted = await _repository.DeleteWorkflowAsync(workflowId);
        
        if (deleted)
        {
            _logger.LogInformation("Workflow deleted: {Id}", workflowId);
        }
        else
        {
            _logger.LogWarning("Could not delete workflow: {Id}", workflowId);
        }
        
        return deleted;
    }

    /// <summary>
    /// Get all registered workflows
    /// </summary>
    public Task<IEnumerable<Workflow>> GetAllWorkflowsAsync()
    {
        return _repository.GetAllWorkflowsAsync();
    }

    /// <summary>
    /// Get a workflow by ID
    /// </summary>
    public Task<Workflow?> GetWorkflowAsync(string workflowId)
    {
        return _repository.GetWorkflowAsync(workflowId);
    }

    /// <summary>
    /// Get the execution history for a workflow
    /// </summary>
    public Task<IEnumerable<WorkflowStatus>> GetWorkflowHistoryAsync(string workflowId, int limit = 20)
    {
        return _repository.GetWorkflowHistoryAsync(workflowId, limit);
    }

    /// <summary>
    /// Get the execution history for a workflow by name
    /// </summary>
    public Task<IEnumerable<WorkflowStatus>> GetWorkflowHistoryByNameAsync(string workflowName, int limit = 20)
    {
        return _repository.GetWorkflowHistoryByNameAsync(workflowName, limit);
    }

    /// <summary>
    /// Get the status of a workflow execution
    /// </summary>
    public Task<WorkflowStatus?> GetWorkflowStatusAsync(string executionId)
    {
        return _repository.GetWorkflowStatusAsync(executionId);
    }

    /// <summary>
    /// Trigger a workflow by endpoint
    /// </summary>
    public async Task<string?> TriggerWorkflowByEndpointAsync(string endpoint)
    {
        _logger.LogInformation("Triggering workflow by endpoint: {Endpoint}", endpoint);
        
        // Find the workflow by endpoint
        var workflow = await _repository.GetWorkflowByEndpointAsync(endpoint);
        if (workflow == null)
        {
            _logger.LogWarning("No workflow found for endpoint: {Endpoint}", endpoint);
            return null;
        }
        
        // Create an execution
        var status = await _repository.CreateWorkflowExecutionAsync(workflow.Id);
        
        // Start executing the workflow in the background
        _ = Task.Run(async () =>
        {
            try
            {
                await _engine.ExecuteWorkflowAsync(workflow, status.ExecutionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing workflow: {Name} ({Id})", 
                    workflow.Name, status.ExecutionId);
            }
        });
        
        _logger.LogInformation("Workflow triggered by endpoint: {Endpoint} ({Id})", 
            endpoint, status.ExecutionId);
        
        return status.ExecutionId;
    }
}
