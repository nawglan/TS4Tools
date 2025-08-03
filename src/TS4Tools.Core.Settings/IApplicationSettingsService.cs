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

namespace TS4Tools.Core.Settings;

/// <summary>
/// Service interface for accessing application settings in a modern, testable way.
/// Provides strongly-typed access to configuration values with change notification.
/// </summary>
public interface IApplicationSettingsService
{
    /// <summary>
    /// Gets the current application settings.
    /// </summary>
    ApplicationSettings Current { get; }

    /// <summary>
    /// Event raised when settings are changed and reloaded.
    /// </summary>
    event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    /// <summary>
    /// Reloads settings from the configuration source.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task representing the reload operation.</returns>
    Task ReloadAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Event arguments for settings change notifications.
/// </summary>
public sealed class SettingsChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsChangedEventArgs"/> class.
    /// </summary>
    /// <param name="previousSettings">The previous settings values.</param>
    /// <param name="currentSettings">The current settings values.</param>
    public SettingsChangedEventArgs(ApplicationSettings previousSettings, ApplicationSettings currentSettings)
    {
        PreviousSettings = previousSettings ?? throw new ArgumentNullException(nameof(previousSettings));
        CurrentSettings = currentSettings ?? throw new ArgumentNullException(nameof(currentSettings));
    }

    /// <summary>
    /// Gets the previous settings values.
    /// </summary>
    public ApplicationSettings PreviousSettings { get; }

    /// <summary>
    /// Gets the current settings values.
    /// </summary>
    public ApplicationSettings CurrentSettings { get; }
}
