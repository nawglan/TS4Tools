# ADR-006: Golden Master Testing Strategy

**Status:** Accepted
**Date:** August 8, 2025
**Deciders:** Architecture Team, QA Lead, Senior Developers

## Context

The greenfield migration of TS4Tools requires absolute certainty that the new implementation produces byte-identical results to the legacy system. Without an existing comprehensive test suite, we risk introducing subtle compatibility issues that could break existing workflows or corrupt user data.

The legacy Sims4Tools codebase processes complex binary package formats (DBPF) with dozens of resource types, compression algorithms, and intricate parsing logic developed over many years. Any deviation could result in:

- Package corruption
- Loss of modding community compatibility
- Subtle behavioral differences that surface only with specific file combinations
- Performance regressions with large files

## Decision

We will implement a comprehensive **Golden Master Testing Strategy** as the primary validation mechanism for migration compatibility, ensuring byte-perfect compatibility between legacy and modern implementations.

## Rationale

### Why Golden Master Testing is Critical

**Golden Master Testing** (also called Characterization Testing) captures the current behavior of a system and uses it as a reference for future implementations. This approach is essential when:

- âœ… No existing test suite exists (our situation)
- âœ… Behavior must be preserved exactly
- âœ… Complex business logic needs migration (114+ projects)
- âœ… Risk of subtle regressions is high
- âœ… Domain expertise is encoded in implementation details

### Traditional Testing vs Golden Master

| Approach | Pros | Cons | Fit for Migration |
|----------|------|------|------------------|
| **Unit Testing** | Fast, isolated, good coverage | Requires known expected behavior | âŒ No existing specifications |
| **Integration Testing** | Tests workflows, realistic scenarios | Complex setup, slower feedback | âš ï¸ Useful but insufficient |
| **Golden Master Testing** | Captures exact current behavior, comprehensive | Requires stable reference system | âœ… **Perfect for migration** |

## Implementation Strategy

### Phase 1: Test Data Collection

```csharp
public class TestDataCollector
{
    private readonly string _sims4InstallPath;
    private readonly ILogger<TestDataCollector> _logger;

    public async Task<TestPackageCollection> CollectRealWorldPackagesAsync()
    {
        var collection = new TestPackageCollection();

        // Collect official game packages
        var gamePackages = await CollectGamePackagesAsync();
        collection.AddRange(gamePackages);

        // Collect community-created packages (with permission)
        var communityPackages = await CollectCommunityPackagesAsync();
        collection.AddRange(communityPackages);

        // Generate synthetic edge cases
        var syntheticPackages = await GenerateSyntheticTestCasesAsync();
        collection.AddRange(syntheticPackages);

        _logger.LogInformation("Collected {Count} test packages totaling {Size:F1} MB",
            collection.Count, collection.TotalSizeBytes / 1024.0 / 1024.0);

        return collection;
    }

    private async Task<IEnumerable<TestPackage>> CollectGamePackagesAsync()
    {
        var packages = new List<TestPackage>();
        var gameDataPath = Path.Combine(_sims4InstallPath, "Data", "Client");

        if (!Directory.Exists(gameDataPath))
        {
            _logger.LogWarning("Game data path not found: {Path}", gameDataPath);
            return packages;
        }

        foreach (var packageFile in Directory.GetFiles(gameDataPath, "*.package"))
        {
            try
            {
                var fileInfo = new FileInfo(packageFile);
                packages.Add(new TestPackage
                {
                    Name = Path.GetFileName(packageFile),
                    Path = packageFile,
                    SizeBytes = fileInfo.Length,
                    Source = PackageSource.Official,
                    Category = ClassifyPackage(packageFile)
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process package: {Path}", packageFile);
            }
        }

        return packages.OrderBy(p => p.SizeBytes).ToList(); // Test small files first
    }
}

public class TestPackage
{
    public string Name { get; set; }
    public string Path { get; set; }
    public long SizeBytes { get; set; }
    public PackageSource Source { get; set; }
    public PackageCategory Category { get; set; }
    public List<string> KnownIssues { get; set; } = new();
}

public enum PackageSource { Official, Community, Synthetic }
public enum PackageCategory { Core, Expansion, StuffPack, ModFolder, UserGenerated }
```

### Phase 2: Golden Master Capture

```csharp
public class GoldenMasterGenerator
{
    private readonly ILegacyPackageService _legacyService;
    private readonly ILogger<GoldenMasterGenerator> _logger;

    public async Task<GoldenMasterSuite> GenerateGoldenMastersAsync(
        TestPackageCollection packages,
        IProgress<GenerationProgress> progress = null)
    {
        var suite = new GoldenMasterSuite();
        var totalPackages = packages.Count;

        for (int i = 0; i < totalPackages; i++)
        {
            var package = packages[i];
            progress?.Report(new GenerationProgress(i + 1, totalPackages, package.Name));

            try
            {
                var goldenMaster = await GenerateGoldenMasterAsync(package);
                suite.Add(goldenMaster);

                _logger.LogDebug("Generated golden master for {Package}: {ResourceCount} resources",
                    package.Name, goldenMaster.Resources.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate golden master for {Package}", package.Name);
                suite.AddFailure(package, ex);
            }
        }

        return suite;
    }

    private async Task<PackageGoldenMaster> GenerateGoldenMasterAsync(TestPackage package)
    {
        var goldenMaster = new PackageGoldenMaster
        {
            SourcePackage = package,
            GeneratedAt = DateTimeOffset.UtcNow,
            LegacyVersion = GetLegacyVersion()
        };

        // Load package with legacy implementation
        using var legacyPackage = _legacyService.LoadPackage(package.Path);

        // Capture package-level properties
        goldenMaster.Header = CaptureHeader(legacyPackage);
        goldenMaster.Index = CaptureIndex(legacyPackage);

        // Capture all resources
        foreach (var resource in legacyPackage.GetResourceList())
        {
            try
            {
                var resourceMaster = await CaptureResourceGoldenMasterAsync(legacyPackage, resource);
                goldenMaster.Resources.Add(resourceMaster);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to capture resource {Resource} from {Package}",
                    resource.ToString(), package.Name);
            }
        }

        // Capture round-trip serialization
        goldenMaster.SerializedBytes = await CaptureRoundTripAsync(legacyPackage);

        return goldenMaster;
    }

    private async Task<byte[]> CaptureRoundTripAsync(IPackage package)
    {
        using var memoryStream = new MemoryStream();
        await package.SaveAsync(memoryStream);
        return memoryStream.ToArray();
    }
}

public class PackageGoldenMaster
{
    public TestPackage SourcePackage { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }
    public string LegacyVersion { get; set; }

    public PackageHeader Header { get; set; }
    public List<ResourceIndexEntry> Index { get; set; }
    public List<ResourceGoldenMaster> Resources { get; set; } = new();
    public byte[] SerializedBytes { get; set; }

    public string GetFingerprint()
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(SerializedBytes);
        return Convert.ToHexString(hash);
    }
}
```

### Phase 3: Automated Test Generation

```csharp
public class GoldenMasterTestGenerator
{
    public async Task GenerateTestSuiteAsync(GoldenMasterSuite suite, string outputPath)
    {
        var testClassTemplate = await File.ReadAllTextAsync("Templates/GoldenMasterTest.template");

        foreach (var goldenMaster in suite.GoldenMasters)
        {
            var testClass = GenerateTestClass(goldenMaster, testClassTemplate);
            var fileName = $"GoldenMaster_{SanitizeFileName(goldenMaster.SourcePackage.Name)}_Tests.cs";
            var fullPath = Path.Combine(outputPath, fileName);

            await File.WriteAllTextAsync(fullPath, testClass);
        }

        // Generate master test runner
        var runnerCode = GenerateTestRunner(suite);
        await File.WriteAllTextAsync(Path.Combine(outputPath, "GoldenMasterTestRunner.cs"), runnerCode);
    }

    private string GenerateTestClass(PackageGoldenMaster goldenMaster, string template)
    {
        return template
            .Replace("{{PackageName}}", SanitizeClassName(goldenMaster.SourcePackage.Name))
            .Replace("{{PackagePath}}", goldenMaster.SourcePackage.Path)
            .Replace("{{ExpectedFingerprint}}", goldenMaster.GetFingerprint())
            .Replace("{{ResourceCount}}", goldenMaster.Resources.Count.ToString())
            .Replace("{{GeneratedAt}}", goldenMaster.GeneratedAt.ToString("O"));
    }
}

// Generated test example
[Fact]
public async Task RoundTrip_GameplayData_ProducesIdenticalOutput()
{
    // Arrange
    var packagePath = @"C:\Program Files (x86)\Origin Games\The Sims 4\Data\Client\GameplayData.package";
    var expectedFingerprint = "A1B2C3D4E5F6..."; // SHA256 of expected output

    // Act - Load with new implementation
    var newPackage = await _newPackageService.LoadPackageAsync(packagePath);
    var actualBytes = await newPackage.SerializeAsync();

    // Assert - Byte-perfect match required
    var actualFingerprint = ComputeSHA256(actualBytes);
    Assert.Equal(expectedFingerprint, actualFingerprint);
}
```

### Phase 4: Continuous Validation

```csharp
public class ContinuousGoldenMasterValidator
{
    private readonly IGoldenMasterRepository _repository;
    private readonly INewPackageService _newService;
    private readonly IMetrics _metrics;

    public async Task<ValidationReport> ValidateAllGoldenMastersAsync()
    {
        var report = new ValidationReport { StartTime = DateTimeOffset.UtcNow };
        var allMasters = await _repository.GetAllGoldenMastersAsync();

        var tasks = allMasters.Select(async master =>
        {
            try
            {
                var result = await ValidateSingleGoldenMasterAsync(master);
                return result;
            }
            catch (Exception ex)
            {
                return ValidationResult.Failed(master, ex);
            }
        });

        var results = await Task.WhenAll(tasks);

        report.Results = results.ToList();
        report.EndTime = DateTimeOffset.UtcNow;
        report.Summary = new ValidationSummary
        {
            TotalTests = results.Length,
            Passed = results.Count(r => r.Status == ValidationStatus.Passed),
            Failed = results.Count(r => r.Status == ValidationStatus.Failed),
            Skipped = results.Count(r => r.Status == ValidationStatus.Skipped)
        };

        // Report metrics
        _metrics.Counter("golden_master_tests_total").Increment(report.Summary.TotalTests);
        _metrics.Counter("golden_master_tests_passed").Increment(report.Summary.Passed);
        _metrics.Counter("golden_master_tests_failed").Increment(report.Summary.Failed);

        return report;
    }

    private async Task<ValidationResult> ValidateSingleGoldenMasterAsync(PackageGoldenMaster master)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Load with new implementation
            var newPackage = await _newService.LoadPackageAsync(master.SourcePackage.Path);
            var actualBytes = await newPackage.SerializeAsync();

            // Compare with golden master
            var expectedFingerprint = master.GetFingerprint();
            var actualFingerprint = ComputeFingerprint(actualBytes);

            if (expectedFingerprint == actualFingerprint)
            {
                return ValidationResult.Passed(master, stopwatch.Elapsed);
            }
            else
            {
                var diff = await GenerateDifferenceReportAsync(master.SerializedBytes, actualBytes);
                return ValidationResult.Failed(master, new GoldenMasterMismatchException(diff));
            }
        }
        catch (Exception ex)
        {
            return ValidationResult.Failed(master, ex);
        }
    }
}
```

## Test Data Management

### Storage Strategy

```csharp
public interface IGoldenMasterRepository
{
    Task<GoldenMasterSuite> LoadSuiteAsync(string suiteName);
    Task SaveSuiteAsync(string suiteName, GoldenMasterSuite suite);
    Task<bool> SuiteExistsAsync(string suiteName);
    Task<IEnumerable<string>> GetAvailableSuitesAsync();
}

public class FileSystemGoldenMasterRepository : IGoldenMasterRepository
{
    private readonly string _basePath;
    private readonly IJsonSerializer _serializer;

    public async Task SaveSuiteAsync(string suiteName, GoldenMasterSuite suite)
    {
        var suitePath = Path.Combine(_basePath, suiteName);
        Directory.CreateDirectory(suitePath);

        // Save metadata
        var metadata = new SuiteMetadata
        {
            Name = suiteName,
            CreatedAt = DateTimeOffset.UtcNow,
            PackageCount = suite.GoldenMasters.Count,
            TotalSizeBytes = suite.GoldenMasters.Sum(gm => gm.SerializedBytes.Length)
        };

        await File.WriteAllTextAsync(
            Path.Combine(suitePath, "metadata.json"),
            _serializer.Serialize(metadata));

        // Save individual golden masters (compressed)
        foreach (var master in suite.GoldenMasters)
        {
            var fileName = $"{SanitizeFileName(master.SourcePackage.Name)}.json.gz";
            var filePath = Path.Combine(suitePath, fileName);

            using var fileStream = File.Create(filePath);
            using var gzipStream = new GZipStream(fileStream, CompressionLevel.Optimal);
            using var writer = new StreamWriter(gzipStream);

            await writer.WriteAsync(_serializer.Serialize(master));
        }
    }
}
```

### Test Data Categories

```csharp
public class TestDataCategorizer
{
    public TestDataCategories CategorizePackages(IEnumerable<TestPackage> packages)
    {
        return new TestDataCategories
        {
            // Size-based categories
            SmallPackages = packages.Where(p => p.SizeBytes < 1_000_000).ToList(),           // < 1MB
            MediumPackages = packages.Where(p => p.SizeBytes < 50_000_000).ToList(),         // < 50MB
            LargePackages = packages.Where(p => p.SizeBytes >= 50_000_000).ToList(),         // >= 50MB

            // Content-based categories
            CoreGamePackages = packages.Where(p => p.Category == PackageCategory.Core).ToList(),
            ExpansionPackages = packages.Where(p => p.Category == PackageCategory.Expansion).ToList(),
            ModPackages = packages.Where(p => p.Category == PackageCategory.ModFolder).ToList(),

            // Edge cases
            EmptyPackages = packages.Where(p => GetResourceCount(p) == 0).ToList(),
            SingleResourcePackages = packages.Where(p => GetResourceCount(p) == 1).ToList(),
            MassivePackages = packages.Where(p => GetResourceCount(p) > 10000).ToList(),

            // Known problematic cases
            CorruptedPackages = packages.Where(p => p.KnownIssues.Contains("corruption")).ToList(),
            LegacyFormatPackages = packages.Where(p => p.KnownIssues.Contains("legacy")).ToList()
        };
    }
}
```

## Performance Monitoring

### Benchmark Integration

```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class GoldenMasterBenchmarks
{
    private GoldenMasterSuite _testSuite;
    private INewPackageService _newService;

    [GlobalSetup]
    public async Task Setup()
    {
        var repository = new FileSystemGoldenMasterRepository("test-data");
        _testSuite = await repository.LoadSuiteAsync("core-packages");
        _newService = new NewPackageService();
    }

    [Benchmark]
    [Arguments("small-package")]
    [Arguments("medium-package")]
    [Arguments("large-package")]
    public async Task<bool> ValidatePackage(string packageName)
    {
        var master = _testSuite.GoldenMasters.First(gm =>
            gm.SourcePackage.Name.Contains(packageName));

        var newPackage = await _newService.LoadPackageAsync(master.SourcePackage.Path);
        var actualBytes = await newPackage.SerializeAsync();

        return ComputeFingerprint(actualBytes) == master.GetFingerprint();
    }

    [Benchmark]
    public async Task<ValidationReport> ValidateFullSuite()
    {
        var validator = new ContinuousGoldenMasterValidator(
            new InMemoryGoldenMasterRepository(_testSuite),
            _newService,
            NullMetrics.Instance);

        return await validator.ValidateAllGoldenMastersAsync();
    }
}
```

## CI/CD Integration

### Pipeline Configuration

```yaml

# .github/workflows/golden-master-tests.yml

name: Golden Master Validation

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  golden-master-tests:
    runs-on: ubuntu-latest
    timeout-minutes: 120

    steps:

    - uses: actions/checkout@v3

    - name: Setup .NET 9
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 9.0.x

    - name: Cache Golden Master Data
      uses: actions/cache@v3
      with:
        path: test-data/golden-masters
        key: golden-masters-${{ hashFiles('test-data/metadata.json') }}

    - name: Download Test Data
      run: |
        # Download compressed golden master suite (stored separately due to size)
        curl -L "${{ secrets.GOLDEN_MASTER_DOWNLOAD_URL }}" -o golden-masters.tar.gz
        tar -xzf golden-masters.tar.gz -C test-data/

    - name: Run Golden Master Tests
      run: |
        dotnet test tests/TS4Tools.Tests.GoldenMaster/ \
          --configuration Release \
          --logger "trx;LogFileName=golden-master-results.trx" \
          --collect:"XPlat Code Coverage" \
          -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

    - name: Upload Test Results
      if: always()
      uses: actions/upload-artifact@v3
      with:
        name: golden-master-test-results
        path: |
          TestResults/golden-master-results.trx
          TestResults/coverage.opencover.xml

    - name: Generate Compatibility Report
      run: |
        dotnet run --project tools/CompatibilityReporter/ \
          -- --test-results TestResults/golden-master-results.trx \
             --output-path compatibility-report.html

    - name: Comment PR with Results
      if: github.event_name == 'pull_request'
      uses: actions/github-script@v6
      with:
        script: |
          const fs = require('fs');
          const report = fs.readFileSync('compatibility-report.html', 'utf8');

          github.rest.issues.createComment({
            issue_number: context.issue.number,
            owner: context.repo.owner,
            repo: context.repo.repo,
            body: `## Golden Master Test Results\n\n${report}`
          });
```

## Quality Gates & Success Criteria

### Automated Quality Gates

```csharp
public class GoldenMasterQualityGate
{
    private readonly ILogger<GoldenMasterQualityGate> _logger;

    public async Task<QualityGateResult> EvaluateAsync(ValidationReport report)
    {
        var criteria = new List<QualityCriterion>
        {
            // Compatibility criteria (BLOCKING)
            new QualityCriterion("100% Core Package Compatibility",
                () => report.GetCategoryResults("core").All(r => r.Status == ValidationStatus.Passed),
                CriterionSeverity.Blocking),

            new QualityCriterion("95%+ Overall Package Compatibility",
                () => report.Summary.SuccessRate >= 0.95m,
                CriterionSeverity.Blocking),

            // Performance criteria (HIGH)
            new QualityCriterion("Performance Within 10% of Baseline",
                () => report.AverageExecutionTime <= BaselineExecutionTime * 1.1m,
                CriterionSeverity.High),

            // Coverage criteria (MEDIUM)
            new QualityCriterion("All Resource Types Covered",
                () => report.GetResourceTypeCoverage().CoveragePercentage >= 100m,
                CriterionSeverity.Medium)
        };

        var results = new List<CriterionResult>();
        foreach (var criterion in criteria)
        {
            try
            {
                var passed = criterion.Evaluate();
                results.Add(new CriterionResult(criterion, passed));

                if (!passed && criterion.Severity == CriterionSeverity.Blocking)
                {
                    _logger.LogError("BLOCKING quality criterion failed: {Criterion}", criterion.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Quality criterion evaluation failed: {Criterion}", criterion.Name);
                results.Add(new CriterionResult(criterion, false, ex));
            }
        }

        return new QualityGateResult
        {
            OverallResult = results.Where(r => r.Criterion.Severity == CriterionSeverity.Blocking).All(r => r.Passed)
                ? QualityGateStatus.Passed
                : QualityGateStatus.Failed,
            CriterionResults = results,
            EvaluatedAt = DateTimeOffset.UtcNow
        };
    }
}
```

## Maintenance & Evolution

### Golden Master Refresh Strategy

```csharp
public class GoldenMasterMaintenanceService
{
    public async Task<RefreshResult> RefreshGoldenMastersAsync(
        string suiteName,
        RefreshOptions options)
    {
        var currentSuite = await _repository.LoadSuiteAsync(suiteName);
        var refreshResult = new RefreshResult();

        foreach (var master in currentSuite.GoldenMasters)
        {
            try
            {
                // Check if source package still exists and hasn't changed
                if (!File.Exists(master.SourcePackage.Path))
                {
                    refreshResult.AddMissing(master);
                    continue;
                }

                var currentModified = File.GetLastWriteTime(master.SourcePackage.Path);
                if (currentModified > master.GeneratedAt && !options.ForceRefresh)
                {
                    refreshResult.AddModified(master);
                    continue;
                }

                // Regenerate golden master if requested
                if (options.RefreshAll || options.RefreshSpecific.Contains(master.SourcePackage.Name))
                {
                    var newMaster = await _generator.GenerateGoldenMasterAsync(master.SourcePackage);

                    // Compare with existing - breaking change detection
                    if (!master.GetFingerprint().Equals(newMaster.GetFingerprint()))
                    {
                        refreshResult.AddBreakingChange(master, newMaster);
                    }
                    else
                    {
                        refreshResult.AddRefreshed(master, newMaster);
                    }
                }
            }
            catch (Exception ex)
            {
                refreshResult.AddError(master, ex);
            }
        }

        return refreshResult;
    }
}
```

## Consequences

### Positive

- **Absolute Compatibility Assurance**: Byte-perfect validation of migration results
- **Regression Prevention**: Automatic detection of behavior changes
- **Comprehensive Coverage**: Tests real-world scenarios with actual game data
- **Continuous Validation**: CI/CD integration ensures ongoing compatibility
- **Performance Monitoring**: Built-in benchmarking and performance regression detection
- **Documentation**: Golden masters serve as behavioral specifications

### Challenges

- **Storage Requirements**: Large test data sets require significant storage
- **Maintenance Overhead**: Golden masters must be updated when behavior intentionally changes
- **Initial Setup Complexity**: Generating comprehensive golden master suite is time-intensive
- **Test Execution Time**: Full golden master validation can be slow
- **False Positives**: Environment differences may cause spurious failures

### Mitigation Strategies

- **Tiered Testing**: Fast subset for PR validation, full suite for releases
- **Cloud Storage**: Use artifact storage for large golden master datasets
- **Incremental Updates**: Only regenerate affected golden masters when needed
- **Parallel Execution**: Run golden master tests in parallel to reduce total time
- **Environment Standardization**: Use containers to ensure consistent test environments

## Success Metrics

| Metric | Target | Measurement Method |
|--------|--------|--------------------|
| **Test Coverage** | 100% of resource types | Automated coverage analysis |
| **Compatibility Rate** | 99.9%+ packages pass | Golden master validation results |
| **Performance Regression** | â‰¤ 5% slower than baseline | Automated benchmark comparison |
| **False Positive Rate** | â‰¤ 1% | Manual validation of failed tests |
| **Test Execution Time** | â‰¤ 30 minutes full suite | CI/CD pipeline monitoring |

## Related Decisions

- ADR-004: Greenfield Migration Strategy (provides context for golden master necessity)
- ADR-001: .NET 9 Framework (target platform for new implementation)
- ADR-008: Cross-Platform File Format Compatibility
