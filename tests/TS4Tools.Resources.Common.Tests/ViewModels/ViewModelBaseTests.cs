using FluentAssertions;
using TS4Tools.Resources.Common.ViewModels;
using Xunit;

namespace TS4Tools.Resources.Common.Tests.ViewModels;

public class ViewModelBaseTests
{
    private sealed class TestViewModel : ViewModelBase
    {
        public string _testProperty = string.Empty; // Make public for test access
        private int _intProperty;

        public string TestProperty
        {
            get => _testProperty;
            set => SetProperty(ref _testProperty, value);
        }

        public int IntProperty
        {
            get => _intProperty;
            set => SetProperty(ref _intProperty, value);
        }

        public bool ActionExecuted { get; set; } // Make setter public

        public string TestPropertyWithAction
        {
            get => _testProperty;
            set => SetProperty(ref _testProperty, value, () => ActionExecuted = true);
        }

        public void TriggerPropertyChanged(string propertyName)
        {
            OnPropertyChanged(propertyName);
        }

        public void TriggerMultiplePropertiesChanged(params string[] propertyNames)
        {
            OnPropertiesChanged(propertyNames);
        }

        // Public wrapper method for testing SetProperty
        public bool TestSetProperty<T>(ref T field, T value, string? propertyName = null)
        {
            return SetProperty(ref field, value, propertyName);
        }
    }

    [Fact]
    public void SetProperty_WithDifferentValue_RaisesPropertyChanged()
    {
        // Arrange
        var viewModel = new TestViewModel();
        var propertyChangedRaised = false;
        string? changedPropertyName = null;

        viewModel.PropertyChanged += (sender, e) =>
        {
            propertyChangedRaised = true;
            changedPropertyName = e.PropertyName;
        };

        // Act
        viewModel.TestProperty = "New Value";

        // Assert
        propertyChangedRaised.Should().BeTrue();
        changedPropertyName.Should().Be(nameof(TestViewModel.TestProperty));
        viewModel.TestProperty.Should().Be("New Value");
    }

    [Fact]
    public void SetProperty_WithSameValue_DoesNotRaisePropertyChanged()
    {
        // Arrange
        var viewModel = new TestViewModel();
        viewModel.TestProperty = "Initial Value";
        
        var propertyChangedRaised = false;
        viewModel.PropertyChanged += (sender, e) => propertyChangedRaised = true;

        // Act
        viewModel.TestProperty = "Initial Value";

        // Assert
        propertyChangedRaised.Should().BeFalse();
    }

    [Fact]
    public void SetProperty_ReturnsTrue_WhenValueChanges()
    {
        // Arrange
        var viewModel = new TestViewModel();
        var testField = "Initial";

        // Act
        var result = viewModel.TestSetProperty(ref testField, "New Value");

        // Assert
        result.Should().BeTrue();
        testField.Should().Be("New Value");
    }

    [Fact]
    public void SetProperty_ReturnsFalse_WhenValueDoesNotChange()
    {
        // Arrange
        var viewModel = new TestViewModel();
        var testField = "Same Value";

        // Act
        var result = viewModel.TestSetProperty(ref testField, "Same Value");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SetProperty_WithAction_ExecutesActionWhenValueChanges()
    {
        // Arrange
        var viewModel = new TestViewModel();

        // Act
        viewModel.TestPropertyWithAction = "New Value";

        // Assert
        viewModel.ActionExecuted.Should().BeTrue();
    }

    [Fact]
    public void SetProperty_WithAction_DoesNotExecuteActionWhenValueDoesNotChange()
    {
        // Arrange
        var viewModel = new TestViewModel();
        viewModel.TestPropertyWithAction = "Initial Value";
        viewModel.ActionExecuted = false; // Reset flag

        // Act
        viewModel.TestPropertyWithAction = "Initial Value";

        // Assert
        viewModel.ActionExecuted.Should().BeFalse();
    }

    [Fact]
    public void OnPropertyChanged_RaisesPropertyChangedEvent()
    {
        // Arrange
        var viewModel = new TestViewModel();
        var propertyChangedRaised = false;
        string? changedPropertyName = null;

        viewModel.PropertyChanged += (sender, e) =>
        {
            propertyChangedRaised = true;
            changedPropertyName = e.PropertyName;
        };

        // Act
        viewModel.TriggerPropertyChanged("TestPropertyName");

        // Assert
        propertyChangedRaised.Should().BeTrue();
        changedPropertyName.Should().Be("TestPropertyName");
    }

    [Fact]
    public void OnPropertiesChanged_RaisesPropertyChangedForMultipleProperties()
    {
        // Arrange
        var viewModel = new TestViewModel();
        var changedProperties = new List<string>();

        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName != null)
                changedProperties.Add(e.PropertyName);
        };

        // Act
        viewModel.TriggerMultiplePropertiesChanged("Property1", "Property2", "Property3");

        // Assert
        changedProperties.Should().HaveCount(3);
        changedProperties.Should().Contain("Property1");
        changedProperties.Should().Contain("Property2");
        changedProperties.Should().Contain("Property3");
    }

    [Fact]
    public void PropertyChanged_WithDifferentValueTypes_WorksCorrectly()
    {
        // Arrange
        var viewModel = new TestViewModel();
        var propertyChangedCount = 0;

        viewModel.PropertyChanged += (sender, e) => propertyChangedCount++;

        // Act
        viewModel.TestProperty = "String Value";
        viewModel.IntProperty = 42;

        // Assert
        propertyChangedCount.Should().Be(2);
        viewModel.TestProperty.Should().Be("String Value");
        viewModel.IntProperty.Should().Be(42);
    }
}
