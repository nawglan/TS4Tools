# TS4Tools

A modern, cross-platform package editor for [The Sims 4](https://en.wikipedia.org/wiki/The_Sims_4).

## About

TS4Tools is a **greenfield rewrite** of the venerable [s4pe/s4pi (Sims4Tools)](https://github.com/s4ptacle/Sims4Tools) project, rebuilt from the ground up using modern .NET and [Avalonia UI](https://avaloniaui.net/) to provide native cross-platform support for **Windows**, **macOS**, and **Linux**.

The original s4pe (Sims 4 Package Editor), based on the s4pi (Sims 4 Package Interface) library, has been an essential tool for The Sims 4 modding community since the game's release. This rewrite aims to preserve all the functionality that modders depend on while modernizing the codebase for the future.

## Goals

- **Cross-platform**: Native support for Windows, macOS, and Linux
- **Modern .NET**: Built on .NET 10+ with async/await throughout
- **Clean architecture**: Separation of concerns with interfaces, core library, and UI
- **Performance**: Efficient memory usage with Span<T>/Memory<T> and lazy loading
- **Extensibility**: Plugin-friendly resource wrapper system

## Technology Stack

- **.NET 10** - Latest .NET runtime
- **C# 13+** - Modern language features
- **Avalonia UI** - Cross-platform XAML UI framework
- **CommunityToolkit.Mvvm** - MVVM framework
- **xUnit + FluentAssertions** - Testing

## Current Status

- DBPF package reader/writer (ZLIB + RefPack decode, legacy compression semantics, byte-identical save of untouched resources)
- WrapperDealer-style handler registry: all legacy wrapper types registered, disabled wrappers, plugin DLLs from a `Wrappers` folder
- 60+ resource wrappers (CAS, Catalog, RCOL/Mesh, Jazz, Image/DDS/DST/RLE/Thumbnail, SimData, STBL, NameMap, Text, …), validated
  against real game and mod packages (`docs/real-data-validation.md`)
- s4pe GUI parity: multi-column browser and filter, editors/viewers for every wrapped type, property grid, commit/save workflow,
  import/export/copy/paste, helpers, DDS/RLE/DMAP tools, 3D model viewer
- `TS4Tools.Compatibility`: the s4pi public API (`s4pi.Interfaces`, `s4pi.Package`, `s4pi.WrapperDealer`, …) for recompiled plugins.
  Legacy-style wrapper plugins (`AResourceHandler` + `AResource`) placed in the `Wrappers` folder are used by the app's Value
  view, property grid and commit for resource types no built-in wrapper handles. **Binary compatibility with the original
  s4pi/s4pe DLLs is not provided** (they target .NET Framework and WinForms): plugins must be recompiled against
  `TS4Tools.Compatibility`.
- Legacy FNV hashing (FNV-24/32/64/64CLIP), ExtList type tags, catalog tag names, FileTable/GameFolders
- Resource versions newer than the legacy s4pi snapshot (e.g. current-game CASP > v37) are shown raw rather than misparsed

## Building

```bash
dotnet build
dotnet test
```

## Original Project Credits

This project builds upon the foundation laid by the original s4pe/s4pi contributors. Without their pioneering work on Sims modding tools, this project would not exist.

### Original Contributors

*Roughly in chronological order — [full details here](https://github.com/s4ptacle/Sims4Tools/graphs/contributors)*

- **Peter Jones** - Main author of s3pe/s3pi
- **[Rick](https://gib.me)** - A pioneer in TS4
- **[ChaosMageX](https://github.com/ChaosMageX)** - Initial s4p* setup; work on DATA, RIG and GEOM wrappers
- **[andrewtavera](https://github.com/andrewtavera)** - Mesh parts and other help
- **[granthes](https://github.com/granthes)** - Several contributions pre-release and in the early stages
- **[snaitf](https://github.com/Snaitf)** - Decoding and contributions for CCOL, COBJ, trims as well as bugfixes
- **IngeJones** - A kind lady who doesn't want her name mentioned
- **[Kuree](https://github.com/Kuree)** - Maintained the project in 2014 and 2015
- **[CmarNYC](https://github.com/cmarNYC)** - Continued contributions
- **[pbox](https://github.com/pboxx)** - Continued contributions
- **[Buzzler](https://github.com/BrutalBuzzler)** - Continued contributions

### Special Thanks

Without Peter Jones' work on s3pe/s3pi, this project would not exist. His philosophy to share and distribute this as an open source project will be carried on.

## Related Projects

- [s4pe/s4pi (Sims4Tools)](https://github.com/s4ptacle/Sims4Tools) - The original project this is based on
- [s3pi/s3pe](http://s3pi.sourceforge.net/) - The Sims 3 predecessor

## License

[GNU General Public License v3](http://www.gnu.org/licenses/gpl-3.0.html)

This project is licensed under GPLv3, consistent with the original s4pe/s4pi project.

### Non-Commercial Use

TS4Tools is a free, open-source, non-commercial community project intended for individual use by Sims 4 players and modders. It is not sold, monetized, or offered as a commercial product or service, and it is not affiliated with or endorsed by Electronic Arts or Maxis.

## Contributing

Contributions are welcome! Please feel free to submit pull requests.
