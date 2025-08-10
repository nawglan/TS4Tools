using System.ComponentModel;

using TS4Tools.Core.Interfaces;

namespace TS4Tools.Resources.Catalog;

/// <summary>
/// Represents a selling point (commodity effect) for an object in the catalog.
/// Selling points define how an object affects Sim needs or provides benefits.
/// </summary>
public readonly record struct SellingPoint : INotifyPropertyChanged
{
    #region Properties

    /// <summary>
    /// Gets the commodity type identifier (e.g., Fun, Comfort, Energy).
    /// </summary>
    [ElementPriority(0)]
    public ushort Commodity { get; init; }

    /// <summary>
    /// Gets the amount of the commodity effect (positive values increase the need).
    /// </summary>
    [ElementPriority(1)]
    public uint Amount { get; init; }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="SellingPoint"/> struct.
    /// </summary>
    /// <param name="commodity">The commodity type identifier.</param>
    /// <param name="amount">The amount of the commodity effect.</param>
    public SellingPoint(ushort commodity, uint amount)
    {
        Commodity = commodity;
        Amount = amount;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets a string representation of the selling point.
    /// </summary>
    /// <returns>A formatted string containing commodity and amount information.</returns>
    public override string ToString()
    {
        return $"SellingPoint(Commodity={Commodity}, Amount={Amount})";
    }

    /// <summary>
    /// Deconstructs the selling point into its components.
    /// </summary>
    /// <param name="commodity">The commodity type identifier.</param>
    /// <param name="amount">The amount of the commodity effect.</param>
    public void Deconstruct(out ushort commodity, out uint amount)
    {
        commodity = Commodity;
        amount = Amount;
    }

    #endregion

    #region INotifyPropertyChanged Implementation

    /// <inheritdoc />
    /// <remarks>
    /// This event is never raised for this record struct since it's immutable,
    /// but is implemented to satisfy interface requirements for UI binding.
    /// </remarks>
    public event PropertyChangedEventHandler? PropertyChanged { add { } remove { } }

    #endregion
}
