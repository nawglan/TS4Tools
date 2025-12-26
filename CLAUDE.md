# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TS4Tools is a greenfield rewrite of legacy Sims4Tools (s4pe/s4pi), providing cross-platform (Windows, macOS, Linux) manipulation of Sims 4 `.package` files using .NET 9 and Avalonia UI.

## Build Commands

```bash
dotnet restore          # Restore dependencies
dotnet build            # Build solution
dotnet test             # Run all tests
dotnet run --project src/TS4Tools.UI  # Run the application
```

## Architecture

- `src/TS4Tools.Core/` - Package parsing, resource handling, business logic
- `src/TS4Tools.UI/` - Avalonia MVVM application
- `src/TS4Tools.Interfaces/` - Public API contracts
- `src/TS4Tools.Compatibility/` - Legacy s4pi API compatibility layer
- `tests/` - xUnit tests with FluentAssertions
- `legacy_references/Sims4Tools/` - **s4pi/s4pe reference code (SINGLE SOURCE OF TRUTH)**

### Technology Stack

- .NET 9, C# 12+
- Avalonia UI with MVVM (ReactiveUI/CommunityToolkit.Mvvm)
- xUnit, FluentAssertions, BenchmarkDotNet
- MSBuild with centralized package management (`Directory.Build.props`, `Directory.Packages.props`)

## Development Philosophy

### Legacy Analysis is MANDATORY

Before implementing ANY feature:

1. **Analyze s4pi** (`legacy_references/Sims4Tools/s4pi/` and `s4pi Wrappers/`) - understand the business logic, edge cases, and data formats
2. **Analyze s4pe** (`legacy_references/Sims4Tools/s4pe/`) - understand UI patterns and user workflows
3. **Extract business logic** - document algorithms, validation rules, format handling
4. **Design modern implementation** - preserve logic while using modern .NET patterns
5. **Validate against legacy** - ensure equivalent behavior for same inputs

### Core Principles

- **No copy-paste**: Understand the "why" and rewrite with modern patterns
- **Test-first from legacy analysis**: Write tests based on s4pi/s4pe behavior, not assumptions
- **Cross-platform**: Must work consistently on Windows, macOS, Linux
- **Plugin compatibility**: Preserve public API signatures for existing plugins
- **Format evolution**: Read all versions, write latest format

### Forbidden Practices

- Copying s4pi/s4pe code without understanding it
- Implementing without analyzing legacy counterparts
- Writing tests from assumptions instead of legacy analysis
- Referencing s4pi/s4pe code outside of `legacy_references/Sims4Tools/`

## Code Quality

### C# Patterns

- Use C# 12+ features (primary constructors, collection expressions)
- Global usings declared in .csproj, NOT in .cs files
- Async/await for all I/O operations
- Nullable reference types with explicit handling
- `Span<T>`/`Memory<T>` for efficient binary processing

### Event Subscription Management

Always track and unsubscribe event handlers to prevent memory leaks:

```csharp
// BAD - lambda subscriptions in loops are never unsubscribed
foreach (var item in items)
{
    var vm = new ItemViewModel(item);
    vm.PropertyChanged += (s, e) => { /* handle */ };  // Memory leak!
    _viewModels.Add(vm);
}

// GOOD - track subscriptions for cleanup
private readonly List<(ItemViewModel VM, PropertyChangedEventHandler Handler)> _subscriptions = [];

private void LoadItems()
{
    ClearSubscriptions();  // Clean up previous subscriptions
    foreach (var item in items)
    {
        var vm = new ItemViewModel(item);
        PropertyChangedEventHandler handler = (s, e) => { /* handle */ };
        vm.PropertyChanged += handler;
        _subscriptions.Add((vm, handler));
        _viewModels.Add(vm);
    }
}

private void ClearSubscriptions()
{
    foreach (var (vm, handler) in _subscriptions)
        vm.PropertyChanged -= handler;
    _subscriptions.Clear();
}
```

This pattern is critical in MVVM ViewModels where child ViewModels are created dynamically.

### Security for .package Parsing

Never trust file values without validation:

```csharp
// BAD
var count = reader.ReadInt32();
for (int i = 0; i < count; i++) { /* process */ }

// GOOD
var count = reader.ReadInt32();
if (count < 0 || count > MaxReasonableResourceCount)
    throw new PackageFormatException($"Invalid resource count: {count}");
for (int i = 0; i < count; i++) { /* process */ }
```

Validate: array bounds, string lengths, file offsets, resource counts, loop counters.

## Commit Messages

Use conventional commits with a "why" explanation:

```
<type>[scope]: <description>

[body explaining WHY - REQUIRED]
```

Types: feat, fix, docs, style, refactor, perf, test, build, ci, chore
