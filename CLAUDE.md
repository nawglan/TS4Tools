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

### Legacy Code is the Primary Source of Truth

The legacy s4pi/s4pe codebase in `legacy_references/Sims4Tools/` is a **working, battle-tested implementation**. It has been used by the Sims modding community for years and correctly handles real-world .package files. When implementing any feature:

- **Trust the legacy code** - It works. If something seems odd, investigate why before assuming it's wrong.
- **Port algorithms faithfully** - The legacy code's algorithms are proven correct. Modernize the syntax, not the logic.
- **Use legacy as test oracle** - If your implementation produces different results than legacy for the same input, your implementation is likely wrong.

### Legacy Analysis is MANDATORY

Before implementing ANY feature:

1. **Find the legacy implementation** - Search `legacy_references/Sims4Tools/s4pi/`, `s4pi Wrappers/`, and `s4pe/` for the relevant code
2. **Understand the algorithm** - Read and understand what the legacy code does and why
3. **Document the source** - Add comments referencing the legacy file and line numbers (e.g., `// Source: DSTResource.cs lines 197-270`)
4. **Port with modern patterns** - Rewrite using C# 12+, Span<T>, async/await, but preserve the core logic
5. **Validate against legacy** - Ensure equivalent behavior for the same inputs

### Source References Apply to ALL Files

Every file in `src/TS4Tools.Wrappers/` must include a `// Source:` comment, including:

- **Resource classes** - Reference the legacy resource implementation
- **Factory classes** - Reference the legacy `AResourceHandler` class (e.g., `// Source: legacy_references/Sims4Tools/s4pi Wrappers/StblResource/StblResource.cs lines 423-433`)
- **Helper/Common classes** - Reference the legacy equivalent or parent resource file

This applies even when the modern pattern differs from legacy (e.g., factories vs. handlers). Document what legacy code the modern implementation replaces.

### Core Principles

- **Legacy-first**: When in doubt, do what the legacy code does
- **No copy-paste**: Understand the "why" and rewrite with modern patterns
- **Test-first from legacy analysis**: Write tests based on s4pi/s4pe behavior, not assumptions
- **Cross-platform**: Must work consistently on Windows, macOS, Linux
- **Plugin compatibility**: Preserve public API signatures for existing plugins
- **Format evolution**: Read all versions, write latest format

### Forbidden Practices

- Implementing features without first finding the legacy implementation
- Assuming the legacy code is wrong without thorough investigation
- Writing tests from assumptions instead of legacy analysis
- Using external libraries when legacy code provides a working algorithm
- Referencing s4pi/s4pe code outside of `legacy_references/Sims4Tools/`
- Adding `using System.*` directives to .cs files (use global usings in .csproj instead)

## Code Quality

### C# Patterns

- Use C# 12+ features (primary constructors, collection expressions)
- Async/await for all I/O operations
- Nullable reference types with explicit handling
- `Span<T>`/`Memory<T>` for efficient binary processing

### Global Usings (CRITICAL)

**NEVER add `using System.*` directives to .cs files.** All common usings are declared as global usings in each project's .csproj file.

Available in `TS4Tools.Wrappers.csproj`:
- `System.Buffers.Binary` - BinaryPrimitives for endian-aware reading
- `System.Collections` - Collection interfaces
- `System.Diagnostics.CodeAnalysis` - Nullability attributes
- `System.Text` - Encoding, StringBuilder

Available in `Directory.Build.props` (all projects):
- `System.Collections.ObjectModel` - ObservableCollection
- Common System namespaces

```csharp
// BAD - DO NOT DO THIS
using System.Buffers.Binary;
using System.Text;

namespace TS4Tools.Wrappers;
public class MyResource { }

// GOOD - just use the types directly, they're globally available
namespace TS4Tools.Wrappers;
public class MyResource { }
```

If you need a namespace not in global usings, add it to the appropriate .csproj `<Using>` ItemGroup, not to individual .cs files.

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
