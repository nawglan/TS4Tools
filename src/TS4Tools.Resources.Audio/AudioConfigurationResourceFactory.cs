using TS4Tools.Core.Interfaces;
using TS4Tools.Core.Package;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Audio;

/// <summary>
/// Factory for creating AudioConfigurationResource instances.
/// Handles Audio Configuration resource type 0xFD04E3BE for voice acting and sound effect configurations.
/// </summary>
public sealed class AudioConfigurationResourceFactory : ResourceFactoryBase<AudioConfigurationResource>
{
    /// <summary>
    /// The resource type ID for Audio Configuration resources.
    /// </summary>
    public const uint ResourceType = 0xFD04E3BE;

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioConfigurationResourceFactory"/> class.
    /// </summary>
    public AudioConfigurationResourceFactory() : base(new[] { "0xFD04E3BE" }, priority: 50)
    {
    }

    /// <summary>
    /// Creates an AudioConfigurationResource from a stream.
    /// </summary>
    /// <param name="apiVersion">The API version.</param>
    /// <param name="stream">The stream containing the resource data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation and contains the created resource.</returns>
    public override async Task<AudioConfigurationResource> CreateResourceAsync(
        int apiVersion,
        Stream? stream = null,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // For async compliance

        // Create a basic Audio Configuration resource
        var key = new ResourceKey(ResourceType, 0x00000000, 0x0000000000000000);
        byte[] data;

        if (stream != null)
        {
            data = new byte[stream.Length];
            await stream.ReadExactlyAsync(data, cancellationToken);
        }
        else
        {
            // Create empty resource
            data = Array.Empty<byte>();
        }

        // Try to extract audio effect name from the data or use a default
        var audioEffectName = TryExtractAudioEffectName(key);

        return new AudioConfigurationResource(key, data, audioEffectName);
    }

    /// <summary>
    /// Attempts to extract the audio effect name from the resource key's instance ID.
    /// This is used to identify voice acting (vo) resources and their effect names.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <returns>The extracted audio effect name if available; otherwise, an empty string.</returns>
    private static string TryExtractAudioEffectName(ResourceKey key)
    {
        try
        {
            // The instance ID often contains a hash of the audio effect name
            // Voice acting files typically start with "vo" followed by details
            var instanceId = key.Instance;
            
            // Check if this might be a voice acting resource based on common patterns
            // Voice acting instance IDs often have specific bit patterns
            if ((instanceId & 0xFF00000000000000UL) == 0x5600000000000000UL ||
                (instanceId & 0xFF00000000000000UL) == 0x2100000000000000UL)
            {
                return "vo_unknown"; // Placeholder for voice acting
            }

            // For now, return empty string since we can't easily reverse the hash
            // In a full implementation, you might maintain a lookup table of known hashes
            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Validates that the provided data is valid for an Audio Configuration resource.
    /// </summary>
    /// <param name="data">The data to validate.</param>
    /// <returns>True if the data is valid; otherwise, false.</returns>
    public static bool ValidateAudioConfigurationData(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            return false;
        }

        if (data.Length < 4)
        {
            return false;
        }

        try
        {
            using var reader = new BinaryReader(new MemoryStream(data));
            var version = reader.ReadUInt32();

            // Check for reasonable version values
            if (version == 0 || version > 10)
            {
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
