# ADR-003: Cross-Platform UI with Avalonia

**Status:** Accepted
**Date:** August 3, 2025
**Deciders:** Architecture Team, UI/UX Team

## Context

The legacy TS4Tools application was built with Windows Forms, limiting it to Windows-only deployment. Modern cross-platform requirements demand a UI framework that supports Windows, macOS, and Linux while providing a native-feeling experience on each platform.

## Decision

We will adopt Avalonia UI 11.3+ as the primary UI framework for the cross-platform TS4Tools application.

## Rationale

### Requirements Analysis

1. **Cross-Platform**: Must run natively on Windows, macOS, and Linux
1. **Performance**: Must handle large datasets (10k+ resources) efficiently
1. **Native Feel**: Should integrate well with each platform's conventions
1. **MVVM Support**: Must support modern MVVM patterns with data binding
1. **Extensibility**: Plugin architecture for custom resource editors
1. **Accessibility**: Comply with accessibility standards on all platforms

### Framework Comparison

| Framework | Cross-Platform | Performance | Native Feel | MVVM | Learning Curve |
|-----------|----------------|-------------|-------------|------|----------------|
| **âœ… Avalonia** | Full | Excellent | Good | Excellent | Medium |
| .NET MAUI | Limited | Good | Excellent | Good | Medium |
| Electron + Blazor | Full | Poor | Poor | Good | Low |
| WPF | Windows Only | Excellent | Excellent | Excellent | Low |
| Flutter | Full | Excellent | Fair | Fair | High |

### Avalonia Advantages

1. **XAML Familiarity**: WPF developers can leverage existing knowledge
1. **Performance**: Native rendering with hardware acceleration
1. **Theming**: Fluent UI themes available for modern appearance
1. **Data Binding**: Full MVVM support with reactive extensions
1. **Controls**: Rich control library with virtualization support
1. **Community**: Active development and growing ecosystem

## Architecture Design

### MVVM Pattern Implementation

```csharp
// ViewModels use ReactiveUI for enhanced MVVM
public class PackageExplorerViewModel : ReactiveObject
{
    private ObservableCollection<ResourceViewModel> _resources = new();
    private string _searchFilter = string.Empty;

    public ObservableCollection<ResourceViewModel> Resources
    {
        get => _resources;
        set => this.RaiseAndSetIfChanged(ref _resources, value);
    }

    public string SearchFilter
    {
        get => _searchFilter;
        set => this.RaiseAndSetIfChanged(ref _searchFilter, value);
    }

    // Reactive command with async support
    public ReactiveCommand<Unit, Unit> LoadPackageCommand { get; }

    public PackageExplorerViewModel(IPackageService packageService)
    {
        LoadPackageCommand = ReactiveCommand.CreateFromTask(LoadPackageAsync);

        // Reactive filtering
        this.WhenAnyValue(x => x.SearchFilter)
            .Throttle(TimeSpan.FromMilliseconds(300))
            .DistinctUntilChanged()
            .Subscribe(filter => FilterResources(filter));
    }
}
```

### Platform-Specific Implementations

```csharp
// Platform services with dependency injection
public interface IDialogService
{
    Task<string?> ShowOpenFileDialogAsync(string title, FileDialogFilter[] filters);
    Task<bool> ShowConfirmationDialogAsync(string title, string message);
    Task ShowErrorDialogAsync(string title, string message);
}

#if WINDOWS
public class WindowsDialogService : IDialogService
{
    public async Task<string?> ShowOpenFileDialogAsync(string title, FileDialogFilter[] filters)
    {
        var dialog = new OpenFileDialog { Title = title, Filters = filters.ToList() };
        var result = await dialog.ShowAsync(GetActiveWindow());
        return result?.FirstOrDefault();
    }
}
#endif
```

### Theme and Styling Strategy

```xml
<!-- Application.axaml - Fluent theme with customizations -->
<Application.Styles>
    <FluentTheme />
    <StyleInclude Source="/Styles/TS4ToolsTheme.axaml" />
</Application.Styles>

<!-- Custom theme for TS4Tools branding -->
<Styles xmlns="https://github.com/avaloniaui">
    <Style Selector="Window">
        <Setter Property="Background" Value="{DynamicResource SystemChromeLowColor}" />
    </Style>

    <Style Selector="DataGrid">
        <Setter Property="GridLinesVisibility" Value="Horizontal" />
        <Setter Property="AlternatingRowBackground" Value="{DynamicResource SystemBaseLowColor}" />
    </Style>
</Styles>
```

## Application Structure

### Main Application Layout

```
src/TS4Tools.Desktop/
â”œâ”€â”€ App.axaml                    # Application entry point
â”œâ”€â”€ ViewModels/
â”‚   â”œâ”€â”€ MainWindowViewModel.cs   # Main application state
â”‚   â”œâ”€â”€ PackageExplorerViewModel.cs
â”‚   â”œâ”€â”€ ResourceEditorViewModel.cs
â”‚   â””â”€â”€ SettingsViewModel.cs
â”œâ”€â”€ Views/
â”‚   â”œâ”€â”€ MainWindow.axaml         # Main application window
â”‚   â”œâ”€â”€ PackageExplorerView.axaml
â”‚   â”œâ”€â”€ ResourceEditorView.axaml
â”‚   â””â”€â”€ SettingsView.axaml
â”œâ”€â”€ Controls/                    # Custom controls
â”‚   â”œâ”€â”€ ResourceTreeView.axaml
â”‚   â”œâ”€â”€ HexEditor.axaml
â”‚   â””â”€â”€ ImagePreview.axaml
â”œâ”€â”€ Services/                    # UI services
â”‚   â”œâ”€â”€ IDialogService.cs
â”‚   â”œâ”€â”€ IThemeService.cs
â”‚   â””â”€â”€ Platform/                # Platform-specific implementations
â””â”€â”€ Styles/
    â”œâ”€â”€ TS4ToolsTheme.axaml
    â””â”€â”€ Controls/
```

### Performance Optimizations

1. **Virtualization**: Use `VirtualizingStackPanel` for large lists
1. **Async Operations**: All file I/O operations are async
1. **Progressive Loading**: Load resources on-demand
1. **Memory Management**: Dispose of large objects properly
1. **UI Threading**: Keep UI thread responsive with proper async/await

## Migration Strategy

### Phase 1: Core UI Migration (Week 1-2)

1. Create Avalonia project structure
1. Implement main window and basic navigation
1. Convert core Windows Forms dialogs
1. Establish MVVM patterns

### Phase 2: Feature Migration (Week 3-4)

1. Package explorer with tree view
1. Resource editing capabilities
1. Search and filter functionality
1. Settings and preferences

### Phase 3: Platform-Specific Features (Week 5-6)

1. Native file dialogs for each platform
1. Platform-specific theming
1. Menu and toolbar integration
1. Accessibility compliance

### Phase 4: Advanced Features (Week 7-8)

1. Plugin architecture for custom editors
1. Advanced resource preview capabilities
1. Drag-and-drop support
1. Performance optimization and profiling

## Platform-Specific Considerations

### Windows

- Native file dialogs with shell integration
- Windows 11 theme support
- Jump lists and taskbar integration
- High DPI awareness

### macOS

- Native menu bar integration
- macOS-style dialogs and sheets
- Retina display support
- Sandboxing compatibility

### Linux

- GTK integration for native feel
- Desktop environment theme respect
- File manager integration
- Accessibility tools support

## Alternative Frameworks Considered

### .NET MAUI

**Pros:**

- Microsoft's official cross-platform framework
- Excellent native integration
- Strong tooling support

**Cons:**

- Limited Linux support
- Desktop support still maturing
- Complex for desktop-first applications

**Decision:** Rejected due to incomplete Linux support and desktop focus

### Electron with Blazor

**Pros:**

- Web technology familiarity
- Rapid development
- Cross-platform by default

**Cons:**

- Poor performance with large datasets
- Large memory footprint
- Non-native appearance

**Decision:** Rejected due to performance concerns

### Flutter

**Pros:**

- Excellent performance
- True cross-platform
- Modern reactive framework

**Cons:**

- Dart language learning curve
- Limited .NET integration
- Desktop support still evolving

**Decision:** Rejected due to .NET ecosystem mismatch

## Implementation Examples

### Data Grid with Virtualization

```xml
<DataGrid ItemsSource="{Binding Resources}"
          SelectedItem="{Binding SelectedResource}"
          VirtualizationMode="VirtualizedItems"
          AutoGenerateColumns="False">
    <DataGrid.Columns>
        <DataGridTextColumn Header="Type" Binding="{Binding ResourceType}" />
        <DataGridTextColumn Header="Group" Binding="{Binding GroupId}" />
        <DataGridTextColumn Header="Instance" Binding="{Binding InstanceId}" />
        <DataGridTextColumn Header="Size" Binding="{Binding Size, StringFormat=N0}" />
    </DataGrid.Columns>
</DataGrid>
```

### Reactive Search Implementation

```csharp
public PackageExplorerViewModel(IPackageService packageService)
{
    // Reactive search with debouncing
    var searchResults = this.WhenAnyValue(x => x.SearchFilter)
        .Throttle(TimeSpan.FromMilliseconds(300))
        .DistinctUntilChanged()
        .Where(filter => !string.IsNullOrWhiteSpace(filter))
        .SelectMany(filter => Observable.FromAsync(() => SearchResourcesAsync(filter)))
        .ObserveOn(RxApp.MainThreadScheduler);

    searchResults.Subscribe(results =>
    {
        FilteredResources.Clear();
        FilteredResources.AddRange(results);
    });
}
```

### Platform-Specific Menu Integration

```csharp
// macOS-specific menu bar integration
#if MACOS
public void ConfigureMacOSMenu()
{
    var menuBar = new NativeMenuBar();
    var fileMenu = new NativeMenu("File");

    fileMenu.Add(new NativeMenuItem("Open Package...")
    {
        Command = LoadPackageCommand
    });

    NativeMenu.SetMenu(this, menuBar);
}
#endif
```

## Performance Benchmarks

### Target Performance Metrics

| Scenario | Current (WinForms) | Target (Avalonia) | Validation Method |
|----------|-------------------|-------------------|-------------------|
| Application Startup | 2.3s | 1.5s | Startup profiling |
| Load 10k Resources | 5.2s | 2.1s | Memory profiler |
| Search/Filter | 850ms | 200ms | UI responsiveness |
| Memory Usage | 180MB | 120MB | Memory profiler |

### Optimization Techniques

1. **UI Virtualization**: Only render visible items
1. **Background Loading**: Load data on background threads
1. **Caching**: Cache rendered UI elements
1. **Progressive Updates**: Update UI incrementally

## Testing Strategy

### Unit Testing

```csharp
[Test]
public void SearchFilter_WithValidTerm_FiltersResults()
{
    // Arrange
    var viewModel = new PackageExplorerViewModel(mockPackageService);
    viewModel.Resources.AddRange(CreateTestResources());

    // Act
    viewModel.SearchFilter = "texture";

    // Assert - using reactive testing
    viewModel.FilteredResources.Should().HaveCount(3);
    viewModel.FilteredResources.Should().OnlyContain(r => r.Name.Contains("texture"));
}
```

### Integration Testing

- Cross-platform UI automation
- Performance regression testing
- Accessibility compliance testing
- Visual regression testing

## Consequences

### Positive

- **Cross-Platform**: True native support for Windows, macOS, Linux
- **Performance**: Better than web-based alternatives
- **Maintainability**: Modern MVVM patterns with reactive programming
- **Familiarity**: XAML knowledge transfers from WPF
- **Community**: Growing ecosystem with active development

### Negative

- **Learning Curve**: Team needs Avalonia-specific training
- **Platform Differences**: Some features require platform-specific code
- **Maturity**: Newer framework with occasional breaking changes
- **Documentation**: Less comprehensive than WPF documentation

### Neutral

- **XAML Complexity**: Similar complexity to WPF development
- **Performance**: Better than Electron, similar to native frameworks

## Success Criteria

### Functional Requirements

- [ ] Application runs natively on Windows 10/11, macOS 12+, Ubuntu 20.04+
- [ ] All legacy Windows Forms functionality migrated
- [ ] Performance meets or exceeds legacy application
- [ ] Native platform integration (dialogs, themes, accessibility)

### Quality Requirements

- [ ] 95%+ unit test coverage for ViewModels
- [ ] Cross-platform UI automation tests
- [ ] Accessibility compliance on all platforms
- [ ] Performance benchmarks meet targets

### User Experience

- [ ] Native look and feel on each platform
- [ ] Responsive UI with large datasets (10k+ items)
- [ ] Keyboard shortcuts work consistently
- [ ] Theme support (light/dark/system)

## Future Considerations

### Plugin Architecture

- MVVM-based plugin system
- Custom resource editor plugins
- Theme and style customization
- Extension points for new resource types

### Advanced Features

- Multi-window support
- Advanced docking panels
- Scripting integration
- Workflow automation

______________________________________________________________________

**Status**: Accepted and In Progress
**Related ADRs**: ADR-001 (.NET 9), ADR-002 (Dependency Injection)
**Next Review**: After Phase 1.6 completion
