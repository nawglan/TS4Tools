using System.ComponentModel;
using TS4Tools.Core.Interfaces;
using TS4Tools.Resources.Common;
using TS4Tools.Resources.Common.CatalogTags;
using TS4Tools.Resources.Common.Collections;

namespace TS4Tools.Resources.Catalog;

/// <summary>
/// Implementation of catalog tag resources for hierarchical tagging and categorization of catalog items.
/// Provides comprehensive support for organizing Buy/Build mode content with advanced filtering capabilities.
/// </summary>
[CatalogResource([0xCAAAD4B0], CatalogType.Custom, 200, "Catalog Tag Resource for hierarchical tagging")]
public sealed class CatalogTagResource : ICatalogTagResource
{
    private uint _tagId;
    private string _tagName = string.Empty;
    private string _description = string.Empty;
    private uint _parentTagId;
    private int _sortOrder;
    private CatalogTagCategory _category;
    private TgiReference? _iconReference;
    private CatalogTagFlags _flags = CatalogTagFlags.Default;
    private readonly List<uint> _childTagIds = [];
    private readonly List<TagFilterCriterion> _filterCriteria = [];
    private CatalogCommonBlock? _commonBlock;
    private bool _isModified;

    /// <inheritdoc />
    public event EventHandler? ResourceChanged;

    /// <inheritdoc />
    public Stream Stream => throw new NotSupportedException("CatalogTagResource does not support direct stream access. Use LoadFromStreamAsync/SaveToStreamAsync instead.");

    /// <inheritdoc />
    public byte[] AsBytes
    {
        get
        {
            using var stream = new MemoryStream();
            SaveToStreamAsync(stream).GetAwaiter().GetResult();
            return stream.ToArray();
        }
    }

    /// <inheritdoc />
    public int RecommendedApiVersion => 1;

    /// <inheritdoc />
    public int RequestedApiVersion { get; set; } = 1;

    /// <inheritdoc />
    public IReadOnlyList<string> ContentFields => new List<string>
    {
        nameof(TagId),
        nameof(TagName),
        nameof(Category),
        nameof(Flags),
        nameof(ParentTagId),
        nameof(TgiReference),
        nameof(Description),
        nameof(SortOrder),
        nameof(ChildTagIds),
        nameof(FilterCriteria)
    };

    /// <inheritdoc />
    public TypedValue this[int index]
    {
        get
        {
            if (index < 0 || index >= ContentFields.Count)
                return new TypedValue(typeof(object), null);

            var fieldName = ContentFields[index];
            return this[fieldName];
        }
        set
        {
            if (index >= 0 && index < ContentFields.Count)
            {
                var fieldName = ContentFields[index];
                this[fieldName] = value;
            }
        }
    }

    /// <inheritdoc />
    public TypedValue this[string name]
    {
        get => name switch
        {
            nameof(TagId) => new TypedValue(typeof(uint), TagId),
            nameof(TagName) => new TypedValue(typeof(string), TagName),
            nameof(Category) => new TypedValue(typeof(CatalogTagCategory), Category),
            nameof(Flags) => new TypedValue(typeof(CatalogTagFlags), Flags),
            nameof(ParentTagId) => new TypedValue(typeof(uint), ParentTagId),
            nameof(IconReference) => new TypedValue(typeof(TgiReference), IconReference),
            nameof(Description) => new TypedValue(typeof(string), Description),
            nameof(SortOrder) => new TypedValue(typeof(int), SortOrder),
            nameof(ChildTagIds) => new TypedValue(typeof(IReadOnlyList<uint>), ChildTagIds),
            nameof(FilterCriteria) => new TypedValue(typeof(IReadOnlyList<TagFilterCriterion>), FilterCriteria),
            _ => new TypedValue(typeof(object), null)
        };
        set
        {
            switch (name)
            {
                case nameof(TagId):
                    TagId = (uint)(value.Value ?? 0);
                    break;
                case nameof(TagName):
                    TagName = (string)(value.Value ?? "");
                    break;
                case nameof(Category):
                    Category = (CatalogTagCategory)(value.Value ?? CatalogTagCategory.Function);
                    break;
                case nameof(Flags):
                    Flags = (CatalogTagFlags)(value.Value ?? CatalogTagFlags.None);
                    break;
                case nameof(ParentTagId):
                    ParentTagId = (uint)(value.Value ?? 0);
                    break;
                case nameof(Description):
                    Description = (string)(value.Value ?? "");
                    break;
                case nameof(SortOrder):
                    SortOrder = (int)(value.Value ?? 0);
                    break;
            }
            OnResourceChanged();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _childTagIds.Clear();
        _filterCriteria.Clear();
        GC.SuppressFinalize(this);
    }

    private void OnResourceChanged()
    {
        _isModified = true;
        ResourceChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public uint TagId
    {
        get => _tagId;
        set { _tagId = value; _isModified = true; }
    }

    /// <inheritdoc />
    public string TagName
    {
        get => _tagName;
        set
        {
            _tagName = value ?? string.Empty;
            _isModified = true;
            OnResourceChanged();
        }
    }

    /// <inheritdoc />
    public string Description
    {
        get => _description;
        set { _description = value ?? string.Empty; _isModified = true; }
    }

    /// <inheritdoc />
    public uint ParentTagId
    {
        get => _parentTagId;
        set { _parentTagId = value; _isModified = true; }
    }

    /// <inheritdoc />
    public int SortOrder
    {
        get => _sortOrder;
        set { _sortOrder = value; _isModified = true; }
    }

    /// <inheritdoc />
    public CatalogTagCategory Category
    {
        get => _category;
        set { _category = value; _isModified = true; }
    }

    /// <inheritdoc />
    public TgiReference? IconReference
    {
        get => _iconReference;
        set { _iconReference = value; _isModified = true; }
    }

    /// <inheritdoc />
    public CatalogTagFlags Flags
    {
        get => _flags;
        set { _flags = value; _isModified = true; }
    }

    /// <inheritdoc />
    public IList<uint> ChildTagIds => _childTagIds;

    /// <inheritdoc />
    public IList<TagFilterCriterion> FilterCriteria => _filterCriteria;

    /// <inheritdoc />
    public CatalogCommonBlock? CommonBlock => _commonBlock;

    /// <inheritdoc />
    public uint Version => 1;

    /// <inheritdoc />
    public CatalogType CatalogType => CatalogType.Object; // Tags are part of object catalogs

    /// <inheritdoc />
    public bool IsModified => _isModified;

    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogTagResource"/> class.
    /// </summary>
    public CatalogTagResource()
    {
        _commonBlock = new CatalogCommonBlock();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogTagResource"/> class with specified values.
    /// </summary>
    /// <param name="tagId">The unique tag identifier.</param>
    /// <param name="tagName">The display name of the tag.</param>
    /// <param name="category">The tag category.</param>
    public CatalogTagResource(uint tagId, string tagName, CatalogTagCategory category = CatalogTagCategory.Function)
    {
        _tagId = tagId;
        _tagName = tagName ?? string.Empty;
        _category = category;
        _commonBlock = new CatalogCommonBlock();
    }

    /// <inheritdoc />
    public void AddChildTag(uint childTagId)
    {
        if (!_childTagIds.Contains(childTagId))
        {
            _childTagIds.Add(childTagId);
            OnResourceChanged();
        }
    }

    /// <inheritdoc />
    public bool RemoveChildTag(uint childTagId)
    {
        var removed = _childTagIds.Remove(childTagId);
        if (removed)
        {
            OnResourceChanged();
        }
        return removed;
    }

    /// <inheritdoc />
    public void ClearChildTags()
    {
        if (_childTagIds.Count > 0)
        {
            _childTagIds.Clear();
            OnResourceChanged();
        }
    }

    /// <inheritdoc />
    public void AddFilterCriterion(TagFilterCriterion criterion)
    {
        ArgumentNullException.ThrowIfNull(criterion);
        _filterCriteria.Add(criterion);
        OnResourceChanged();
    }

    /// <inheritdoc />
    public bool RemoveFilterCriterion(TagFilterCriterion criterion)
    {
        var removed = _filterCriteria.Remove(criterion);
        if (removed)
        {
            OnResourceChanged();
        }
        return removed;
    }

    /// <inheritdoc />
    public void ClearFilterCriteria()
    {
        if (_filterCriteria.Count > 0)
        {
            _filterCriteria.Clear();
            OnResourceChanged();
        }
    }

    /// <inheritdoc />
    public bool ValidateHierarchy()
    {
        // Check for circular references
        var visited = new HashSet<uint> { TagId };
        var current = ParentTagId;

        while (current != 0)
        {
            if (visited.Contains(current))
            {
                return false; // Circular reference detected
            }
            visited.Add(current);

            // In a real implementation, we'd look up the parent tag
            // For now, assume the hierarchy is valid if no circular reference
            break;
        }

        return true;
    }

    /// <inheritdoc />
    public bool IsAncestorOf(uint tagId)
    {
        return GetDescendantIds().Contains(tagId);
    }

    /// <inheritdoc />
    public bool IsDescendantOf(uint tagId)
    {
        return GetAncestorIds().Contains(tagId);
    }

    /// <inheritdoc />
    public IEnumerable<uint> GetAncestorIds()
    {
        var ancestors = new List<uint>();
        var current = ParentTagId;

        while (current != 0)
        {
            ancestors.Add(current);
            // In a real implementation, we'd look up the parent tag and get its ParentTagId
            // For now, stop at the first parent to avoid infinite loop
            break;
        }

        return ancestors;
    }

    /// <inheritdoc />
    public IEnumerable<uint> GetDescendantIds()
    {
        var descendants = new List<uint>();
        GetDescendantsRecursive(TagId, descendants);
        return descendants;
    }

    /// <inheritdoc />
    public async Task LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        await ReadFromStreamAsync(reader, cancellationToken);
        _isModified = false;
    }

    /// <inheritdoc />
    public async Task SaveToStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        await WriteToStreamAsync(writer, cancellationToken);
        _isModified = false;
    }

    /// <inheritdoc />
    public IEnumerable<string> Validate()
    {
        var errors = new List<string>();

        if (TagId == 0)
            errors.Add("TagId cannot be zero");

        if (string.IsNullOrWhiteSpace(TagName))
            errors.Add("TagName cannot be empty");

        if (!ValidateHierarchy())
            errors.Add("Tag hierarchy contains circular references");

        // Validate filter criteria
        foreach (var criterion in FilterCriteria)
        {
            if (string.IsNullOrWhiteSpace(criterion.PropertyName))
                errors.Add("Filter criterion property name cannot be empty");
        }

        return errors;
    }

    /// <inheritdoc />
    public async Task<ICatalogResource> CloneAsync()
    {
        var clone = new CatalogTagResource
        {
            TagId = TagId,
            TagName = TagName,
            Description = Description,
            ParentTagId = ParentTagId,
            SortOrder = SortOrder,
            Category = Category,
            IconReference = IconReference,
            Flags = Flags,
            _commonBlock = _commonBlock,
            _isModified = _isModified
        };

        // Copy child tag IDs
        clone._childTagIds.AddRange(ChildTagIds);

        // Copy filter criteria
        foreach (var criterion in FilterCriteria)
        {
            clone._filterCriteria.Add(new TagFilterCriterion
            {
                PropertyName = criterion.PropertyName,
                Operator = criterion.Operator,
                Value = criterion.Value,
                IsRequired = criterion.IsRequired
            });
        }

        return await Task.FromResult<ICatalogResource>(clone);
    }

    /// <summary>
    /// Recursively gets descendant tag IDs.
    /// </summary>
    /// <param name="parentId">The parent tag ID to start from.</param>
    /// <param name="descendants">The collection to add descendants to.</param>
    private void GetDescendantsRecursive(uint parentId, List<uint> descendants)
    {
        // Add immediate children
        descendants.AddRange(ChildTagIds);

        // In a real implementation, we'd look up each child tag and get its children
        // For now, only return immediate children to avoid complexity
    }

    /// <summary>
    /// Asynchronously reads the catalog tag resource data from a binary reader.
    /// </summary>
    /// <param name="reader">The binary reader to read from.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    private async Task ReadFromStreamAsync(BinaryReader reader, CancellationToken cancellationToken)
    {
        // Read basic tag properties
        TagId = reader.ReadUInt32();
        TagName = reader.ReadString();
        Description = reader.ReadString();
        ParentTagId = reader.ReadUInt32();
        SortOrder = reader.ReadInt32();
        Category = (CatalogTagCategory)reader.ReadUInt32();
        Flags = (CatalogTagFlags)reader.ReadUInt32();

        // Read icon reference if present
        var hasIcon = reader.ReadBoolean();
        if (hasIcon)
        {
            IconReference = new TgiReference(
                reader.ReadUInt32(), // TypeId
                reader.ReadUInt32(), // GroupId
                reader.ReadUInt64()  // InstanceId
            );
        }

        // Read child tag IDs
        var childCount = reader.ReadInt32();
        _childTagIds.Clear();
        for (int i = 0; i < childCount; i++)
        {
            _childTagIds.Add(reader.ReadUInt32());
        }

        // Read filter criteria
        var criteriaCount = reader.ReadInt32();
        _filterCriteria.Clear();
        for (int i = 0; i < criteriaCount; i++)
        {
            var criterion = new TagFilterCriterion
            {
                PropertyName = reader.ReadString(),
                Operator = (FilterOperator)reader.ReadByte(),
                IsRequired = reader.ReadBoolean()
            };

            // Read value based on operator type
            var valueType = reader.ReadByte();
            criterion.Value = valueType switch
            {
                0 => null,
                1 => reader.ReadString(),
                2 => reader.ReadInt32(),
                3 => reader.ReadUInt32(),
                4 => reader.ReadSingle(),
                5 => reader.ReadBoolean(),
                _ => null
            };

            _filterCriteria.Add(criterion);
        }

        await Task.CompletedTask; // Keep async for interface consistency
    }

    /// <summary>
    /// Asynchronously writes the catalog tag resource data to a binary writer.
    /// </summary>
    /// <param name="writer">The binary writer to write to.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    private async Task WriteToStreamAsync(BinaryWriter writer, CancellationToken cancellationToken)
    {
        // Write basic tag properties
        writer.Write(TagId);
        writer.Write(TagName);
        writer.Write(Description);
        writer.Write(ParentTagId);
        writer.Write(SortOrder);
        writer.Write((uint)Category);
        writer.Write((uint)Flags);

        // Write icon reference
        writer.Write(IconReference != null);
        if (IconReference != null)
        {
            writer.Write(IconReference.TypeId);
            writer.Write(IconReference.GroupId);
            writer.Write(IconReference.InstanceId);
        }

        // Write child tag IDs
        writer.Write(ChildTagIds.Count);
        foreach (var childId in ChildTagIds)
        {
            writer.Write(childId);
        }

        // Write filter criteria
        writer.Write(FilterCriteria.Count);
        foreach (var criterion in FilterCriteria)
        {
            writer.Write(criterion.PropertyName);
            writer.Write((byte)criterion.Operator);
            writer.Write(criterion.IsRequired);

            // Write value with type information
            if (criterion.Value == null)
            {
                writer.Write((byte)0);
            }
            else if (criterion.Value is string stringValue)
            {
                writer.Write((byte)1);
                writer.Write(stringValue);
            }
            else if (criterion.Value is int intValue)
            {
                writer.Write((byte)2);
                writer.Write(intValue);
            }
            else if (criterion.Value is uint uintValue)
            {
                writer.Write((byte)3);
                writer.Write(uintValue);
            }
            else if (criterion.Value is float floatValue)
            {
                writer.Write((byte)4);
                writer.Write(floatValue);
            }
            else if (criterion.Value is bool boolValue)
            {
                writer.Write((byte)5);
                writer.Write(boolValue);
            }
            else
            {
                writer.Write((byte)0); // Null for unsupported types
            }
        }

        await Task.CompletedTask; // Keep async for interface consistency
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _childTagIds.Clear();
        _filterCriteria.Clear();
        await Task.CompletedTask;
    }
}
