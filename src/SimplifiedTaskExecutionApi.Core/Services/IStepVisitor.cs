
using SimplifiedTaskExecutionApi.Core.Models;

namespace SimplifiedTaskExecutionApi.Core.Services;

/// <summary>
/// Interface for the visitor pattern to process different step types
/// </summary>
public interface IStepVisitor
{
    /// <summary>
    /// Process a step of type "Process"
    /// </summary>
    /// <param name="step">The step to process</param>
    /// <param name="context">Optional execution context</param>
    Task VisitProcessStepAsync(WorkflowStep step, object? context = null);
    
    /// <summary>
    /// Process a step of type "Parallel"
    /// </summary>
    /// <param name="step">The step to process</param>
    /// <param name="context">Optional execution context</param>
    Task VisitParallelStepAsync(WorkflowStep step, object? context = null);
    
    /// <summary>
    /// Process a step of type "Serial"
    /// </summary>
    /// <param name="step">The step to process</param>
    /// <param name="context">Optional execution context</param>
    Task VisitSerialStepAsync(WorkflowStep step, object? context = null);
    
    /// <summary>
    /// Process a step of type "Batch"
    /// </summary>
    /// <param name="step">The step to process</param>
    /// <param name="context">Optional execution context</param>
    Task VisitBatchStepAsync(WorkflowStep step, object? context = null);
    
    /// <summary>
    /// Process a step of type "Executable"
    /// </summary>
    /// <param name="step">The step to process</param>
    /// <param name="context">Optional execution context</param>
    Task VisitExecutableStepAsync(WorkflowStep step, object? context = null);
    
    /// <summary>
    /// Process a step of an unknown type
    /// </summary>
    /// <param name="step">The step to process</param>
    /// <param name="context">Optional execution context</param>
    Task VisitUnknownStepAsync(WorkflowStep step, object? context = null);
}
