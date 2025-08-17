using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Specialized.Geometry.Factories
{
    /// <summary>
    /// Factory for creating and loading BlendGeometryResource instances.
    /// Handles creation from various sources and format detection.
    /// </summary>
    public class BlendGeometryResourceFactory : ResourceFactoryBase<IBlendGeometryResource>
    {
        /// <summary>
        /// Initializes a new instance of the BlendGeometryResourceFactory class.
        /// </summary>
        public BlendGeometryResourceFactory() : base(new[] { "0x015A1849" }) // TBD: Set correct resource type
        {
        }

        /// <inheritdoc />
        public override Task<IBlendGeometryResource> CreateResourceAsync(
            int apiVersion,
            Stream? stream = null,
            CancellationToken cancellationToken = default)
        {
            var resource = new BlendGeometryResource();

            // Load blend geometry data from stream if it contains data
            if (stream != null && stream.Length > 0)
            {
                // TBD: Implement stream loading
            }

            return Task.FromResult<IBlendGeometryResource>(resource);
        }

        /// <summary>
        /// Creates a new BlendGeometryResource with the specified mesh name and version.
        /// </summary>
        /// <param name="meshName">The mesh name/identifier.</param>
        /// <param name="version">The version string.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A new BlendGeometryResource instance.</returns>
        public async Task<IBlendGeometryResource> CreateAsync(string meshName, string version, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(meshName);
            ArgumentException.ThrowIfNullOrWhiteSpace(version);

            var resource = new BlendGeometryResource(meshName, version);
            await Task.CompletedTask;
            return resource;
        }
    }
}
