# Package Analysis Script

This script provides comprehensive analysis of all .package files in a Sims 4 game directory. It identifies and analyzes resources within each package, reports on parsing success rates, and identifies unknown resource types.

## Features

- **Comprehensive Package Scanning**: Finds and analyzes all .package files in the Sims 4 installation directory
- **Resource Type Identification**: Uses the TS4Tools resource type registry to identify known vs unknown resource types
- **Parsing Success Analysis**: Attempts to parse each resource and reports success/failure rates
- **Performance Optimized**: Uses parallel processing for analyzing multiple packages simultaneously
- **Detailed Reporting**: Generates both console output and a detailed JSON report

## Output

The script provides several types of output:

### 1. Package Analysis Results
Shows each package file with:
- Package file name
- Number of resources
- File size
- Analysis status (Success/Failed)

### 2. Summary Statistics
- Total packages found and analyzed
- Success/failure rates for package loading
- Total resources found
- Resource parsing success rates
- Overall identification success rates

### 3. Resource Type Analysis
For each resource type found:
- Resource type ID (hex format)
- Type name (if known)
- Count of resources
- Total size
- Parsing success rate
- Known/Unknown status

### 4. Unknown Resource Types
- List of unknown resource type IDs
- Count of occurrences for each unknown type
- Most common unknown types

### 5. JSON Report
Detailed JSON report saved to `PackageAnalysisReport.json` containing:
- Complete summary statistics
- Detailed resource type analysis
- Unknown resource types list
- Timestamps and metadata

## Usage

1. **Configure the game directory**: Ensure your `appsettings.json` file has the correct path to your Sims 4 installation:
   ```json
   {
     "ApplicationSettings": {
       "Game": {
         "InstallationDirectory": "/path/to/your/sims4/installation"
       }
     }
   }
   ```

2. **Build the project**:
   ```bash
   dotnet build
   ```

3. **Run the analysis**:
   ```bash
   dotnet run
   ```

## Configuration

The script uses the standard TS4Tools configuration from `appsettings.json`. Key settings:

- `ApplicationSettings.Game.InstallationDirectory`: Path to Sims 4 installation directory
- `ApplicationSettings.EnableDetailedLogging`: Enable detailed logging for debugging
- `Logging.LogLevel`: Control log verbosity

## Performance Considerations

- The script uses parallel processing to improve performance
- Concurrent package analysis is limited to the number of CPU cores
- Large game installations may take several minutes to analyze completely
- Memory usage is optimized by processing packages individually

## Understanding the Results

### Success Rates
- **Parse Success Rate**: Percentage of resources that could be loaded without errors
- **Full Identification Success Rate**: Percentage of resources that are both parseable and fully identified

### Resource Status
- **Known**: Resource type is recognized by TS4Tools
- **Unknown**: Resource type is not yet implemented in TS4Tools
- **Successfully Parsed**: Resource could be loaded and basic data accessed
- **Fully Identified**: Resource is completely understood and can be fully processed

### Common Unknown Types
The script will highlight the most frequently occurring unknown resource types, which can help prioritize implementation of new resource wrappers.

## Troubleshooting

### Game Directory Not Found
Ensure the `InstallationDirectory` in `appsettings.json` points to the correct Sims 4 installation directory.

### Permission Errors
Ensure the script has read permission for the Sims 4 installation directory and all subdirectories.

### Out of Memory Errors
For very large game installations, consider:
- Closing other applications to free memory
- Running the analysis on a subset of packages first
- Increasing system virtual memory

### Performance Issues
- Reduce concurrent processing by modifying the semaphore limit in the code
- Run on an SSD for better I/O performance
- Ensure sufficient free disk space for temporary files

## Output Files

- **Console Output**: Real-time progress and summary statistics
- **PackageAnalysisReport.json**: Detailed JSON report for further analysis
- **Log files**: Detailed logs if logging is enabled in configuration

## Next Steps

Use the results to:
1. Identify which resource types need implementation
2. Prioritize development based on frequency of unknown types
3. Validate existing resource parsers
4. Monitor parsing success rates over time
5. Generate statistics for documentation and development planning
