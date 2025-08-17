using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TS4Tools.Core.Resources;

namespace TS4Tools.Resources.Specialized.Templates
{
    /// <summary>
    /// Factory for creating ComplateResource instances.
    /// Supports both creation from streams and new template creation.
    /// </summary>
    public class ComplateResourceFactory : ResourceFactoryBase<IComplateResource>
    {
        /// <summary>
        /// Initializes a new instance of the ComplateResourceFactory class.
        /// </summary>
        public ComplateResourceFactory() : base(new[] { "0x00000000" }) // TBD: Set correct resource type
        {
        }

        /// <inheritdoc />
        public override Task<IComplateResource> CreateResourceAsync(
            int apiVersion,
            Stream? stream = null,
            CancellationToken cancellationToken = default)
        {
            var resource = new ComplateResource();

            // Load template data from stream if it contains data
            if (stream != null && stream.Length > 0)
            {
                return LoadTemplateDataAsync(resource, stream, cancellationToken);
            }

            return Task.FromResult<IComplateResource>(resource);
        }

        #region Private Methods

        private static async Task<IComplateResource> LoadTemplateDataAsync(
            ComplateResource resource,
            Stream stream,
            CancellationToken cancellationToken)
        {
            try
            {
                using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);

                // Read template header
                var templateName = reader.ReadString();
                var parentTemplateId = reader.ReadUInt32();
                var isInheritable = reader.ReadBoolean();

                // Update resource properties using the indexers
                resource["TemplateName"] = templateName;
                resource["ParentTemplateId"] = parentTemplateId == 0 ? null : parentTemplateId;
                resource["IsInheritable"] = isInheritable;

                // Read parameters
                var parameterCount = reader.ReadInt32();
                for (int i = 0; i < parameterCount; i++)
                {
                    var parameter = ReadParameter(reader);
                    await resource.SetParameterAsync(parameter.Name, parameter, cancellationToken);
                }

                // Read template data
                var dataCount = reader.ReadInt32();
                var templateData = new System.Collections.Generic.Dictionary<string, object>();

                for (int i = 0; i < dataCount; i++)
                {
                    var key = reader.ReadString();
                    var value = ReadValue(reader);
                    templateData[key] = value;
                }

                await resource.UpdateTemplateDataAsync(templateData, cancellationToken);

                return resource;
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Failed to load template data from stream: {ex.Message}", ex);
            }
        }

        private static TemplateParameter ReadParameter(BinaryReader reader)
        {
            var name = reader.ReadString();
            var typeName = reader.ReadString();
            var isRequired = reader.ReadBoolean();
            var description = reader.ReadString();

            var hasDefaultValue = reader.ReadBoolean();
            object? defaultValue = null;
            if (hasDefaultValue)
            {
                defaultValue = ReadValue(reader);
            }

            // Resolve type from type name
            var parameterType = Type.GetType(typeName) ?? typeof(object);

            return new TemplateParameter
            {
                Name = name,
                ParameterType = parameterType,
                IsRequired = isRequired,
                Description = description,
                DefaultValue = defaultValue,
                Constraints = Array.Empty<IParameterConstraint>()
            };
        }

        private static object ReadValue(BinaryReader reader)
        {
            var typeCode = reader.ReadByte();

            return typeCode switch
            {
                1 => reader.ReadString(),
                2 => reader.ReadInt32(),
                3 => reader.ReadUInt32(),
                4 => reader.ReadBoolean(),
                5 => reader.ReadSingle(),
                6 => reader.ReadDouble(),
                _ => reader.ReadString() // Fallback to string
            };
        }

        #endregion
    }
}
