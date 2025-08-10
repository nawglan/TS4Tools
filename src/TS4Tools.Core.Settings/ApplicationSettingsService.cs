/***************************************************************************
 *  Copyright (C) 2025 by the TS4Tools contributors                       *
 *                                                                         *
 *  This file is part of TS4Tools                                         *
 *                                                                         *
 *  TS4Tools is free software: you can redistribute it and/or modify      *
 *  it under the terms of the GNU General Public License as published by  *
 *  the Free Software Foundation, either version 3 of the License, or     *
 *  (at your option) any later version.                                   *
 *                                                                         *
 *  TS4Tools is distributed in the hope that it will be useful,           *
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the          *
 *  GNU General Public License for more details.                          *
 *                                                                         *
 *  You should have received a copy of the GNU General Public License     *
 *  along with TS4Tools. If not, see <http://www.gnu.org/licenses/>.      *
 ***************************************************************************/

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace TS4Tools.Core.Settings;

/// <summary>
/// Implementation of <see cref="IApplicationSettingsService"/> using the modern IOptionsMonitor pattern.
/// Provides reactive settings management with automatic change detection and validation.
/// </summary>
public sealed class ApplicationSettingsService : IApplicationSettingsService, IDisposable
{
    private readonly IOptionsMonitor<ApplicationSettings> _optionsMonitor;
    private readonly IDisposable? _changeSubscription;
    private ApplicationSettings _current;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationSettingsService"/> class.
    /// </summary>
    /// <param name="optionsMonitor">The options monitor for reactive configuration changes.</param>
    /// <exception cref="ArgumentNullException">Thrown when optionsMonitor is null.</exception>
    public ApplicationSettingsService(IOptionsMonitor<ApplicationSettings> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _current = _optionsMonitor.CurrentValue;

        // Subscribe to configuration changes for reactive updates
        _changeSubscription = _optionsMonitor.OnChange(OnSettingsChanged);
    }

    /// <inheritdoc />
    public ApplicationSettings Current
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _current;
        }
    }

    /// <inheritdoc />
    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    /// <inheritdoc />
    public Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // IOptionsMonitor automatically handles reloading from configuration sources
        // This method provides explicit reload capability for scenarios where
        // immediate reload is required
        var newSettings = _optionsMonitor.CurrentValue;
        if (!ReferenceEquals(_current, newSettings))
        {
            OnSettingsChanged(newSettings);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles settings change notifications from the options monitor.
    /// </summary>
    /// <param name="newSettings">The new settings values.</param>
    private void OnSettingsChanged(ApplicationSettings newSettings)
    {
        if (_disposed)
            return;

        var previousSettings = _current;
        _current = newSettings;

        try
        {
            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(previousSettings, newSettings));
        }
        catch
        {
            // Prevent exceptions in event handlers from breaking the service
            // In production, this should be logged
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _changeSubscription?.Dispose();
        _disposed = true;
    }
}
