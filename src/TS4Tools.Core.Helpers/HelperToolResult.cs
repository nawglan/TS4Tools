namespace TS4Tools.Core.Helpers;

/// <summary>
/// Result of executing a helper tool
/// </summary>
public class HelperToolResult
{
    /// <summary>
    /// Gets whether the execution was successful
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the exit code from the helper tool
    /// </summary>
    public int ExitCode { get; init; }

    /// <summary>
    /// Gets the standard output from the helper tool
    /// </summary>
    public string StandardOutput { get; init; } = string.Empty;

    /// <summary>
    /// Gets the standard error from the helper tool
    /// </summary>
    public string StandardError { get; init; } = string.Empty;

    /// <summary>
    /// Gets any exception that occurred during execution
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Gets the result data from the helper tool (if applicable)
    /// </summary>
    public byte[]? ResultData { get; init; }

    /// <summary>
    /// Gets the duration of the execution
    /// </summary>
    public TimeSpan ExecutionTime { get; init; }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static HelperToolResult Success(int exitCode, string standardOutput, string standardError, TimeSpan executionTime, byte[]? resultData = null) =>
        new()
        {
            IsSuccess = true,
            ExitCode = exitCode,
            StandardOutput = standardOutput,
            StandardError = standardError,
            ExecutionTime = executionTime,
            ResultData = resultData
        };

    /// <summary>
    /// Creates a failure result
    /// </summary>
    public static HelperToolResult Failure(int exitCode, string standardOutput, string standardError, TimeSpan executionTime, Exception? exception = null) =>
        new()
        {
            IsSuccess = false,
            ExitCode = exitCode,
            StandardOutput = standardOutput,
            StandardError = standardError,
            ExecutionTime = executionTime,
            Exception = exception
        };

    /// <summary>
    /// Creates a failure result from an exception
    /// </summary>
    public static HelperToolResult FromException(Exception exception, TimeSpan executionTime) =>
        new()
        {
            IsSuccess = false,
            ExitCode = -1,
            StandardOutput = string.Empty,
            StandardError = exception.Message,
            ExecutionTime = executionTime,
            Exception = exception
        };
}
