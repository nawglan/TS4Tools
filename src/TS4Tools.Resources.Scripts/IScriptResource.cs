using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;

namespace TS4Tools.Resources.Scripts;

/// <summary>
/// Interface for script resources that handle encrypted .NET assemblies in Sims 4 packages.
/// </summary>
public interface IScriptResource : IResource
{
    /// <summary>
    /// Gets or sets the resource key for this script resource.
    /// </summary>
    ResourceKey ResourceKey { get; set; }

    /// <summary>
    /// Gets or sets the version of the script resource format.
    /// </summary>
    byte Version { get; set; }

    /// <summary>
    /// Gets or sets the game version string (available in version > 1).
    /// </summary>
    string GameVersion { get; set; }

    /// <summary>
    /// Gets or sets an unknown field (typically 0x2BC4F79F).
    /// </summary>
    uint Unknown2 { get; set; }

    /// <summary>
    /// Gets or sets the MD5 checksum data (64 bytes).
    /// </summary>
    ReadOnlyMemory<byte> MD5Sum { get; set; }

    /// <summary>
    /// Gets the decrypted assembly data.
    /// </summary>
    ReadOnlyMemory<byte> AssemblyData { get; }

    /// <summary>
    /// Sets the assembly data to be encrypted and stored.
    /// </summary>
    /// <param name="assemblyData">The raw assembly bytes to store</param>
    void SetAssemblyData(ReadOnlySpan<byte> assemblyData);

    /// <summary>
    /// Gets assembly information by loading and inspecting the decrypted assembly.
    /// </summary>
    /// <returns>Assembly information including types, references, and metadata</returns>
    Task<AssemblyInfo> GetAssemblyInfoAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Information about a .NET assembly extracted from script resource.
/// </summary>
/// <param name="FullName">The full name of the assembly</param>
/// <param name="Location">The location of the assembly</param>
/// <param name="ExportedTypes">List of exported type names</param>
/// <param name="ReferencedAssemblies">List of referenced assembly names</param>
/// <param name="Properties">Dictionary of assembly properties</param>
public sealed record AssemblyInfo(
    string FullName,
    string Location,
    IReadOnlyList<string> ExportedTypes,
    IReadOnlyList<string> ReferencedAssemblies,
    IReadOnlyDictionary<string, string> Properties
);
