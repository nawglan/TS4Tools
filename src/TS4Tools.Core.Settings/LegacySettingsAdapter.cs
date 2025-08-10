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
using System.Reflection;
using System.Threading;

namespace TS4Tools.Core.Settings;

/// <summary>
/// Compatibility adapter that provides the same static interface as the legacy s4pi.Settings.Settings class.
/// This allows gradual migration from static settings access to modern dependency injection patterns.
/// 
/// <para>
/// <strong>Migration Path:</strong>
/// <list type="number">
/// <item><description>Use this adapter to maintain compatibility during initial migration</description></item>
/// <item><description>Gradually replace static access with injected <see cref="IApplicationSettingsService"/></description></item>
/// <item><description>Remove this adapter once all code uses dependency injection</description></item>
/// </list>
/// </para>
/// </summary>
/// <remarks>
/// This class uses a thread-safe singleton pattern to provide global access to settings.
/// However, it's recommended to migrate to dependency injection for better testability and maintainability.
/// </remarks>
public static class LegacySettingsAdapter
{
    private static readonly Lazy<ApplicationSettings> _lazySettings = new(() =>
        SettingsServiceExtensions.GetDefaultSettings(),
        LazyThreadSafetyMode.ExecutionAndPublication);

    private static ApplicationSettings Settings => _lazySettings.Value;

    /// <summary>
    /// When true, run extra checks as part of normal operation.
    /// Equivalent to legacy Settings.Checking property.
    /// </summary>
    public static bool Checking => Settings.EnableExtraChecking;

    /// <summary>
    /// When true, assume data is dirty regardless of tracking.
    /// Equivalent to legacy Settings.AsBytesWorkaround property.
    /// </summary>
    public static bool AsBytesWorkaround => Settings.AssumeDataDirty;

    /// <summary>
    /// Indicate whether the wrapper should use TS4 format to decode files.
    /// Equivalent to legacy Settings.IsTS4 property.
    /// </summary>
    public static bool IsTS4 => Settings.UseTS4Format;

    /// <summary>
    /// Reloads settings from configuration sources.
    /// This method forces a refresh of the cached settings.
    /// </summary>
    /// <remarks>
    /// Note: This is a breaking change from the legacy implementation which had no reload capability.
    /// The reload creates a new settings instance, so any previously cached references become stale.
    /// </remarks>
    public static void Reload()
    {
        // Force recreation of the lazy settings instance
        // This is done by reflection to reset the lazy field
        var field = typeof(Lazy<ApplicationSettings>).GetField("m_value",
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (field != null)
        {
            // Reset the lazy value to force reload on next access
            field.SetValue(_lazySettings, null);
        }
    }

    /// <summary>
    /// Gets the full modern settings object for advanced scenarios.
    /// Use this method when you need access to settings not available in the legacy interface.
    /// </summary>
    /// <returns>The complete application settings object.</returns>
    public static ApplicationSettings FullSettings => Settings;
}
