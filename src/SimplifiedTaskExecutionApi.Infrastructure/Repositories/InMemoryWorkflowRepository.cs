
using SimplifiedTaskExecutionApi.Core.Models;
using SimplifiedTaskExecutionApi.Core.Repositories;

namespace SimplifiedTaskExecutionApi.Infrastructure.Repositories;

/// <summary>
/// In-memory implementation of the workflow repository
/// </summary>
public class InMemoryWorkflowRepository : IWorkflowRepository
{
    private readonly Dictionary<string, Workflow> _workflows = new();
    private readonly Dictionary<string, WorkflowStatus> _statuses = new();
    private readonly Dictionary<string, List<WorkflowStatus>> _history = new();
    private readonly Dictionary<string, string> _endpointToWorkflowMapping = new();
    private readonly object _lock = new();

    /// <summary>
    /// Create a new workflow execution
    /// </summary>
    public Task<WorkflowStatus> CreateWorkflowExecutionAsync(string workflowId)
    {
        lock (_lock)
        {
            if (!_workflows.TryGetValue(workflowId, out var workflow))
            {
                throw new KeyNotFoundException($"Workflow with ID {workflowId} not found");
            }

            var executionId = Guid.NewGuid().ToString();
            var status = new WorkflowStatus
            {
                ExecutionId = executionId,
                WorkflowId = workflowId,
                WorkflowName = workflow.Name,
                State = ExecutionState.Pending,
                StartTime = DateTime.UtcNow
            };

            _statuses[executionId] = status;

            if (!_history.TryGetValue(workflowId, out var history))
            {
                history = new List<WorkflowStatus>();
                _history[workflowId] = history;
            }

            history.Add(status);

            return Task.FromResult(status);
        }
    }

    /// <summary>
    /// Delete a workflow
    /// </summary>
    public Task<bool> DeleteWorkflowAsync(string id)
    {
        lock (_lock)
        {
            if (!_workflows.TryGetValue(id, out var workflow))
            {
                return Task.FromResult(false);
            }

            _workflows.Remove(id);

            if (workflow.Endpoint != null)
            {
                _endpointToWorkflowMapping.Remove(workflow.Endpoint);
            }

            return Task.FromResult(true);
        }
    }

    /// <summary>
    /// Get all workflows
    /// </summary>
    public Task<IEnumerable<Workflow>> GetAllWorkflowsAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_workflows.Values.AsEnumerable());
        }
    }

    /// <summary>
    /// Get a workflow by ID
    /// </summary>
    public Task<Workflow?> GetWorkflowAsync(string id)
    {
        lock (_lock)
        {
            return _workflows.TryGetValue(id, out var workflow)
                ? Task.FromResult<Workflow?>(workflow)
                : Task.FromResult<Workflow?>(null);
        }
    }

    /// <summary>
    /// Get a workflow by endpoint
    /// </summary>
    public Task<Workflow?> GetWorkflowByEndpointAsync(string endpoint)
    {
        lock (_lock)
        {
            if (_endpointToWorkflowMapping.TryGetValue(endpoint, out var workflowId))
            {
                return GetWorkflowAsync(workflowId);
            }

            return Task.FromResult<Workflow?>(null);
        }
    }

    /// <summary>
    /// Get execution history for a workflow
    /// </summary>
    public Task<IEnumerable<WorkflowStatus>> GetWorkflowHistoryAsync(string workflowId, int limit = 20)
    {
        lock (_lock)
        {
            if (!_history.TryGetValue(workflowId, out var history))
            {
                return Task.FromResult(Enumerable.Empty<WorkflowStatus>());
            }

            return Task.FromResult(history.OrderByDescending(h => h.StartTime).Take(limit));
        }
    }

    /// <summary>
    /// Get execution history by workflow name
    /// </summary>
    public Task<IEnumerable<WorkflowStatus>> GetWorkflowHistoryByNameAsync(string workflowName, int limit = 20)
    {
        lock (_lock)
        {
            var result = new List<WorkflowStatus>();

            foreach (var history in _history.Values)
            {
                result.AddRange(history.Where(h => h.WorkflowName == workflowName));
            }

            return Task.FromResult(result.OrderByDescending(h => h.StartTime).Take(limit).AsEnumerable());
        }
    }

    /// <summary>
    /// Get workflow status by execution ID
    /// </summary>
    public Task<WorkflowStatus?> GetWorkflowStatusAsync(string executionId)
    {
        lock (_lock)
        {
            return _statuses.TryGetValue(executionId, out var status)
                ? Task.FromResult<WorkflowStatus?>(status)
                : Task.FromResult<WorkflowStatus?>(null);
        }
    }

    /// <summary>
    /// Save a workflow definition
    /// </summary>
    public Task<string> SaveWorkflowAsync(Workflow workflow)
    {
        lock (_lock)
        {
            if (string.IsNullOrEmpty(workflow.Id))
            {
                workflow.Id = Guid.NewGuid().ToString();
            }

            workflow.UpdatedAt = DateTime.UtcNow;
            _workflows[workflow.Id] = workflow;

            if (workflow.Endpoint != null)
            {
                _endpointToWorkflowMapping[workflow.Endpoint] = workflow.Id;
            }

            return Task.FromResult(workflow.Id);
        }
    }

    /// <summary>
    /// Update workflow status
    /// </summary>
    public Task<bool> UpdateWorkflowStatusAsync(WorkflowStatus status)
    {
        lock (_lock)
        {
            if (!_statuses.ContainsKey(status.ExecutionId))
            {
                return Task.FromResult(false);
            }

            _statuses[status.ExecutionId] = status;

            // Also update in history
            if (_history.TryGetValue(status.WorkflowId, out var history))
            {
                var index = history.FindIndex(h => h.ExecutionId == status.ExecutionId);
                if (index >= 0)
                {
                    history[index] = status;
                }
            }

            return Task.FromResult(true);
        }
    }
}
