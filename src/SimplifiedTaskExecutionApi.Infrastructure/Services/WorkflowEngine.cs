
using Microsoft.Extensions.Logging;
using SimplifiedTaskExecutionApi.Core.Models;
using SimplifiedTaskExecutionApi.Core.Repositories;
using SimplifiedTaskExecutionApi.Core.Services;
using SimplifiedTaskExecutionApi.Core.Validators;
using SimplifiedTaskExecutionApi.Infrastructure.Executors;

namespace SimplifiedTaskExecutionApi.Infrastructure.Services;

/// <summary>
/// Implementation of the workflow execution engine
/// </summary>
public class WorkflowEngine : IWorkflowEngine, IStepVisitor
{
    private readonly IWorkflowRepository _repository;
    private readonly HttpTaskExecutor _httpExecutor;
    private readonly ShellTaskExecutor _shellExecutor;
    private readonly WorkflowValidator _validator;
    private readonly ILogger<WorkflowEngine> _logger;
    private readonly Dictionary<string, CancellationTokenSource> _cancellationSources = new();

    /// <summary>
    /// Constructor with dependencies
    /// </summary>
    public WorkflowEngine(
        IWorkflowRepository repository,
        HttpTaskExecutor httpExecutor,
        ShellTaskExecutor shellExecutor,
        WorkflowValidator validator,
        ILogger<WorkflowEngine> logger)
    {
        _repository = repository;
        _httpExecutor = httpExecutor;
        _shellExecutor = shellExecutor;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// Execute a workflow
    /// </summary>
    public async Task<WorkflowStatus> ExecuteWorkflowAsync(
        Workflow workflow, 
        string? executionId = null, 
        CancellationToken cancellationToken = default)
    {
        // Validate the workflow
        var validationErrors = await ValidateWorkflowAsync(workflow);
        if (validationErrors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Workflow validation failed: {string.Join(", ", validationErrors)}");
        }

        // Save the workflow if it's new
        var existingWorkflow = await _repository.GetWorkflowAsync(workflow.Id);
        if (existingWorkflow == null)
        {
            await _repository.SaveWorkflowAsync(workflow);
        }

        // Create or get the execution status
        WorkflowStatus status;
        if (string.IsNullOrEmpty(executionId))
        {
            status = await _repository.CreateWorkflowExecutionAsync(workflow.Id);
            executionId = status.ExecutionId;
        }
        else
        {
            var existingStatus = await _repository.GetWorkflowStatusAsync(executionId);
            if (existingStatus == null)
            {
                throw new KeyNotFoundException($"Execution with ID {executionId} not found");
            }
            status = existingStatus;
        }

        // Setup cancellation
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (workflow.Timeout.HasValue)
        {
            cts.CancelAfter(workflow.Timeout.Value);
        }

        _cancellationSources[executionId] = cts;

        try
        {
            // Update status to running
            status.State = ExecutionState.Running;
            status.StartTime = DateTime.UtcNow;
            await _repository.UpdateWorkflowStatusAsync(status);

            _logger.LogInformation("Starting workflow execution: {Name} ({Id})", 
                workflow.Name, executionId);

            // Process the steps
            foreach (var step in workflow.Steps)
            {
                step.Workflow = workflow;
                var stepStatus = await ProcessStepAsync(step, status, cts.Token);
                status.Steps.Add(stepStatus);

                // Check if we should stop execution (failed or cancelled)
                if (stepStatus.State == ExecutionState.Failed || 
                    stepStatus.State == ExecutionState.Cancelled)
                {
                    status.State = stepStatus.State;
                    status.ErrorMessage = stepStatus.ErrorMessage;
                    status.EndTime = DateTime.UtcNow;
                    await _repository.UpdateWorkflowStatusAsync(status);
                    return status;
                }
            }

            // All steps completed successfully
            status.State = ExecutionState.Completed;
            status.EndTime = DateTime.UtcNow;
            await _repository.UpdateWorkflowStatusAsync(status);

            _logger.LogInformation("Workflow execution completed: {Name} ({Id})", 
                workflow.Name, executionId);

            return status;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Workflow execution cancelled: {Name} ({Id})", 
                workflow.Name, executionId);
            
            status.State = ExecutionState.Cancelled;
            status.EndTime = DateTime.UtcNow;
            await _repository.UpdateWorkflowStatusAsync(status);
            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing workflow: {Name} ({Id})", 
                workflow.Name, executionId);
            
            status.State = ExecutionState.Failed;
            status.ErrorMessage = ex.Message;
            status.EndTime = DateTime.UtcNow;
            await _repository.UpdateWorkflowStatusAsync(status);
            return status;
        }
        finally
        {
            _cancellationSources.Remove(executionId);
        }
    }

    /// <summary>
    /// Process a workflow step
    /// </summary>
    public async Task<StepStatus> ProcessStepAsync(
        WorkflowStep step, 
        object? executionContext = null, 
        CancellationToken cancellationToken = default)
    {
        var stepStatus = new StepStatus
        {
            StepId = step.Id,
            StepName = step.Name,
            StepType = step.Type,
            State = ExecutionState.Running,
            StartTime = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Processing step: {Name} ({Type})", step.Name, step.Type);
            
            // Use visitor pattern to process the step
            await step.AcceptAsync(this, executionContext);
            
            stepStatus.State = ExecutionState.Completed;
            stepStatus.EndTime = DateTime.UtcNow;

            _logger.LogInformation("Step completed: {Name} ({Type})", step.Name, step.Type);
            
            return stepStatus;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Step cancelled: {Name} ({Type})", step.Name, step.Type);
            
            stepStatus.State = ExecutionState.Cancelled;
            stepStatus.EndTime = DateTime.UtcNow;
            return stepStatus;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing step: {Name} ({Type})", step.Name, step.Type);
            
            stepStatus.State = ExecutionState.Failed;
            stepStatus.ErrorMessage = ex.Message;
            stepStatus.EndTime = DateTime.UtcNow;
            return stepStatus;
        }
    }

    /// <summary>
    /// Cancel a workflow execution
    /// </summary>
    public async Task<bool> CancelExecutionAsync(string executionId)
    {
        var status = await _repository.GetWorkflowStatusAsync(executionId);
        if (status == null)
        {
            return false;
        }

        if (status.State != ExecutionState.Running && status.State != ExecutionState.Pending)
        {
            return false; // Already completed or cancelled
        }

        if (_cancellationSources.TryGetValue(executionId, out var cts))
        {
            try
            {
                cts.Cancel();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling workflow: {Id}", executionId);
            }
        }

        status.State = ExecutionState.Cancelled;
        status.EndTime = DateTime.UtcNow;
        await _repository.UpdateWorkflowStatusAsync(status);

        _logger.LogInformation("Workflow cancelled: {Id}", executionId);
        
        return true;
    }

    /// <summary>
    /// Process any pending workflows
    /// </summary>
    public async Task<int> ProcessPendingWorkflowsAsync(CancellationToken cancellationToken = default)
    {
        // This would typically be called by a background service
        // For now, we'll return 0 as we're not implementing this in this simplified version
        return 0;
    }

    /// <summary>
    /// Validate a workflow definition
    /// </summary>
    public Task<List<string>> ValidateWorkflowAsync(Workflow workflow)
    {
        var validationResult = _validator.Validate(workflow);
        var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();

        // Check for circular dependencies
        if (_validator.HasCircularDependencies(workflow))
        {
            errors.Add("Workflow contains circular dependencies");
        }

        return Task.FromResult(errors);
    }

    #region IStepVisitor implementation

    /// <summary>
    /// Process a step of type "Process"
    /// </summary>
    public async Task VisitProcessStepAsync(WorkflowStep step, object? context = null)
    {
        var cancellationToken = CancellationToken.None;
        if (context is WorkflowStatus status && 
            _cancellationSources.TryGetValue(status.ExecutionId, out var cts))
        {
            cancellationToken = cts.Token;
        }

        var result = await _httpExecutor.ExecuteAsync(step, cancellationToken);
        
        if (context is StepStatus stepStatus)
        {
            stepStatus.Result = result;
        }
    }

    /// <summary>
    /// Process a step of type "Parallel"
    /// </summary>
    public async Task VisitParallelStepAsync(WorkflowStep step, object? context = null)
    {
        if (step.Steps == null || step.Steps.Count == 0)
        {
            return;
        }

        var cancellationToken = CancellationToken.None;
        if (context is WorkflowStatus status && 
            _cancellationSources.TryGetValue(status.ExecutionId, out var cts))
        {
            cancellationToken = cts.Token;
        }

        var stepStatus = new StepStatus
        {
            StepId = step.Id,
            StepName = step.Name,
            StepType = step.Type,
            ChildSteps = new List<StepStatus>()
        };

        if (context is StepStatus parentStepStatus)
        {
            parentStepStatus.ChildSteps ??= new List<StepStatus>();
            parentStepStatus.ChildSteps.Add(stepStatus);
        }

        // Execute child steps in parallel
        var tasks = step.Steps.Select(s => 
            ProcessStepAsync(s, stepStatus, cancellationToken)).ToList();

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Process a step of type "Serial"
    /// </summary>
    public async Task VisitSerialStepAsync(WorkflowStep step, object? context = null)
    {
        if (step.Steps == null || step.Steps.Count == 0)
        {
            return;
        }

        var cancellationToken = CancellationToken.None;
        if (context is WorkflowStatus status && 
            _cancellationSources.TryGetValue(status.ExecutionId, out var cts))
        {
            cancellationToken = cts.Token;
        }

        var stepStatus = new StepStatus
        {
            StepId = step.Id,
            StepName = step.Name,
            StepType = step.Type,
            ChildSteps = new List<StepStatus>()
        };

        if (context is StepStatus parentStepStatus)
        {
            parentStepStatus.ChildSteps ??= new List<StepStatus>();
            parentStepStatus.ChildSteps.Add(stepStatus);
        }

        // Execute child steps in series
        foreach (var childStep in step.Steps)
        {
            var childStatus = await ProcessStepAsync(childStep, stepStatus, cancellationToken);
            
            // If a step fails, stop execution of subsequent steps
            if (childStatus.State == ExecutionState.Failed || 
                childStatus.State == ExecutionState.Cancelled)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Process a step of type "Batch"
    /// </summary>
    public async Task VisitBatchStepAsync(WorkflowStep step, object? context = null)
    {
        var cancellationToken = CancellationToken.None;
        if (context is WorkflowStatus status && 
            _cancellationSources.TryGetValue(status.ExecutionId, out var cts))
        {
            cancellationToken = cts.Token;
        }

        var result = await _shellExecutor.ExecuteAsync(step, cancellationToken);
        
        if (context is StepStatus stepStatus)
        {
            stepStatus.Result = result;
        }
    }

    /// <summary>
    /// Process a step of type "Executable"
    /// </summary>
    public async Task VisitExecutableStepAsync(WorkflowStep step, object? context = null)
    {
        var cancellationToken = CancellationToken.None;
        if (context is WorkflowStatus status && 
            _cancellationSources.TryGetValue(status.ExecutionId, out var cts))
        {
            cancellationToken = cts.Token;
        }

        var result = await _shellExecutor.ExecuteAsync(step, cancellationToken);
        
        if (context is StepStatus stepStatus)
        {
            stepStatus.Result = result;
        }
    }

    /// <summary>
    /// Process a step of an unknown type
    /// </summary>
    public Task VisitUnknownStepAsync(WorkflowStep step, object? context = null)
    {
        throw new NotSupportedException($"Unsupported step type: {step.Type}");
    }

    #endregion
}
