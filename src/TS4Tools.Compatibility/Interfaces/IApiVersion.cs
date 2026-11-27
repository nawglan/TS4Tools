// Source: legacy_references/Sims4Tools/s4pi/Interfaces/IApiVersion.cs lines 22-39

namespace s4pi.Interfaces;

/// <summary>
/// Support for API versioning.
/// </summary>
public interface IApiVersion
{
    /// <summary>
    /// The version of the API in use.
    /// </summary>
    int RequestedApiVersion { get; }

    /// <summary>
    /// The best supported version of the API available.
    /// </summary>
    int RecommendedApiVersion { get; }
}
