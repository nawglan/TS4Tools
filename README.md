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

🚧 **Work in Progress**

- ✅ Core DBPF package reader/writer
- ✅ ZLIB compression + RefPack decompression
- ✅ Resource handler system
- ✅ STBL (String Table) wrapper
- ✅ NameMap wrapper
- ✅ FNV hashing utilities
- 🔲 Avalonia UI application
- 🔲 Advanced resource wrappers (CAS, Catalog, Mesh)

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
- **IngeJones** - A kind contributor
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

## Contributing

Contributions are welcome! Please feel free to submit pull requests.
