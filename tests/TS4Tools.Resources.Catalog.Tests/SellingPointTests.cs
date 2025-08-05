using FluentAssertions;
using TS4Tools.Resources.Catalog;
using Xunit;

namespace TS4Tools.Resources.Catalog.Tests;

public sealed class SellingPointTests
{
    [Theory]
    [InlineData((ushort)0, 0u)]
    [InlineData((ushort)1, 100u)]
    [InlineData((ushort)999, 12345u)]
    [InlineData((ushort)65535, uint.MaxValue)]
    public void Constructor_WithValidValues_ShouldInitializeCorrectly(ushort commodity, uint amount)
    {
        // Act
        var sellingPoint = new SellingPoint(commodity, amount);
        
        // Assert
        sellingPoint.Commodity.Should().Be(commodity);
        sellingPoint.Amount.Should().Be(amount);
    }
    
    [Fact]
    public void DefaultConstructor_ShouldInitializeWithZeros()
    {
        // Act
        var sellingPoint = new SellingPoint();
        
        // Assert
        sellingPoint.Commodity.Should().Be(0);
        sellingPoint.Amount.Should().Be(0u);
    }
    
    [Theory]
    [InlineData((ushort)1, 50u, "SellingPoint(Commodity=1, Amount=50)")]
    [InlineData((ushort)999, 12345u, "SellingPoint(Commodity=999, Amount=12345)")]
    [InlineData((ushort)0, 0u, "SellingPoint(Commodity=0, Amount=0)")]
    public void ToString_ShouldReturnFormattedString(ushort commodity, uint amount, string expected)
    {
        // Arrange
        var sellingPoint = new SellingPoint(commodity, amount);
        
        // Act
        var result = sellingPoint.ToString();
        
        // Assert
        result.Should().Be(expected);
    }
    
    [Fact]
    public void Deconstruct_ShouldReturnCorrectValues()
    {
        // Arrange
        var originalCommodity = (ushort)123;
        var originalAmount = 456u;
        var sellingPoint = new SellingPoint(originalCommodity, originalAmount);
        
        // Act
        var (commodity, amount) = sellingPoint;
        
        // Assert
        commodity.Should().Be(originalCommodity);
        amount.Should().Be(originalAmount);
    }
    
    [Fact]
    public void Equals_WithIdenticalValues_ShouldReturnTrue()
    {
        // Arrange
        var sellingPoint1 = new SellingPoint(100, 200);
        var sellingPoint2 = new SellingPoint(100, 200);
        
        // Act & Assert
        sellingPoint1.Equals(sellingPoint2).Should().BeTrue();
        (sellingPoint1 == sellingPoint2).Should().BeTrue();
        (sellingPoint1 != sellingPoint2).Should().BeFalse();
        sellingPoint1.GetHashCode().Should().Be(sellingPoint2.GetHashCode());
    }
    
    [Theory]
    [InlineData((ushort)100, 200u, (ushort)100, 201u)] // Different amount
    [InlineData((ushort)100, 200u, (ushort)101, 200u)] // Different commodity
    [InlineData((ushort)100, 200u, (ushort)101, 201u)] // Both different
    public void Equals_WithDifferentValues_ShouldReturnFalse(ushort commodity1, uint amount1, ushort commodity2, uint amount2)
    {
        // Arrange
        var sellingPoint1 = new SellingPoint(commodity1, amount1);
        var sellingPoint2 = new SellingPoint(commodity2, amount2);
        
        // Act & Assert
        sellingPoint1.Equals(sellingPoint2).Should().BeFalse();
        (sellingPoint1 == sellingPoint2).Should().BeFalse();
        (sellingPoint1 != sellingPoint2).Should().BeTrue();
    }
    
    [Fact]
    public void PropertyChanged_ShouldNeverRaise()
    {
        // Arrange
        var sellingPoint = new SellingPoint(1, 100);
        var eventRaised = false;
        
        // Act
        sellingPoint.PropertyChanged += (_, _) => eventRaised = true;
        
        // Assert
        eventRaised.Should().BeFalse("PropertyChanged should never be raised for immutable record structs");
    }
    
    [Fact]
    public void ImplicitConversion_ToTuple_ShouldWork()
    {
        // Arrange
        var sellingPoint = new SellingPoint(123, 456);
        
        // Act
        (ushort commodity, uint amount) = sellingPoint;
        
        // Assert
        commodity.Should().Be(123);
        amount.Should().Be(456u);
    }
    
    [Theory]
    [InlineData((ushort)0, 0u)]
    [InlineData((ushort)1, 1u)]
    [InlineData((ushort)65535, uint.MaxValue)]
    public void EdgeValues_ShouldBeHandledCorrectly(ushort commodity, uint amount)
    {
        // Act
        var sellingPoint = new SellingPoint(commodity, amount);
        
        // Assert
        sellingPoint.Commodity.Should().Be(commodity);
        sellingPoint.Amount.Should().Be(amount);
    }
    
    [Fact]
    public void RecordEquality_ShouldWorkCorrectly()
    {
        // Arrange
        var sellingPoint1 = new SellingPoint(50, 75);
        var sellingPoint2 = new SellingPoint(50, 75);
        var sellingPoint3 = new SellingPoint(51, 75);
        
        // Act & Assert
        // Test equality
        sellingPoint1.Should().Be(sellingPoint2);
        sellingPoint1.Should().NotBe(sellingPoint3);
        
        // Test hash codes
        sellingPoint1.GetHashCode().Should().Be(sellingPoint2.GetHashCode());
        sellingPoint1.GetHashCode().Should().NotBe(sellingPoint3.GetHashCode());
    }
    
    [Fact]
    public void SellingPoint_ShouldBeValueType()
    {
        // Arrange & Act
        var sellingPoint1 = new SellingPoint(1, 100);
        var sellingPoint2 = sellingPoint1; // Copy
        
        // Assert
        // Both should have the same values (value semantics)
        sellingPoint1.Should().Be(sellingPoint2);
        sellingPoint1.Equals(sellingPoint2).Should().BeTrue("Should have value semantics");
    }
    
    [Fact]
    public void Collections_ShouldWorkCorrectly()
    {
        // Arrange
        var sellingPoints = new[]
        {
            new SellingPoint(1, 50),
            new SellingPoint(2, 75),
            new SellingPoint(3, 100)
        };
        
        var list = new List<SellingPoint>(sellingPoints);
        var set = new HashSet<SellingPoint>(sellingPoints);
        
        // Act & Assert
        list.Should().HaveCount(3);
        set.Should().HaveCount(3);
        
        // Test contains
        list.Should().Contain(new SellingPoint(2, 75));
        set.Should().Contain(new SellingPoint(2, 75));
        
        // Test not contains
        list.Should().NotContain(new SellingPoint(2, 76));
        set.Should().NotContain(new SellingPoint(2, 76));
    }
}
