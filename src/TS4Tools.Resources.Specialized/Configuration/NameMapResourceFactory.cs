using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TS4Tools.Core.Resources;
using TS4Tools.Core.Interfaces.Resources;

namespace TS4Tools.Resources.Specialized.Configuration
{
    /// <summary>
    /// Factory for creating NameMapResource instances.
    /// Provides methods to create name map resources from streams or create new name map resources.
    /// </summary>
    public class NameMapResourceFactory : ResourceFactoryBase<INameMapResource>
    {
        /// <summary>
        /// Initializes a new instance of the NameMapResourceFactory class.
        /// </summary>
        public NameMapResourceFactory() : base(new[] { "NameMapResource" }, 1)
        {
        }

        /// <inheritdoc />
        public override async Task<INameMapResource> CreateResourceAsync(int apiVersion, Stream? stream = null, CancellationToken cancellationToken = default)
        {
            var nameMapResource = new NameMapResource();

            if (stream != null)
            {
                await LoadFromStreamAsync(nameMapResource, stream, cancellationToken);
            }

            return nameMapResource;
        }

        /// <summary>
        /// Creates a new name map resource with specified configuration.
        /// </summary>
        /// <param name="nameMapId">Name map identifier.</param>
        /// <param name="version">Version.</param>
        /// <param name="category">Category.</param>
        /// <param name="isCaseSensitive">Whether names are case-sensitive.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>New name map resource.</returns>
        public async Task<INameMapResource> CreateNameMapAsync(
            string nameMapId,
            string version,
            string category,
            bool isCaseSensitive,
            CancellationToken cancellationToken = default)
        {
            var nameMapResource = new NameMapResource(nameMapId, version, category, isCaseSensitive);
            await Task.CompletedTask;
            return nameMapResource;
        }

        /// <summary>
        /// Creates a name map resource with initial mappings.
        /// </summary>
        /// <param name="nameMapId">Name map identifier.</param>
        /// <param name="version">Version.</param>
        /// <param name="category">Category.</param>
        /// <param name="isCaseSensitive">Whether names are case-sensitive.</param>
        /// <param name="initialMappings">Initial name-to-ID mappings.</param>
        /// <param name="metadata">Initial metadata.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>New name map resource with data.</returns>
        public async Task<INameMapResource> CreateWithMappingsAsync(
            string nameMapId,
            string version,
            string category,
            bool isCaseSensitive,
            IDictionary<string, uint> initialMappings,
            IDictionary<string, object>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            var nameMapResource = new NameMapResource(nameMapId, version, category, isCaseSensitive);

            // Add initial mappings
            if (initialMappings.Count > 0)
            {
                await nameMapResource.AddMappingsBatchAsync(initialMappings, cancellationToken);
            }

            // Set metadata
            if (metadata != null && metadata.Count > 0)
            {
                await nameMapResource.UpdateMetadataAsync(metadata, cancellationToken);
            }

            return nameMapResource;
        }

        private static async Task LoadFromStreamAsync(
            NameMapResource nameMapResource,
            Stream stream,
            CancellationToken cancellationToken)
        {
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

            // Read header
            var nameMapId = reader.ReadString();
            var version = reader.ReadString();
            var category = reader.ReadString();
            var isCaseSensitive = reader.ReadBoolean();

            // Set properties using reflection
            SetNameMapProperties(nameMapResource, nameMapId, version, category, isCaseSensitive);

            // Read mappings
            var mappingCount = reader.ReadInt32();
            var mappings = new Dictionary<string, uint>();
            for (int i = 0; i < mappingCount; i++)
            {
                var name = reader.ReadString();
                var id = reader.ReadUInt32();
                mappings[name] = id;
            }

            // Add mappings to name map
            if (mappings.Count > 0)
            {
                await nameMapResource.AddMappingsBatchAsync(mappings, cancellationToken);
            }

            // Read metadata
            var metadataCount = reader.ReadInt32();
            var metadata = new Dictionary<string, object>();
            for (int i = 0; i < metadataCount; i++)
            {
                var key = reader.ReadString();
                var value = ReadValue(reader);
                metadata[key] = value;
            }

            // Set metadata
            if (metadata.Count > 0)
            {
                await nameMapResource.UpdateMetadataAsync(metadata, cancellationToken);
            }
        }

        private static void SetNameMapProperties(
            NameMapResource nameMapResource,
            string nameMapId,
            string version,
            string category,
            bool isCaseSensitive)
        {
            // Use reflection to set private properties
            var type = typeof(NameMapResource);

            var nameMapIdProperty = type.GetProperty(nameof(INameMapResource.NameMapId));
            nameMapIdProperty?.SetValue(nameMapResource, nameMapId);

            var versionProperty = type.GetProperty(nameof(INameMapResource.Version));
            versionProperty?.SetValue(nameMapResource, version);

            var categoryProperty = type.GetProperty(nameof(INameMapResource.Category));
            categoryProperty?.SetValue(nameMapResource, category);

            var isCaseSensitiveProperty = type.GetProperty(nameof(INameMapResource.IsCaseSensitive));
            isCaseSensitiveProperty?.SetValue(nameMapResource, isCaseSensitive);
        }

        private static object ReadValue(BinaryReader reader)
        {
            var valueType = reader.ReadByte();
            return valueType switch
            {
                1 => reader.ReadString(),
                2 => reader.ReadInt32(),
                3 => reader.ReadUInt32(),
                4 => reader.ReadBoolean(),
                5 => reader.ReadSingle(),
                6 => reader.ReadDouble(),
                _ => reader.ReadString()
            };
        }
    }
}
