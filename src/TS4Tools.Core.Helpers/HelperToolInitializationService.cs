using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TS4Tools.Core.Helpers;

/// <summary>
/// Background service that initializes helper tools on application startup
/// </summary>
public class HelperToolInitializationService : BackgroundService
{
    private readonly IHelperToolService _helperToolService;
    private readonly ILogger<HelperToolInitializationService> _logger;

    /// <summary>
    /// Initializes a new instance of the HelperToolInitializationService class
    /// </summary>
    /// <param name="helperToolService">Helper tool service instance</param>
    /// <param name="logger">Logger instance</param>
    public HelperToolInitializationService(
        IHelperToolService helperToolService,
        ILogger<HelperToolInitializationService> logger)
    {
        _helperToolService = helperToolService ?? throw new ArgumentNullException(nameof(helperToolService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the background service task
    /// </summary>
    /// <param name="stoppingToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Initializing helper tools...");
            await _helperToolService.ReloadHelpersAsync();

            var availableHelpers = _helperToolService.GetAvailableHelperTools();
            _logger.LogInformation("Helper tool initialization completed. Available tools: {Count}", availableHelpers.Count);

            if (availableHelpers.Count > 0)
            {
                _logger.LogDebug("Available helper tools: {Helpers}", string.Join(", ", availableHelpers));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize helper tools");
        }
    }
}
