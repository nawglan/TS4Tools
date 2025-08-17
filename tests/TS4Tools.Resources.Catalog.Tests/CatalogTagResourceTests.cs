/***************************************************************************
 *  Copyright (C) 2025 TS4Tools Project                                    *
 *                                                                         *
 *  This file is part of TS4Tools                                         *
 *                                                                         *
 *  TS4Tools is free software: you can redistribute it and/or modify      *
 *  it under the terms of the GNU General Public License as published by   *
 *  the Free Software Foundation, either version 3 of the License, or      *
 *  (at your option) any later version.                                    *
 *                                                                         *
 *  TS4Tools is distributed in the hope that it will be useful,           *
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of         *
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the          *
 *  GNU General Public License for more details.                           *
 *                                                                         *
 *  You should have received a copy of the GNU General Public License      *
 *  along with TS4Tools.  If not, see <http://www.gnu.org/licenses/>.     *
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TS4Tools.Resources.Catalog;
using TS4Tools.Resources.Common;
using Xunit;

namespace TS4Tools.Resources.Catalog.Tests;

/// <summary>
/// Unit tests for <see cref="CatalogTagResource"/>.
/// </summary>
public class CatalogTagResourceTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_Default_SetsDefaultValues()
    {
        // Arrange & Act
        var resource = new CatalogTagResource();

        // Assert
        Assert.Equal(0u, resource.TagId);
        Assert.Equal(string.Empty, resource.TagName);
        Assert.Equal(string.Empty, resource.Description);
        Assert.Equal(0u, resource.ParentTagId);
        Assert.Equal(0, resource.SortOrder);
        Assert.Equal(CatalogTagCategory.Function, resource.Category);
        Assert.Equal(CatalogTagFlags.Default, resource.Flags);
        Assert.Null(resource.IconReference);
        Assert.Empty(resource.ChildTagIds);
        Assert.Empty(resource.FilterCriteria);
        Assert.False(resource.IsModified);
    }

    [Fact]
    public void Constructor_WithParameters_SetsSpecifiedValues()
    {
        // Arrange
        const uint tagId = 12345u;
        const string tagName = "TestTag";
        const CatalogTagCategory category = CatalogTagCategory.Style;

        // Act
        var resource = new CatalogTagResource(tagId, tagName, category);

        // Assert
        Assert.Equal(tagId, resource.TagId);
        Assert.Equal(tagName, resource.TagName);
        Assert.Equal(category, resource.Category);
    }

    [Fact]
    public void Constructor_WithNullTagName_SetsEmptyString()
    {
        // Arrange & Act
        var resource = new CatalogTagResource(123, null!, CatalogTagCategory.Room);

        // Assert
        Assert.Equal(string.Empty, resource.TagName);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void TagId_SetValue_UpdatesProperty()
    {
        // Arrange
        var resource = new CatalogTagResource();
        const uint newTagId = 999u;

        // Act
        resource.TagId = newTagId;

        // Assert
        Assert.Equal(newTagId, resource.TagId);
        Assert.True(resource.IsModified);
    }

    [Fact]
    public void TagName_SetValue_UpdatesProperty()
    {
        // Arrange
        var resource = new CatalogTagResource();
        const string newTagName = "New Tag Name";

        // Act
        resource.TagName = newTagName;

        // Assert
        Assert.Equal(newTagName, resource.TagName);
        Assert.True(resource.IsModified);
    }

    [Fact]
    public void Description_SetValue_UpdatesProperty()
    {
        // Arrange
        var resource = new CatalogTagResource();
        const string newDescription = "Test description";

        // Act
        resource.Description = newDescription;

        // Assert
        Assert.Equal(newDescription, resource.Description);
        Assert.True(resource.IsModified);
    }

    [Fact]
    public void ParentTagId_SetValue_UpdatesProperty()
    {
        // Arrange
        var resource = new CatalogTagResource();
        const uint newParentId = 456u;

        // Act
        resource.ParentTagId = newParentId;

        // Assert
        Assert.Equal(newParentId, resource.ParentTagId);
        Assert.True(resource.IsModified);
    }

    [Fact]
    public void SortOrder_SetValue_UpdatesProperty()
    {
        // Arrange
        var resource = new CatalogTagResource();
        const int newSortOrder = 10;

        // Act
        resource.SortOrder = newSortOrder;

        // Assert
        Assert.Equal(newSortOrder, resource.SortOrder);
        Assert.True(resource.IsModified);
    }

    [Fact]
    public void Category_SetValue_UpdatesProperty()
    {
        // Arrange
        var resource = new CatalogTagResource();
        const CatalogTagCategory newCategory = CatalogTagCategory.Material;

        // Act
        resource.Category = newCategory;

        // Assert
        Assert.Equal(newCategory, resource.Category);
        Assert.True(resource.IsModified);
    }

    [Fact]
    public void Flags_SetValue_UpdatesProperty()
    {
        // Arrange
        var resource = new CatalogTagResource();
        const CatalogTagFlags newFlags = CatalogTagFlags.Visible | CatalogTagFlags.Selectable;

        // Act
        resource.Flags = newFlags;

        // Assert
        Assert.Equal(newFlags, resource.Flags);
        Assert.True(resource.IsModified);
    }

    [Fact]
    public void IconReference_SetValue_UpdatesProperty()
    {
        // Arrange
        var resource = new CatalogTagResource();
        var newIconRef = new TgiReference(0x12345678, 0x87654321, 0x123456789ABCDEF0);

        // Act
        resource.IconReference = newIconRef;

        // Assert
        Assert.Equal(newIconRef, resource.IconReference);
        Assert.True(resource.IsModified);
    }

    #endregion

    #region Child Tag Management Tests

    [Fact]
    public void AddChildTag_ValidId_AddsToCollection()
    {
        // Arrange
        var resource = new CatalogTagResource();
        const uint childId = 789u;

        // Act
        resource.AddChildTag(childId);

        // Assert
        Assert.Contains(childId, resource.ChildTagIds);
        Assert.True(resource.IsModified);
    }

    [Fact]
    public void AddChildTag_DuplicateId_DoesNotAddDuplicate()
    {
        // Arrange
        var resource = new CatalogTagResource();
        const uint childId = 789u;
        resource.AddChildTag(childId);

        // Act
        resource.AddChildTag(childId);

        // Assert
        Assert.Single(resource.ChildTagIds);
        Assert.Equal(childId, resource.ChildTagIds.First());
    }

    [Fact]
    public void RemoveChildTag_ExistingId_RemovesFromCollection()
    {
        // Arrange
        var resource = new CatalogTagResource();
        const uint childId = 789u;
        resource.AddChildTag(childId);

        // Act
        var result = resource.RemoveChildTag(childId);

        // Assert
        Assert.True(result);
        Assert.DoesNotContain(childId, resource.ChildTagIds);
        Assert.True(resource.IsModified);
    }

    [Fact]
    public void RemoveChildTag_NonExistingId_ReturnsFalse()
    {
        // Arrange
        var resource = new CatalogTagResource();

        // Act
        var result = resource.RemoveChildTag(999u);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ClearChildTags_WithChildren_RemovesAllChildren()
    {
        // Arrange
        var resource = new CatalogTagResource();
        resource.AddChildTag(100u);
        resource.AddChildTag(200u);
        resource.AddChildTag(300u);

        // Act
        resource.ClearChildTags();

        // Assert
        Assert.Empty(resource.ChildTagIds);
        Assert.True(resource.IsModified);
    }

    #endregion

    #region Filter Criteria Tests

    [Fact]
    public void AddFilterCriterion_ValidCriterion_AddsToCollection()
    {
        // Arrange
        var resource = new CatalogTagResource();
        var criterion = new TagFilterCriterion
        {
            PropertyName = "Price",
            Operator = FilterOperator.LessThan,
            Value = 1000,
            IsRequired = true
        };

        // Act
        resource.AddFilterCriterion(criterion);

        // Assert
        Assert.Contains(criterion, resource.FilterCriteria);
        Assert.True(resource.IsModified);
    }

    [Fact]
    public void AddFilterCriterion_NullCriterion_ThrowsArgumentNullException()
    {
        // Arrange
        var resource = new CatalogTagResource();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => resource.AddFilterCriterion(null!));
    }

    [Fact]
    public void RemoveFilterCriterion_ExistingCriterion_RemovesFromCollection()
    {
        // Arrange
        var resource = new CatalogTagResource();
        var criterion = new TagFilterCriterion
        {
            PropertyName = "Color",
            Operator = FilterOperator.Equal,
            Value = "Red"
        };
        resource.AddFilterCriterion(criterion);

        // Act
        var result = resource.RemoveFilterCriterion(criterion);

        // Assert
        Assert.True(result);
        Assert.DoesNotContain(criterion, resource.FilterCriteria);
        Assert.True(resource.IsModified);
    }

    [Fact]
    public void RemoveFilterCriterion_NonExistingCriterion_ReturnsFalse()
    {
        // Arrange
        var resource = new CatalogTagResource();
        var criterion = new TagFilterCriterion
        {
            PropertyName = "Style",
            Operator = FilterOperator.NotEqual,
            Value = "Modern"
        };

        // Act
        var result = resource.RemoveFilterCriterion(criterion);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ClearFilterCriteria_WithCriteria_RemovesAllCriteria()
    {
        // Arrange
        var resource = new CatalogTagResource();
        resource.AddFilterCriterion(new TagFilterCriterion { PropertyName = "Test1", Operator = FilterOperator.Equal, Value = "Value1" });
        resource.AddFilterCriterion(new TagFilterCriterion { PropertyName = "Test2", Operator = FilterOperator.NotEqual, Value = "Value2" });

        // Act
        resource.ClearFilterCriteria();

        // Assert
        Assert.Empty(resource.FilterCriteria);
        Assert.True(resource.IsModified);
    }

    #endregion

    #region Hierarchy Validation Tests

    [Fact]
    public void ValidateHierarchy_NoParent_ReturnsTrue()
    {
        // Arrange
        var resource = new CatalogTagResource { TagId = 123u, ParentTagId = 0u };

        // Act
        var result = resource.ValidateHierarchy();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateHierarchy_WithParent_ReturnsTrue()
    {
        // Arrange
        var resource = new CatalogTagResource { TagId = 123u, ParentTagId = 456u };

        // Act
        var result = resource.ValidateHierarchy();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAncestorOf_WithDescendant_ReturnsTrue()
    {
        // Arrange
        var resource = new CatalogTagResource { TagId = 100u };
        resource.AddChildTag(200u);

        // Act
        var result = resource.IsAncestorOf(200u);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAncestorOf_WithoutDescendant_ReturnsFalse()
    {
        // Arrange
        var resource = new CatalogTagResource { TagId = 100u };

        // Act
        var result = resource.IsAncestorOf(999u);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetDescendantIds_WithChildren_ReturnsChildIds()
    {
        // Arrange
        var resource = new CatalogTagResource { TagId = 100u };
        resource.AddChildTag(200u);
        resource.AddChildTag(300u);

        // Act
        var descendants = resource.GetDescendantIds().ToList();

        // Assert
        Assert.Equal(2, descendants.Count);
        Assert.Contains(200u, descendants);
        Assert.Contains(300u, descendants);
    }

    [Fact]
    public void GetAncestorIds_WithParent_ReturnsParentId()
    {
        // Arrange
        var resource = new CatalogTagResource { TagId = 200u, ParentTagId = 100u };

        // Act
        var ancestors = resource.GetAncestorIds().ToList();

        // Assert
        Assert.Single(ancestors);
        Assert.Equal(100u, ancestors.First());
    }

    [Fact]
    public void GetAncestorIds_WithoutParent_ReturnsEmpty()
    {
        // Arrange
        var resource = new CatalogTagResource { TagId = 100u, ParentTagId = 0u };

        // Act
        var ancestors = resource.GetAncestorIds().ToList();

        // Assert
        Assert.Empty(ancestors);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void Validate_ValidResource_ReturnsNoErrors()
    {
        // Arrange
        var resource = new CatalogTagResource(123u, "ValidTag", CatalogTagCategory.Function);

        // Act
        var errors = resource.Validate().ToList();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ZeroTagId_ReturnsError()
    {
        // Arrange
        var resource = new CatalogTagResource { TagId = 0u, TagName = "ValidTag" };

        // Act
        var errors = resource.Validate().ToList();

        // Assert
        Assert.Contains("TagId cannot be zero", errors);
    }

    [Fact]
    public void Validate_EmptyTagName_ReturnsError()
    {
        // Arrange
        var resource = new CatalogTagResource { TagId = 123u, TagName = "" };

        // Act
        var errors = resource.Validate().ToList();

        // Assert
        Assert.Contains("TagName cannot be empty", errors);
    }

    [Fact]
    public void Validate_WhitespaceTagName_ReturnsError()
    {
        // Arrange
        var resource = new CatalogTagResource { TagId = 123u, TagName = "   " };

        // Act
        var errors = resource.Validate().ToList();

        // Assert
        Assert.Contains("TagName cannot be empty", errors);
    }

    [Fact]
    public void Validate_EmptyFilterCriterionPropertyName_ReturnsError()
    {
        // Arrange
        var resource = new CatalogTagResource(123u, "ValidTag");
        resource.AddFilterCriterion(new TagFilterCriterion
        {
            PropertyName = "",
            Operator = FilterOperator.Equal,
            Value = "test"
        });

        // Act
        var errors = resource.Validate().ToList();

        // Assert
        Assert.Contains("Filter criterion property name cannot be empty", errors);
    }

    #endregion

    #region Clone Tests

    [Fact]
    public async Task CloneAsync_CompleteResource_CreatesExactCopy()
    {
        // Arrange
        var originalResource = new CatalogTagResource(123u, "OriginalTag", CatalogTagCategory.Style)
        {
            Description = "Test description",
            ParentTagId = 456u,
            SortOrder = 10,
            Flags = CatalogTagFlags.Visible | CatalogTagFlags.Selectable,
            IconReference = new TgiReference(0x12345678, 0x87654321, 0x123456789ABCDEF0)
        };

        originalResource.AddChildTag(789u);
        originalResource.AddChildTag(101u);
        originalResource.AddFilterCriterion(new TagFilterCriterion
        {
            PropertyName = "Price",
            Operator = FilterOperator.LessThan,
            Value = 500,
            IsRequired = true
        });

        // Act
        var clonedResource = await originalResource.CloneAsync() as CatalogTagResource;

        // Assert
        Assert.NotNull(clonedResource);
        Assert.NotSame(originalResource, clonedResource);
        Assert.Equal(originalResource.TagId, clonedResource.TagId);
        Assert.Equal(originalResource.TagName, clonedResource.TagName);
        Assert.Equal(originalResource.Description, clonedResource.Description);
        Assert.Equal(originalResource.ParentTagId, clonedResource.ParentTagId);
        Assert.Equal(originalResource.SortOrder, clonedResource.SortOrder);
        Assert.Equal(originalResource.Category, clonedResource.Category);
        Assert.Equal(originalResource.Flags, clonedResource.Flags);
        Assert.Equal(originalResource.IconReference, clonedResource.IconReference);
        Assert.Equal(originalResource.ChildTagIds.Count, clonedResource.ChildTagIds.Count);
        Assert.Equal(originalResource.FilterCriteria.Count, clonedResource.FilterCriteria.Count);
    }

    #endregion

    #region Stream Operations Tests

    [Fact]
    public async Task LoadFromStreamAsync_ValidStream_LoadesCorrectly()
    {
        // Arrange
        var resource = new CatalogTagResource();
        var stream = new MemoryStream();

        // Create a simple binary representation
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
        {
            writer.Write(123u); // TagId
            writer.Write("TestTag"); // TagName
            writer.Write("Test Description"); // Description
            writer.Write(456u); // ParentTagId
            writer.Write(10); // SortOrder
            writer.Write(1u); // Category (Style)
            writer.Write(3u); // Flags (Visible | Selectable)
            writer.Write(false); // HasIcon
            writer.Write(0); // Child count
            writer.Write(0); // Criteria count
        }

        stream.Position = 0;

        // Act
        await resource.LoadFromStreamAsync(stream);

        // Assert
        Assert.Equal(123u, resource.TagId);
        Assert.Equal("TestTag", resource.TagName);
        Assert.Equal("Test Description", resource.Description);
        Assert.Equal(456u, resource.ParentTagId);
        Assert.Equal(10, resource.SortOrder);
        Assert.Equal(CatalogTagCategory.Style, resource.Category);
        Assert.Equal(CatalogTagFlags.Visible | CatalogTagFlags.Selectable, resource.Flags);
        Assert.Null(resource.IconReference);
        Assert.False(resource.IsModified);
    }

    [Fact]
    public async Task SaveToStreamAsync_ValidResource_SavesCorrectly()
    {
        // Arrange
        var resource = new CatalogTagResource(789u, "SaveTestTag", CatalogTagCategory.Material)
        {
            Description = "Save test description",
            ParentTagId = 555u,
            SortOrder = 5,
            Flags = CatalogTagFlags.Visible
        };

        var stream = new MemoryStream();

        // Act
        await resource.SaveToStreamAsync(stream);

        // Assert
        Assert.True(stream.Length > 0);
        Assert.False(resource.IsModified);
    }

    [Fact]
    public async Task LoadFromStreamAsync_NullStream_ThrowsArgumentNullException()
    {
        // Arrange
        var resource = new CatalogTagResource();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => resource.LoadFromStreamAsync(null!));
    }

    [Fact]
    public async Task SaveToStreamAsync_NullStream_ThrowsArgumentNullException()
    {
        // Arrange
        var resource = new CatalogTagResource();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => resource.SaveToStreamAsync(null!));
    }

    #endregion

    #region IContentFields Tests

    [Fact]
    public void ContentFields_ReturnsExpectedFieldNames()
    {
        // Arrange
        var resource = new CatalogTagResource();

        // Act
        var contentFields = resource.ContentFields;

        // Assert
        Assert.Contains(nameof(CatalogTagResource.TagId), contentFields);
        Assert.Contains(nameof(CatalogTagResource.TagName), contentFields);
        Assert.Contains(nameof(CatalogTagResource.Category), contentFields);
        Assert.Contains(nameof(CatalogTagResource.Flags), contentFields);
        Assert.Contains(nameof(CatalogTagResource.ParentTagId), contentFields);
        Assert.Contains(nameof(CatalogTagResource.Description), contentFields);
        Assert.Contains(nameof(CatalogTagResource.SortOrder), contentFields);
        Assert.Contains(nameof(CatalogTagResource.ChildTagIds), contentFields);
        Assert.Contains(nameof(CatalogTagResource.FilterCriteria), contentFields);
    }

    [Theory]
    [InlineData(0, nameof(CatalogTagResource.TagId))]
    [InlineData(1, nameof(CatalogTagResource.TagName))]
    [InlineData(2, nameof(CatalogTagResource.Category))]
    public void IndexerByInt_ValidIndex_ReturnsCorrectValue(int index, string expectedFieldName)
    {
        // Arrange
        var resource = new CatalogTagResource(123u, "TestTag", CatalogTagCategory.Function);

        // Act
        var typedValue = resource[index];

        // Assert
        // Verify we're getting the correct field by checking the field name matches
        var actualFieldName = resource.ContentFields[index];
        Assert.Equal(expectedFieldName, actualFieldName);
    }

    [Fact]
    public void IndexerByInt_InvalidIndex_ReturnsNullTypedValue()
    {
        // Arrange
        var resource = new CatalogTagResource();

        // Act
        var typedValue = resource[-1];

        // Assert
        Assert.Equal(typeof(object), typedValue.Type);
        Assert.Null(typedValue.Value);
    }

    [Fact]
    public void IndexerByString_TagId_ReturnsCorrectValue()
    {
        // Arrange
        var resource = new CatalogTagResource { TagId = 999u };

        // Act
        var typedValue = resource[nameof(CatalogTagResource.TagId)];

        // Assert
        Assert.Equal(typeof(uint), typedValue.Type);
        Assert.Equal(999u, typedValue.Value);
    }

    [Fact]
    public void IndexerByString_TagName_ReturnsCorrectValue()
    {
        // Arrange
        var resource = new CatalogTagResource { TagName = "TestTagName" };

        // Act
        var typedValue = resource[nameof(CatalogTagResource.TagName)];

        // Assert
        Assert.Equal(typeof(string), typedValue.Type);
        Assert.Equal("TestTagName", typedValue.Value);
    }

    [Fact]
    public void IndexerByString_InvalidFieldName_ReturnsNullTypedValue()
    {
        // Arrange
        var resource = new CatalogTagResource();

        // Act
        var typedValue = resource["InvalidFieldName"];

        // Assert
        Assert.Equal(typeof(object), typedValue.Type);
        Assert.Null(typedValue.Value);
    }

    #endregion

    #region Resource Interface Tests

    [Fact]
    public void Stream_Access_ThrowsNotSupportedException()
    {
        // Arrange
        var resource = new CatalogTagResource();

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => resource.Stream);
    }

    [Fact]
    public void AsBytes_WithData_ReturnsSerializedData()
    {
        // Arrange
        var resource = new CatalogTagResource(123u, "TestTag", CatalogTagCategory.Function);

        // Act
        var bytes = resource.AsBytes;

        // Assert
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void ApiVersion_Properties_HaveCorrectValues()
    {
        // Arrange
        var resource = new CatalogTagResource();

        // Act & Assert
        Assert.Equal(1, resource.RecommendedApiVersion);
        Assert.Equal(1, resource.RequestedApiVersion);
    }

    [Fact]
    public void ResourceChanged_Event_FiredOnPropertyChanges()
    {
        // Arrange
        var resource = new CatalogTagResource();
        var eventFired = false;
        resource.ResourceChanged += (_, _) => eventFired = true;

        // Act
        resource.TagName = "Changed";

        // Assert
        Assert.True(eventFired);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_WithData_ClearsCollections()
    {
        // Arrange
        var resource = new CatalogTagResource();
        resource.AddChildTag(123u);
        resource.AddFilterCriterion(new TagFilterCriterion
        {
            PropertyName = "Test",
            Operator = FilterOperator.Equal,
            Value = "Value"
        });

        // Act
        resource.Dispose();

        // Assert
        Assert.Empty(resource.ChildTagIds);
        Assert.Empty(resource.FilterCriteria);
    }

    [Fact]
    public async Task DisposeAsync_WithData_ClearsCollections()
    {
        // Arrange
        var resource = new CatalogTagResource();
        resource.AddChildTag(123u);
        resource.AddFilterCriterion(new TagFilterCriterion
        {
            PropertyName = "Test",
            Operator = FilterOperator.Equal,
            Value = "Value"
        });

        // Act
        await resource.DisposeAsync();

        // Assert
        Assert.Empty(resource.ChildTagIds);
        Assert.Empty(resource.FilterCriteria);
    }

    #endregion
}
