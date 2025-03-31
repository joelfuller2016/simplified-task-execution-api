
using FluentValidation;
using SimplifiedTaskExecutionApi.Core.Models;

namespace SimplifiedTaskExecutionApi.Core.Validators;

/// <summary>
/// Validator for workflow definitions
/// </summary>
public class WorkflowValidator : AbstractValidator<Workflow>
{
    /// <summary>
    /// Constructor with validation rules
    /// </summary>
    public WorkflowValidator()
    {
        RuleFor(w => w.Name)
            .NotEmpty().WithMessage("Workflow name is required")
            .MaximumLength(100).WithMessage("Workflow name cannot exceed 100 characters");

        RuleFor(w => w.Type)
            .NotEmpty().WithMessage("Workflow type is required")
            .Must(type => type.Equals("FactotumTrigger", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Workflow type must be 'FactotumTrigger'");

        RuleFor(w => w.Parameters)
            .NotNull().WithMessage("Parameters must not be null");

        When(w => w.Parameters != null, () =>
        {
            RuleFor(w => w.Parameters)
                .Must(HaveEndpointParameter)
                .WithMessage("Parameters must include an 'Endpoint' property with a string value");
        });

        RuleFor(w => w.Steps)
            .NotEmpty().WithMessage("Workflow must contain at least one step");

        RuleForEach(w => w.Steps)
            .SetValidator(new WorkflowStepValidator());
    }

    /// <summary>
    /// Check if the workflow parameters has an endpoint
    /// </summary>
    private bool HaveEndpointParameter(Dictionary<string, object> parameters)
    {
        return parameters.ContainsKey("Endpoint") && parameters["Endpoint"] is string endpoint && !string.IsNullOrWhiteSpace(endpoint);
    }

    /// <summary>
    /// Check if workflow has circular dependencies
    /// </summary>
    /// <param name="workflow">The workflow to check</param>
    /// <returns>True if there are circular dependencies</returns>
    public bool HasCircularDependencies(Workflow workflow)
    {
        // Implementation for checking circular dependencies
        // This would be a more complex graph traversal function
        // For simplified implementation, we'll assume no circular dependencies for now
        return false;
    }
}

/// <summary>
/// Validator for workflow steps
/// </summary>
public class WorkflowStepValidator : AbstractValidator<WorkflowStep>
{
    /// <summary>
    /// Constructor with validation rules
    /// </summary>
    public WorkflowStepValidator()
    {
        RuleFor(s => s.Name)
            .NotEmpty().WithMessage("Step name is required")
            .MaximumLength(100).WithMessage("Step name cannot exceed 100 characters");

        RuleFor(s => s.Type)
            .NotEmpty().WithMessage("Step type is required")
            .Must(BeValidStepType).WithMessage("Step type must be one of: 'Process', 'Parallel', 'Serial', 'Batch', or 'Executable'");

        RuleFor(s => s.Parameters)
            .NotNull().WithMessage("Parameters must not be null");

        // Additional validators based on step type
        When(s => s.Type.Equals("Process", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(s => s.Parameters)
                .Must(HaveEndpointParameter)
                .WithMessage("Process step must have an 'Endpoint' parameter");
        });

        When(s => s.Type.Equals("Parallel", StringComparison.OrdinalIgnoreCase) || 
                  s.Type.Equals("Serial", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(s => s.Steps)
                .NotNull().WithMessage("Container step must have child steps")
                .NotEmpty().WithMessage("Container step must have at least one child step");

            RuleForEach(s => s.Steps)
                .SetValidator(this);
        });

        When(s => s.Type.Equals("Batch", StringComparison.OrdinalIgnoreCase) || 
                  s.Type.Equals("Executable", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(s => s.Parameters)
                .Must(HaveCommandParameter)
                .WithMessage("Batch/Executable step must have a 'Command' parameter");
        });
    }

    /// <summary>
    /// Check if the step type is valid
    /// </summary>
    private bool BeValidStepType(string type)
    {
        var validTypes = new[] { "Process", "Parallel", "Serial", "Batch", "Executable" };
        return validTypes.Any(t => t.Equals(type, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Check if the parameters has an endpoint
    /// </summary>
    private bool HaveEndpointParameter(Dictionary<string, object> parameters)
    {
        return parameters.ContainsKey("Endpoint") && parameters["Endpoint"] is string endpoint && !string.IsNullOrWhiteSpace(endpoint);
    }

    /// <summary>
    /// Check if the parameters has a command
    /// </summary>
    private bool HaveCommandParameter(Dictionary<string, object> parameters)
    {
        return parameters.ContainsKey("Command") && parameters["Command"] is string command && !string.IsNullOrWhiteSpace(command);
    }
}
