using TS4Tools.Core.Interfaces;

namespace TS4Tools.Core.Helpers;

/// <summary>
/// Service for discovering and executing external helper tools
/// </summary>
public interface IHelperToolService
{
    /// <summary>
    /// Executes a helper tool with the specified arguments
    /// </summary>
    /// <param name="helperName">Name of the helper tool</param>
    /// <param name="args">Arguments to pass to the helper tool</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the helper tool execution</returns>
    Task<HelperToolResult> ExecuteAsync(string helperName, string[] args, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a helper tool for a specific resource
    /// </summary>
    /// <param name="helperName">Name of the helper tool</param>
    /// <param name="resource">Resource to process</param>
    /// <param name="args">Additional arguments</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the helper tool execution</returns>
    Task<HelperToolResult> ExecuteForResourceAsync(string helperName, IResource resource, string[] args, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a helper tool is available
    /// </summary>
    /// <param name="helperName">Name of the helper tool</param>
    /// <returns>True if the helper tool is available</returns>
    bool IsHelperToolAvailable(string helperName);

    /// <summary>
    /// Gets all available helper tools
    /// </summary>
    /// <returns>List of available helper tool names</returns>
    IReadOnlyList<string> GetAvailableHelperTools();

    /// <summary>
    /// Gets all helper tools that support a specific resource type
    /// </summary>
    /// <param name="resourceType">Resource type to check</param>
    /// <returns>List of compatible helper tools</returns>
    IReadOnlyList<HelperToolInfo> GetHelpersForResourceType(uint resourceType);

    /// <summary>
    /// Reloads helper tool configuration
    /// </summary>
    /// <returns>Task representing the reload operation</returns>
    Task ReloadHelpersAsync();
}
