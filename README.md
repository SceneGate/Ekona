# Ekona [![MIT License](https://img.shields.io/badge/license-MIT-blue.svg?style=flat)](https://choosealicense.com/licenses/mit/) ![Build and release](https://github.com/SceneGate/Ekona/workflows/Build%20and%20release/badge.svg)

_Ekona_ is a library part of the [_SceneGate_](https://github.com/SceneGate)
framework that provides support for DS and DSi file formats.

The library supports .NET 6.0 and above on Linux, Window and MacOS.

<!-- prettier-ignore -->
| Release | Package                                                           |
| ------- | ----------------------------------------------------------------- |
| Stable  | [![Nuget](https://img.shields.io/nuget/v/SceneGate.Ekona?label=nuget.org&logo=nuget)](https://www.nuget.org/packages/SceneGate.Ekona) |
| Preview | [Azure Artifacts](https://dev.azure.com/SceneGate/SceneGate/_packaging?_a=feed&feed=SceneGate-Preview) |

## Supported formats

- :video_game: DS cartridge:
  - :file_folder: Filesystem: read and write
  - :information_source: Header: read and write, including extended header
  - :framed_picture: Banner and icon: read and write.
  - :closed_lock_with_key: ARM9 secure area encryption and decryption (KEY1).
- :video_game: DSi cartridge:
  - :file_folder: Filesystem: read and write `arm9i` and `arm7i` programs.
  - :information_source: Extended header: read and write
  - :framed_picture: Animated banner icons
  - :closed_lock_with_key: Modcrypt encryption and decryption
  - :lock_with_ink_pen: HMAC validation and generation when keys are provided.
  - :lock_with_ink_pen: Signature validation when keys are provided.

## Getting started

Check-out the
[getting started guide](https://scenegate.github.io/Ekona/dev/introduction.html)
to start using _Ekona_ in no time! Below you can find an example that shows how
to open a DS/DSi ROM file (cartridge dump).

```csharp
// Create Yarhl node from a file (binary format).
Node game = NodeFactory.FromFile("game.nds", FileOpenMode.Read);

// Use the `Binary2NitroRom` converter to convert the binary format
// into node containers (virtual file system tree with files and directories).
game.TransformWith<Binary2NitroRom>();

// And it's done!
// Now we can access to every game file. For instance, we can export one file
Node items = Navigator.SearchNode(game, "data/Items.dat");
items.Stream.WriteTo("dump/Items.dat");
```

## Usage

The project provides the following .NET libraries (NuGet packages in nuget.org).
The libraries only support the latest .NET LTS version: **.NET 6.0**.

- [![SceneGate.Ekona](https://img.shields.io/nuget/v/SceneGate.Ekona?label=SceneGate.Ekona&logo=nuget)](https://www.nuget.org/packages/SceneGate.Ekona)
  - `SceneGate.Ekona.Containers.Rom`: DS and DSi cartridge (ROM) format.
  - `SceneGate.Ekona.Security`: hash and encryption algorithms

Preview releases can be found in this
[Azure DevOps package repository](https://dev.azure.com/SceneGate/SceneGate/_packaging?_a=feed&feed=SceneGate-Preview).
To use a preview release, create a file `nuget.config` in the same directory of
your solution file (.sln) with the following content:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="SceneGate-Preview" value="https://pkgs.dev.azure.com/SceneGate/SceneGate/_packaging/SceneGate-Preview/nuget/v3/index.json" />
  </packageSources>
</configuration>
```

## Documentation

You can get full details about how to use library from the
[documentation](https://scenegate.github.io/Ekona/dev/features/cartridge.html)
website.

Don't miss the
[formats specifications](https://scenegate.github.io/Ekona/specs/cartridge/cartridge.html)
in case you need to do further research.

And don't hesitate to ask questions in the
[project Discussion site!](https://github.com/SceneGate/Ekona/discussions)

## Build

Building requires the
[.NET 6.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) and .NET
Framework 4.8 or the latest
[Mono](https://www.mono-project.com/download/stable/). If you open the project
with VS Code and you did install the
[VS Code Remote Containers](https://code.visualstudio.com/docs/remote/containers)
extension, you can have an already pre-configured development environment with
Docker or Podman.

To build, test and generate artifacts run:

```sh
# Only required the first time
dotnet tool restore

# It will build, run the tests and create the artifacts (NuGets and doc)
dotnet cake
```

To build and test only, run:

```sh
dotnet cake --target=BuildTest
```

To build the documentation only, run:

```sh
dotnet cake --target=Build-Doc
```

If any of the previous tasks fail and you want to get more verbosity, re-run the
command with the argument `--verbosity=diagnostic`

To run the performance test with memory and CPU traces:

```sh
dotnet run --project src/Ekona.PerformanceTests/ -c Release -- -f "*<TestName>*" -m -p EP --maxWidth 60
```

## Special thanks

The DS / DSi cartridge format was based on the amazing reverse engineering work
of Martin Korth at [GBATek](https://problemkaputt.de/gbatek.htm). Its
specifications of the hardware of the video controller and I/O ports was also a
great help in additional reverse engineering.
