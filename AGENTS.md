# AGENTS.md

Entry point for AI coding agents working in this repository (humans are welcome too).

## What this is

TS4Tools is a greenfield .NET 10 rewrite of the legacy Sims4Tools (s4pi/s4pe): a cross-platform library plus Avalonia app for reading and editing Sims 4 `.package` (DBPF) files.

- `src/TS4Tools.Interfaces`: public contracts (`IPackage`, `IResource`, `ResourceKey`, exceptions)
- `src/TS4Tools.Core`: `DbpfPackage`, resource handler registry, `ExtList`, file table
- `src/TS4Tools.Wrappers`: typed resources (STBL, SimData, CASP, catalog, images, ...) and FNV hashing
- `src/TS4Tools.Compatibility`: legacy s4pi API shim
- `src/TS4Tools.Cli`: headless `ts4tools` command-line tool (inspect, unpack/pack, scan)
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
dotnet test tests/TS4Tools.Cli.Tests
dotnet run --project src/TS4Tools.UI         # the app
```

Run test projects individually. A root-level `dotnet test` can hit an unrelated Ubuntu SDK crash.

## Headless tool (`ts4tools`)

Use the CLI to inspect or edit packages without writing C#. It never needs a display.

```bash
# Run from source (builds as needed)
dotnet run --project src/TS4Tools.Cli -- --help

# Or build once and call the binary directly
dotnet build src/TS4Tools.Cli -c Release
T=src/TS4Tools.Cli/bin/Release/net10.0/ts4tools      # (or: dotnet .../ts4tools.dll)
$T <command> --help                                   # authoritative options per command
```

| Command | Purpose |
|---|---|
| `$T info <package> [--json]` | DBPF header fields and resource count |
| `$T list <package> [--type 0xT[,0xT]] [--json]` | resources in index order (`T G I tag memsize compression`) |
| `$T unpack <package> <dir> [--force]` | write `<dir>/manifest.json` + `<dir>/resources/<T>_<G>_<I>[.<n>]<ext>` (decompressed) |
| `$T pack <dir> <package> [--force] [--no-compress-new]` | rebuild a package from an unpack directory |
| `$T compare <a.package> <b.package>` | resource-for-resource diff; exit 1 if different |
| `$T extract <package> <key> <file>` | write one resource's decompressed bytes |
| `$T replace <package> <key> <file> [--output <new.package>]` | replace one resource (in place unless `--output`) |
| `$T scan <path>... [--roundtrip] [--json f] [--markdown f] [--types 0xT,..]` | report types/versions the wrappers cannot handle |

Keys: `TTTTTTTT:GGGGGGGG:IIIIIIIIIIIIIIII`, `0xT-0xG-0xI`, or the unpack file-name stem. Exit codes: 0 ok, 1 failure / differences / scan issues, 2 usage error.

Edit workflow:

```bash
$T unpack in.package work/            # 1. extract
#                                       2. edit work/resources/* (XML/text as text; STBL etc. via the library,
#                                          see docs/agent-guide.md); add files named <T>_<G>_<I>.<ext> to add
#                                          resources, delete files to drop them
$T pack work/ out.package             # 3. rebuild (header, order and compression restored from manifest.json)
$T compare in.package out.package     # 4. review what changed
$T scan out.package --roundtrip       # 5. every wrapped resource still parses and round-trips
```

Notes:
- Unchanged unpack -> pack is resource-for-resource identical, except RefPack-compressed (0xFFFF) resources, which are re-encoded as ZLIB (0x5A42); `pack` warns and `compare` reports the label change.
- `pack` holds all resource data in memory (~3.5 GB peak for the 750 MB ClientFullBuild0).
- Never point `pack`, `replace` or `--output` at files in the game install or a user's Mods folder; write to a scratch copy.
- `scripts/scan-unsupported.sh [gameDir]` scans a game install read-only and regenerates `docs/unsupported-resources.md` (+ `artifacts/unsupported-resources.json`, gitignored).

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
- [docs/unsupported-resources.md](docs/unsupported-resources.md): generated report of resource types and versions the wrappers do not handle (regenerate with `scripts/scan-unsupported.sh`)
