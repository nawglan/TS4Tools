using TS4Tools.Core.Interfaces;

namespace TS4Tools.Core.Interfaces.Resources;

/// <summary>
/// Interface for Environment resources handling weather patterns, seasonal effects, and environmental systems.
/// </summary>
public interface IEnvironmentResource : IResource
{
    /// <summary>
    /// Gets or sets the resource version.
    /// </summary>
    uint Version { get; set; }

    /// <summary>
    /// Gets or sets the current season type.
    /// </summary>
    SeasonType CurrentSeason { get; set; }

    /// <summary>
    /// Gets or sets the season start time.
    /// </summary>
    ulong SeasonStartTime { get; set; }

    /// <summary>
    /// Gets or sets the season GUID.
    /// </summary>
    ulong SeasonGuid { get; set; }

    /// <summary>
    /// Gets the regional weather patterns.
    /// </summary>
    IReadOnlyList<IRegionalWeather> RegionalWeathers { get; }

    /// <summary>
    /// Gets or sets the current moon phase.
    /// </summary>
    MoonPhase CurrentMoonPhase { get; set; }

    /// <summary>
    /// Gets or sets whether environment changes should be skipped.
    /// </summary>
    bool SkipEnvironmentChanges { get; set; }

    /// <summary>
    /// Gets or sets the current temperature.
    /// </summary>
    float Temperature { get; set; }

    /// <summary>
    /// Gets or sets the current humidity level.
    /// </summary>
    float Humidity { get; set; }

    /// <summary>
    /// Gets or sets the current wind speed.
    /// </summary>
    float WindSpeed { get; set; }

    /// <summary>
    /// Gets or sets whether it's currently raining.
    /// </summary>
    bool IsRaining { get; set; }

    /// <summary>
    /// Gets or sets whether it's currently snowing.
    /// </summary>
    bool IsSnowing { get; set; }

    /// <summary>
    /// Gets or sets whether thunder is currently active.
    /// </summary>
    bool ThunderActive { get; set; }

    /// <summary>
    /// Gets the weather forecast update data.
    /// </summary>
    IReadOnlyList<ulong> WeatherForecastIds { get; }

    /// <summary>
    /// Gets the current weather types.
    /// </summary>
    IReadOnlyList<long> WeatherTypes { get; }

    /// <summary>
    /// Gets the weather interpolations.
    /// </summary>
    IReadOnlyList<IWeatherInterpolation> WeatherInterpolations { get; }

    /// <summary>
    /// Adds a regional weather pattern to the environment.
    /// </summary>
    /// <param name="regionalWeather">The regional weather to add.</param>
    void AddRegionalWeather(IRegionalWeather regionalWeather);

    /// <summary>
    /// Removes a regional weather pattern from the environment.
    /// </summary>
    /// <param name="regionId">The region ID to remove weather for.</param>
    /// <returns>True if the regional weather was removed, false otherwise.</returns>
    bool RemoveRegionalWeather(ulong regionId);

    /// <summary>
    /// Removes a regional weather pattern from the environment.
    /// </summary>
    /// <param name="regionalWeather">The regional weather to remove.</param>
    /// <returns>True if the regional weather was removed, false otherwise.</returns>
    bool RemoveRegionalWeather(IRegionalWeather regionalWeather);

    /// <summary>
    /// Gets regional weather by region ID.
    /// </summary>
    /// <param name="regionId">The region ID to get weather for.</param>
    /// <returns>The regional weather if found, null otherwise.</returns>
    IRegionalWeather? GetRegionalWeather(ulong regionId);

    /// <summary>
    /// Updates the weather forecast data.
    /// </summary>
    /// <param name="forecastIds">The forecast instance IDs.</param>
    void UpdateWeatherForecast(IEnumerable<ulong> forecastIds);

    /// <summary>
    /// Updates the current weather types.
    /// </summary>
    /// <param name="weatherTypes">The weather type enums.</param>
    void UpdateWeatherTypes(IEnumerable<long> weatherTypes);

    /// <summary>
    /// Gets regional weather data by region ID.
    /// </summary>
    /// <param name="regionId">The region ID</param>
    /// <returns>The regional weather data, or null if not found</returns>
    IRegionalWeather? GetRegionalWeatherByRegionId(ulong regionId);

    /// <summary>
    /// Adds a weather interpolation.
    /// </summary>
    /// <param name="interpolation">The interpolation to add</param>
    void AddWeatherInterpolation(IWeatherInterpolation interpolation);

    /// <summary>
    /// Removes a weather interpolation.
    /// </summary>
    /// <param name="interpolation">The interpolation to remove</param>
    /// <returns>True if removed, false if not found</returns>
    bool RemoveWeatherInterpolation(IWeatherInterpolation interpolation);

    /// <summary>
    /// Gets weather forecast data.
    /// </summary>
    /// <param name="forecastId">The forecast ID</param>
    /// <returns>Collection of weather interpolations for the forecast</returns>
    IEnumerable<IWeatherInterpolation> GetWeatherForecast(ulong forecastId);

    /// <summary>
    /// Updates weather conditions.
    /// </summary>
    /// <param name="temperature">Temperature value</param>
    /// <param name="humidity">Humidity value</param>
    /// <param name="windSpeed">Wind speed value</param>
    void UpdateWeatherConditions(float temperature, float humidity, float windSpeed);

    /// <summary>
    /// Checks if a weather event is active.
    /// </summary>
    /// <param name="eventType">The event type to check</param>
    /// <returns>True if the event is active</returns>
    bool IsWeatherEvent(string eventType);

    /// <summary>
    /// Saves the resource to a stream.
    /// </summary>
    /// <param name="stream">The stream to save to</param>
    void Save(Stream stream);

    /// <summary>
    /// Saves the resource asynchronously.
    /// </summary>
    /// <returns>Task representing the save operation</returns>
    Task SaveAsync();

    /// <summary>
    /// Loads the resource from a stream asynchronously.
    /// </summary>
    /// <param name="stream">The stream to load from</param>
    /// <returns>Task representing the load operation</returns>
    Task LoadAsync(Stream stream);
}

/// <summary>
/// Represents regional weather data.
/// </summary>
public interface IRegionalWeather
{
    /// <summary>
    /// Gets or sets the region identifier.
    /// </summary>
    ulong RegionId { get; set; }

    /// <summary>
    /// Gets or sets the weather event identifier.
    /// </summary>
    ulong WeatherEvent { get; set; }

    /// <summary>
    /// Gets or sets the forecast timestamp.
    /// </summary>
    ulong ForecastTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the next weather event time.
    /// </summary>
    ulong NextWeatherEventTime { get; set; }

    /// <summary>
    /// Gets the weather interpolations for this region.
    /// </summary>
    IReadOnlyList<IWeatherInterpolation> WeatherInterpolations { get; }

    /// <summary>
    /// Gets the forecast data.
    /// </summary>
    IReadOnlyList<ulong> Forecasts { get; }

    /// <summary>
    /// Gets or sets the override forecast value.
    /// </summary>
    ulong? OverrideForecast { get; set; }

    /// <summary>
    /// Gets or sets the override forecast season stamp.
    /// </summary>
    ulong? OverrideForecastSeasonStamp { get; set; }

    /// <summary>
    /// Gets or sets the temperature.
    /// </summary>
    float Temperature { get; set; }

    /// <summary>
    /// Gets or sets the humidity level.
    /// </summary>
    float Humidity { get; set; }

    /// <summary>
    /// Gets or sets the wind speed.
    /// </summary>
    float WindSpeed { get; set; }

    /// <summary>
    /// Gets or sets whether it's raining.
    /// </summary>
    bool IsRaining { get; set; }

    /// <summary>
    /// Adds a weather interpolation to this region.
    /// </summary>
    /// <param name="interpolation">The weather interpolation to add.</param>
    void AddWeatherInterpolation(IWeatherInterpolation interpolation);

    /// <summary>
    /// Updates the forecast data.
    /// </summary>
    /// <param name="forecasts">The forecast values.</param>
    void UpdateForecasts(IEnumerable<ulong> forecasts);
}

/// <summary>
/// Represents weather interpolation data for seasonal transitions.
/// </summary>
public interface IWeatherInterpolation
{
    /// <summary>
    /// Gets or sets the weather interpolation type.
    /// </summary>
    WeatherInterpolationType MessageType { get; set; }

    /// <summary>
    /// Gets or sets the starting value for the interpolation.
    /// </summary>
    float StartValue { get; set; }

    /// <summary>
    /// Gets or sets the start time for the interpolation.
    /// </summary>
    ulong StartTime { get; set; }

    /// <summary>
    /// Gets or sets the ending value for the interpolation.
    /// </summary>
    float EndValue { get; set; }

    /// <summary>
    /// Gets or sets the end time for the interpolation.
    /// </summary>
    ulong EndTime { get; set; }

    /// <summary>
    /// Gets or sets the interpolation type.
    /// </summary>
    WeatherInterpolationType InterpolationType { get; set; }

    /// <summary>
    /// Gets or sets the temperature start value.
    /// </summary>
    float TemperatureStart { get; set; }

    /// <summary>
    /// Gets or sets the temperature end value.
    /// </summary>
    float TemperatureEnd { get; set; }
}

/// <summary>
/// Season types for environmental systems.
/// </summary>
public enum SeasonType
{
    /// <summary>
    /// Summer season with warm weather and long days.
    /// </summary>
    Summer = 0,

    /// <summary>
    /// Fall/Autumn season with cooling weather and changing foliage.
    /// </summary>
    Fall = 1,

    /// <summary>
    /// Winter season with cold weather and potential snow.
    /// </summary>
    Winter = 2,

    /// <summary>
    /// Spring season with warming weather and new growth.
    /// </summary>
    Spring = 3
}

/// <summary>
/// Moon phases for lunar environmental effects.
/// </summary>
public enum MoonPhase
{
    /// <summary>
    /// Default state with no visible moon.
    /// </summary>
    DefaultNoMoon = -1,

    /// <summary>
    /// New moon phase - moon is not visible.
    /// </summary>
    NewMoon = 0,

    /// <summary>
    /// Waxing crescent - small crescent visible.
    /// </summary>
    WaxingCrescent = 1,

    /// <summary>
    /// First quarter - half moon visible.
    /// </summary>
    FirstQuarter = 2,

    /// <summary>
    /// Waxing gibbous - mostly full moon visible.
    /// </summary>
    WaxingGibbous = 3,

    /// <summary>
    /// Full moon - completely visible.
    /// </summary>
    FullMoon = 4,

    /// <summary>
    /// Waning gibbous - mostly full moon visible, decreasing.
    /// </summary>
    WaningGibbous = 5,

    /// <summary>
    /// Third quarter - half moon visible, decreasing.
    /// </summary>
    ThirdQuarter = 6,

    /// <summary>
    /// Waning crescent - small crescent visible, decreasing.
    /// </summary>
    WaningCrescent = 7
}

/// <summary>
/// Weather interpolation types for seasonal and weather effects.
/// </summary>
public enum WeatherInterpolationType
{
    /// <summary>Season-based interpolation.</summary>
    Season = 0,
    /// <summary>Leaf accumulation effects.</summary>
    LeafAccumulation = 1,
    /// <summary>Flower growth interpolation.</summary>
    FlowerGrowth = 2,
    /// <summary>Foliage reduction effects.</summary>
    FoliageReduction = 3,
    /// <summary>Foliage color shift effects.</summary>
    FoliageColorshift = 4,
    /// <summary>Rainfall weather interpolation.</summary>
    Rainfall = 1000,
    /// <summary>Snowfall weather interpolation.</summary>
    Snowfall = 1001,
    /// <summary>Rain accumulation on surfaces.</summary>
    RainAccumulation = 1002,
    /// <summary>Snow accumulation on surfaces.</summary>
    SnowAccumulation = 1003,
    /// <summary>Window frost effects.</summary>
    WindowFrost = 1004,
    /// <summary>Water freezing effects.</summary>
    WaterFrozen = 1005,
    /// <summary>Wind strength interpolation.</summary>
    Wind = 1006,
    /// <summary>Temperature interpolation.</summary>
    Temperature = 1007,
    /// <summary>Thunder sound effects.</summary>
    Thunder = 1008,
    /// <summary>Lightning visual effects.</summary>
    Lightning = 1009,
    /// <summary>Snow freshness appearance.</summary>
    SnowFreshness = 1010,
    /// <summary>Story act environmental changes.</summary>
    StoryAct = 1011,
    /// <summary>Ecological footprint effects.</summary>
    EcoFootprint = 1012,
    /// <summary>Acid rain environmental effects.</summary>
    AcidRain = 1013,
    /// <summary>Star Wars Resistance pack weather.</summary>
    StarwarsResistance = 1014,
    /// <summary>Star Wars First Order pack weather.</summary>
    StarwarsFirstOrder = 1015,
    /// <summary>Snow iciness effects.</summary>
    SnowIciness = 1016,
    /// <summary>Partly cloudy skybox.</summary>
    SkyboxPartlyCloudy = 2000,
    /// <summary>Clear skybox.</summary>
    SkyboxClear = 2001,
    /// <summary>Light rain clouds skybox.</summary>
    SkyboxLightRainClouds = 2002,
    /// <summary>Dark rain clouds skybox.</summary>
    SkyboxDarkRainClouds = 2003,
    /// <summary>Light snow clouds skybox.</summary>
    SkyboxLightSnowClouds = 2004,
    /// <summary>Dark snow clouds skybox.</summary>
    SkyboxDarkSnowClouds = 2005,
    /// <summary>Cloudy skybox.</summary>
    SkyboxCloudy = 2006,
    /// <summary>Heatwave skybox effects.</summary>
    SkyboxHeatwave = 2007,
    /// <summary>Strange skybox effects.</summary>
    SkyboxStrange = 2008,
    /// <summary>Very strange skybox effects.</summary>
    SkyboxVeryStrange = 2009,
    /// <summary>Industrial pollution skybox.</summary>
    SkyboxIndustrial = 2010
}
