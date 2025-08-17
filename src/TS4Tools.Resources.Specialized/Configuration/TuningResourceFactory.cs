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
    /// Factory for creating TuningResource instances.
    /// Provides methods to create tuning resources from streams or create new tuning resources.
    /// </summary>
    public class TuningResourceFactory : ResourceFactoryBase<ITuningResource>
    {
        /// <summary>
        /// Initializes a new instance of the TuningResourceFactory class.
        /// </summary>
        public TuningResourceFactory() : base(new[] { "TuningResource" }, 1)
        {
        }
        /// <inheritdoc />
        public override async Task<ITuningResource> CreateResourceAsync(int apiVersion, Stream? stream = null, CancellationToken cancellationToken = default)
        {
            var tuningResource = new TuningResource();

            if (stream != null)
            {
                await LoadFromStreamAsync(tuningResource, stream, cancellationToken);
            }

            return tuningResource;
        }

        /// <summary>
        /// Creates a new tuning resource with specified configuration.
        /// </summary>
        /// <param name="tuningName">Tuning name/identifier.</param>
        /// <param name="category">Tuning category.</param>
        /// <param name="instance">Tuning instance ID.</param>
        /// <param name="parentTuningId">Parent tuning ID if inheriting.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>New tuning resource.</returns>
        public async Task<ITuningResource> CreateTuningAsync(
            string tuningName,
            string category,
            ulong instance,
            uint? parentTuningId = null,
            CancellationToken cancellationToken = default)
        {
            var tuningResource = new TuningResource(tuningName, category, instance, parentTuningId);
            await Task.CompletedTask;
            return tuningResource;
        }

        /// <summary>
        /// Creates a tuning resource from tuning data.
        /// </summary>
        /// <param name="tuningName">Tuning name/identifier.</param>
        /// <param name="category">Tuning category.</param>
        /// <param name="instance">Tuning instance ID.</param>
        /// <param name="tuningData">Initial tuning data.</param>
        /// <param name="parameters">Parameter definitions.</param>
        /// <param name="parentTuningId">Parent tuning ID if inheriting.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>New tuning resource with data.</returns>
        public async Task<ITuningResource> CreateWithDataAsync(
            string tuningName,
            string category,
            ulong instance,
            IDictionary<string, object> tuningData,
            IEnumerable<TuningParameter>? parameters = null,
            uint? parentTuningId = null,
            CancellationToken cancellationToken = default)
        {
            var tuningResource = new TuningResource(tuningName, category, instance, parentTuningId);

            // Add parameter definitions
            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    await tuningResource.AddParameterDefinitionAsync(parameter, cancellationToken);
                }
            }

            // Set tuning data
            await tuningResource.UpdateTuningDataAsync(tuningData, cancellationToken);

            return tuningResource;
        }

        private static async Task LoadFromStreamAsync(
            TuningResource tuningResource,
            Stream stream,
            CancellationToken cancellationToken)
        {
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

            // Read tuning header
            var tuningName = reader.ReadString();
            var tuningCategory = reader.ReadString();
            var tuningInstance = reader.ReadUInt64();
            var parentTuningId = reader.ReadUInt32();

            // Set properties using reflection or casting to concrete type
            if (tuningResource is TuningResource concreteTuning)
            {
                SetTuningProperties(concreteTuning, tuningName, tuningCategory, tuningInstance,
                    parentTuningId == 0 ? null : parentTuningId);
            }

            // Read parameters
            var parameterCount = reader.ReadInt32();
            for (int i = 0; i < parameterCount; i++)
            {
                var parameter = ReadParameter(reader);
                await tuningResource.AddParameterDefinitionAsync(parameter, cancellationToken);
            }

            // Read tuning data
            var dataCount = reader.ReadInt32();
            var tuningData = new Dictionary<string, object>();
            for (int i = 0; i < dataCount; i++)
            {
                var key = reader.ReadString();
                var value = ReadValue(reader);
                tuningData[key] = value;
            }

            await tuningResource.UpdateTuningDataAsync(tuningData, cancellationToken);
        }

        private static void SetTuningProperties(
            TuningResource tuningResource,
            string tuningName,
            string tuningCategory,
            ulong tuningInstance,
            uint? parentTuningId)
        {
            // Use reflection to set private properties
            var type = typeof(TuningResource);

            var tuningNameProperty = type.GetProperty(nameof(ITuningResource.TuningName));
            tuningNameProperty?.SetValue(tuningResource, tuningName);

            var tuningCategoryProperty = type.GetProperty(nameof(ITuningResource.TuningCategory));
            tuningCategoryProperty?.SetValue(tuningResource, tuningCategory);

            var tuningInstanceProperty = type.GetProperty(nameof(ITuningResource.TuningInstance));
            tuningInstanceProperty?.SetValue(tuningResource, tuningInstance);

            var parentTuningIdProperty = type.GetProperty(nameof(ITuningResource.ParentTuningId));
            parentTuningIdProperty?.SetValue(tuningResource, parentTuningId);
        }

        private static TuningParameter ReadParameter(BinaryReader reader)
        {
            var name = reader.ReadString();
            var typeName = reader.ReadString();
            var isRequired = reader.ReadBoolean();
            var description = reader.ReadString();

            // Read default value
            object? defaultValue = null;
            if (reader.ReadBoolean())
            {
                defaultValue = ReadValue(reader);
            }

            // Read min/max values
            object? minValue = null;
            if (reader.ReadBoolean())
            {
                minValue = ReadValue(reader);
            }

            object? maxValue = null;
            if (reader.ReadBoolean())
            {
                maxValue = ReadValue(reader);
            }

            // Read allowed values
            var allowedValueCount = reader.ReadInt32();
            var allowedValues = new List<object>();
            for (int i = 0; i < allowedValueCount; i++)
            {
                allowedValues.Add(ReadValue(reader));
            }

            // Try to resolve type
            var parameterType = Type.GetType(typeName) ?? typeof(object);

            return new TuningParameter
            {
                Name = name,
                ParameterType = parameterType,
                IsRequired = isRequired,
                Description = description,
                DefaultValue = defaultValue,
                MinValue = minValue,
                MaxValue = maxValue,
                AllowedValues = allowedValues,
                Validators = new List<ITuningParameterValidator>()
            };
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
                7 => reader.ReadUInt64(),
                _ => reader.ReadString()
            };
        }
    }
}
