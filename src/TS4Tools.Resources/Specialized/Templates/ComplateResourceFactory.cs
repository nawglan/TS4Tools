using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TS4Tools.Resources.Core;

namespace TS4Tools.Resources.Specialized.Templates
{
    /// <summary>
    /// Factory for creating ComplateResource instances.
    /// Supports both creation from streams and new template creation.
    /// </summary>
    public class ComplateResourceFactory : ResourceFactoryBase
    {
        /// <summary>
        /// Initializes a new instance of the ComplateResourceFactory class.
        /// </summary>
        public ComplateResourceFactory() : base()
        {
        }

        /// <inheritdoc />
        public override async Task<IResource> CreateResourceAsync(
            uint resourceType,
            uint resourceGroup,
            ulong resourceInstance,
            Stream stream,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(stream);

            var resource = new ComplateResource
            {
                ResourceType = resourceType,
                ResourceGroup = resourceGroup,
                ResourceInstance = resourceInstance
            };

            // Load template data from stream if it contains data
            if (stream.Length > 0)
            {
                await LoadTemplateDataAsync(resource, stream, cancellationToken);
            }

            return resource;
        }

        /// <inheritdoc />
        public override IResource CreateResource(
            uint resourceType,
            uint resourceGroup,
            ulong resourceInstance,
            Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            var resource = new ComplateResource
            {
                ResourceType = resourceType,
                ResourceGroup = resourceGroup,
                ResourceInstance = resourceInstance
            };

            // Load template data from stream if it contains data
            if (stream.Length > 0)
            {
                LoadTemplateDataSync(resource, stream);
            }

            return resource;
        }

        /// <summary>
        /// Creates a new template resource with the specified name and optional parent template.
        /// </summary>
        /// <param name="templateName">Name for the new template.</param>
        /// <param name="parentTemplateId">Optional parent template ID for inheritance.</param>
        /// <param name="resourceType">Resource type identifier.</param>
        /// <param name="resourceGroup">Resource group identifier.</param>
        /// <param name="resourceInstance">Resource instance identifier.</param>
        /// <returns>New ComplateResource instance.</returns>
        public ComplateResource CreateTemplate(
            string templateName,
            uint? parentTemplateId = null,
            uint resourceType = 0,
            uint resourceGroup = 0,
            ulong resourceInstance = 0)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(templateName);

            return new ComplateResource(templateName, null, parentTemplateId)
            {
                ResourceType = resourceType,
                ResourceGroup = resourceGroup,
                ResourceInstance = resourceInstance
            };
        }

        /// <summary>
        /// Creates a new template resource with initial template data.
        /// </summary>
        /// <param name="templateName">Name for the new template.</param>
        /// <param name="templateData">Initial template data.</param>
        /// <param name="parentTemplateId">Optional parent template ID for inheritance.</param>
        /// <param name="resourceType">Resource type identifier.</param>
        /// <param name="resourceGroup">Resource group identifier.</param>
        /// <param name="resourceInstance">Resource instance identifier.</param>
        /// <returns>New ComplateResource instance.</returns>
        public ComplateResource CreateTemplateWithData(
            string templateName,
            System.Collections.Generic.IDictionary<string, object> templateData,
            uint? parentTemplateId = null,
            uint resourceType = 0,
            uint resourceGroup = 0,
            ulong resourceInstance = 0)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(templateName);
            ArgumentNullException.ThrowIfNull(templateData);

            return new ComplateResource(templateName, templateData, parentTemplateId)
            {
                ResourceType = resourceType,
                ResourceGroup = resourceGroup,
                ResourceInstance = resourceInstance
            };
        }

        #region Private Methods

        private static async Task LoadTemplateDataAsync(
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

                // Update resource properties via reflection or internal access
                // Note: In a real implementation, you'd need internal setters or reflection
                SetTemplateProperties(resource, templateName, parentTemplateId == 0 ? null : parentTemplateId, isInheritable);

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
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Failed to load template data from stream: {ex.Message}", ex);
            }
        }

        private static void LoadTemplateDataSync(ComplateResource resource, Stream stream)
        {
            try
            {
                using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);

                // Read template header
                var templateName = reader.ReadString();
                var parentTemplateId = reader.ReadUInt32();
                var isInheritable = reader.ReadBoolean();

                // Update resource properties
                SetTemplateProperties(resource, templateName, parentTemplateId == 0 ? null : parentTemplateId, isInheritable);

                // Read parameters
                var parameterCount = reader.ReadInt32();
                for (int i = 0; i < parameterCount; i++)
                {
                    var parameter = ReadParameter(reader);
                    resource.SetParameterAsync(parameter.Name, parameter).Wait();
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

                resource.UpdateTemplateDataAsync(templateData).Wait();
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Failed to load template data from stream: {ex.Message}", ex);
            }
        }

        private static void SetTemplateProperties(
            ComplateResource resource,
            string templateName,
            uint? parentTemplateId,
            bool isInheritable)
        {
            // This would typically require internal setters or reflection
            // For now, using ContentFields as a workaround
            resource.ContentFields["TemplateName"] = TypedValue.Create(templateName);
            resource.ContentFields["ParentTemplateId"] = TypedValue.Create(parentTemplateId);
            resource.ContentFields["IsInheritable"] = TypedValue.Create(isInheritable);
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
