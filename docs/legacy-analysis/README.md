# Legacy s4pi/s4pe Analysis

This directory contains comprehensive documentation of the legacy s4pi/s4pe codebase to guide the TS4Tools rewrite.

## Documents

1. **[01-core-interfaces.md](01-core-interfaces.md)** - Core interfaces (IResource, IPackage, IResourceKey) and abstract base classes

2. **[02-dbpf-package-format.md](02-dbpf-package-format.md)** - DBPF package file format, header structure, index format, and compression

3. **[03-wrapper-dealer-architecture.md](03-wrapper-dealer-architecture.md)** - Plugin architecture for resource type wrappers

4. **[04-resource-wrappers-catalog.md](04-resource-wrappers-catalog.md)** - Catalog of all 22 resource wrapper projects and 90+ resource types

5. **[05-s4pe-gui-architecture.md](05-s4pe-gui-architecture.md)** - WinForms GUI architecture, widgets, and workflows

6. **[06-extras-and-helpers.md](06-extras-and-helpers.md)** - s4pi extras libraries and helper applications

7. **[07-rewrite-plan.md](07-rewrite-plan.md)** - TS4Tools rewrite plan with phases and milestones

## Legacy Code Location

All legacy reference code is in:
```
legacy_references/Sims4Tools/
├── s4pi/                    # Core interface library
│   ├── Interfaces/          # IResource, IPackage, etc.
│   ├── Package/             # DBPF format implementation
│   └── WrapperDealer/       # Wrapper discovery
├── s4pi Wrappers/           # 22 resource wrapper projects
├── s4pi Extras/             # Helper libraries
├── s4pe/                    # GUI application
└── s4pe Helpers/            # External helper tools
```

## Key Insights for Rewrite

### Keep
- DBPF header/index format (exact byte compatibility required)
- TGI (Type/Group/Instance) resource identification
- Parse/UnParse pattern for resources
- Dirty tracking and change notification

### Modernize
- Replace reflection with source generators or DI
- Use `Span<T>`/`Memory<T>` for binary parsing
- Async I/O throughout
- MVVM architecture for UI

### Simplify
- Remove unused API versioning system
- Flatten deep inheritance hierarchies
- Replace reflection-based property access
