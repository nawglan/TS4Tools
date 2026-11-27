# AGENTS.md

Entry point for AI coding agents working in this repository (humans are welcome too).

## What this is

TS4Tools is a greenfield .NET 10 rewrite of the legacy Sims4Tools (s4pi/s4pe): a cross-platform library plus Avalonia app for reading and editing Sims 4 `.package` (DBPF) files.

- `src/TS4Tools.Interfaces`: public contracts (`IPackage`, `IResource`, `ResourceKey`, exceptions)
- `src/TS4Tools.Core`: `DbpfPackage`, resource handler registry, `ExtList`, file table
- `src/TS4Tools.Wrappers`: typed resources (STBL, SimData, CASP, catalog, images, ...) and FNV hashing
- `src/TS4Tools.Compatibility`: legacy s4pi API shim
- `src/TS4Tools.UI`: Avalonia app
- `tests/`: xUnit + FluentAssertions
- `legacy_references/Sims4Tools/`: read-only legacy code, the single source of truth. Never modify it or `git add` it.

## Build and test

```bash
dotnet build                                 # must finish with 0 warnings
dotnet test tests/TS4Tools.Core.Tests        # run test projects one at a time
dotnet test tests/TS4Tools.Wrappers.Tests
dotnet test tests/TS4Tools.Compatibility.Tests
dotnet test tests/TS4Tools.UI.Tests
dotnet run --project src/TS4Tools.UI         # the app
```

Run test projects individually. A root-level `dotnet test` can hit an unrelated Ubuntu SDK crash.

## Rules (full text in CLAUDE.md)

- Legacy first: find the legacy implementation, port its algorithm faithfully, cite it in a `// Source: file lines N-M` comment (required for every file in `src/TS4Tools.Wrappers`). If your output differs from legacy for the same input, you are wrong.
- Never add `using System.*` to `.cs` files. Add a `<Using Include="..."/>` to the `.csproj`.
- Validate every count, offset and size read from a package before using it.
- Unsubscribe event handlers you subscribe (track them; no lambdas in loops).
- Never write to the game install or to a user's Mods folder from tests or tools. Use temp directories.
- Commits: conventional (`feat|fix|docs|test|...`) with a body that explains WHY.

## Docs

- [docs/agent-guide.md](docs/agent-guide.md): how to use the libraries and the headless `ts4tools` CLI to read and edit packages (samples are verified by `tests/TS4Tools.Core.Tests/DocSamplesTests.cs`)
- [docs/real-data-validation.md](docs/real-data-validation.md): validation against real game and mod packages
- [docs/unsupported-resources.md](docs/unsupported-resources.md): generated report of resource types and versions the wrappers do not handle (run `scripts/scan-unsupported.sh`; may not exist until generated)
