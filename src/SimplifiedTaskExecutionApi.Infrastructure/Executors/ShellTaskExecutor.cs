
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SimplifiedTaskExecutionApi.Core.Models;

namespace SimplifiedTaskExecutionApi.Infrastructure.Executors;

/// <summary>
/// Executor for Shell tasks (Batch and Executable step types)
/// </summary>
public class ShellTaskExecutor
{
    private readonly ILogger<ShellTaskExecutor> _logger;

    /// <summary>
    /// Constructor with dependencies
    /// </summary>
    public ShellTaskExecutor(ILogger<ShellTaskExecutor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Execute the shell task
    /// </summary>
    /// <param name="step">The step to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the shell command</returns>
    public async Task<object> ExecuteAsync(WorkflowStep step, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get command from parameters
            if (!step.Parameters.TryGetValue("Command", out var cmdObj) || cmdObj is not string command)
            {
                throw new InvalidOperationException("Shell task requires a Command parameter");
            }

            // Get arguments if provided
            var arguments = string.Empty;
            if (step.Parameters.TryGetValue("Arguments", out var argsObj) && argsObj is string args)
            {
                arguments = args;
            }

            // Get working directory if provided
            var workingDirectory = Environment.CurrentDirectory;
            if (step.Parameters.TryGetValue("WorkingDirectory", out var dirObj) && dirObj is string dir)
            {
                workingDirectory = dir;
            }

            // Get timeout
            var timeout = step.GetTimeout() ?? 60; // Default to 60 seconds

            _logger.LogInformation("Executing shell command: {Command} {Arguments} in directory {Directory}", 
                command, arguments, workingDirectory);

            // Prepare the process
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            var outputBuilder = new System.Text.StringBuilder();
            var errorBuilder = new System.Text.StringBuilder();

            process.OutputDataReceived += (_, e) => 
            {
                if (e.Data != null)
                {
                    outputBuilder.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (_, e) => 
            {
                if (e.Data != null)
                {
                    errorBuilder.AppendLine(e.Data);
                }
            };

            // Start the process
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait for completion with timeout
            var completedInTime = await WaitForProcessWithTimeoutAsync(
                process, TimeSpan.FromSeconds(timeout), cancellationToken);

            if (!completedInTime)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(true);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error killing process after timeout");
                }

                throw new TimeoutException(
                    $"Shell task timed out after {timeout} seconds: {command} {arguments}");
            }

            // Process completed within timeout
            var output = outputBuilder.ToString();
            var error = errorBuilder.ToString();
            var exitCode = process.ExitCode;

            _logger.LogInformation("Shell command completed with exit code: {ExitCode}", exitCode);

            if (exitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Shell command failed with exit code {exitCode}: {error}");
            }

            return new
            {
                ExitCode = exitCode,
                Output = output,
                Error = error
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing shell task: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Wait for process to complete with timeout
    /// </summary>
    private static Task<bool> WaitForProcessWithTimeoutAsync(Process process, TimeSpan timeout, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            try
            {
                using var timeoutCts = new CancellationTokenSource(timeout);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    timeoutCts.Token, cancellationToken);

                try
                {
                    return process.WaitForExit((int)timeout.TotalMilliseconds);
                }
                catch (OperationCanceledException)
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }, cancellationToken);
    }
}
