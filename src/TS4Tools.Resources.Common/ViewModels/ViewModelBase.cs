using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TS4Tools.Resources.Common.ViewModels;

/// <summary>
/// Base class for ViewModels providing property change notification.
/// Modern implementation using CallerMemberName for automatic property naming.
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the PropertyChanged event for the specified property.
    /// </summary>
    /// <param name="propertyName">The name of the property. When called from a property setter, this is automatically populated.</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Sets the field value and raises PropertyChanged if the value changed.
    /// </summary>
    /// <typeparam name="T">The type of the property.</typeparam>
    /// <param name="field">Reference to the backing field.</param>
    /// <param name="value">The new value.</param>
    /// <param name="propertyName">The name of the property. When called from a property setter, this is automatically populated.</param>
    /// <returns>True if the value changed and PropertyChanged was raised; otherwise, false.</returns>
#pragma warning disable CA1045 // Do not pass types by reference - This is the standard pattern for property setters in view models
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
#pragma warning restore CA1045
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// Sets the field value, raises PropertyChanged if the value changed, and executes an additional action.
    /// </summary>
    /// <typeparam name="T">The type of the property.</typeparam>
    /// <param name="field">Reference to the backing field.</param>
    /// <param name="value">The new value.</param>
    /// <param name="onChanged">Action to execute when the value changes.</param>
    /// <param name="propertyName">The name of the property. When called from a property setter, this is automatically populated.</param>
    /// <returns>True if the value changed and PropertyChanged was raised; otherwise, false.</returns>
#pragma warning disable CA1045 // Do not pass types by reference - This is the standard pattern for property setters in view models
    protected bool SetProperty<T>(ref T field, T value, Action onChanged, [CallerMemberName] string? propertyName = null)
#pragma warning restore CA1045
    {
        if (!SetProperty(ref field, value, propertyName))
            return false;

        onChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Raises PropertyChanged for multiple properties.
    /// Useful when one property change affects multiple computed properties.
    /// </summary>
    /// <param name="propertyNames">The names of the properties that changed.</param>
    protected void OnPropertiesChanged(params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            OnPropertyChanged(propertyName);
        }
    }
}
