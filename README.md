# Ekona

<!-- markdownlint-disable MD033 -->
<p align="center">
  <a href="https://www.nuget.org/packages/SceneGate.Ekona">
    <img alt="Stable version" src="https://img.shields.io/nuget/v/SceneGate.Ekona?label=nuget.org&logo=nuget" />
  </a>
  &nbsp;
  <a href="https://dev.azure.com/SceneGate/SceneGate/_packaging?_a=feed&feed=SceneGate-Preview">
    <img alt="GitHub commits since latest release (by SemVer)" src="https://img.shields.io/github/commits-since/SceneGate/Ekona/latest?sort=semver" />
  </a>
  &nbsp;
  <a href="https://github.com/SceneGate/Ekona/workflows/Build%20and%20release">
    <img alt="Build and release" src="https://github.com/SceneGate/Ekona/workflows/Build%20and%20release/badge.svg" />
  </a>
  &nbsp;
  <a href="https://choosealicense.com/licenses/mit/">
    <img alt="MIT License" src="https://img.shields.io/badge/license-MIT-blue.svg?style=flat" />
  </a>
  &nbsp;
</p>

_Ekona_ is a library part of the [_SceneGate_](https://github.com/SceneGate)
framework that provides support for **DS and DSi file formats.**

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
[getting started guide](https://scenegate.github.io/Ekona/docs/dev/tutorial.html)
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
The libraries works on supported versions of .NET: 6.0 and 8.0.

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
    <clear/>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="SceneGate-Preview" value="https://pkgs.dev.azure.com/SceneGate/SceneGate/_packaging/SceneGate-Preview/nuget/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
    <packageSource key="SceneGate-Preview">
      <package pattern="Yarhl*" />
      <package pattern="Texim*" />
      <package pattern="SceneGate*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
```

## Documentation

You can get full details about how to use library from the
[documentation](https://scenegate.github.io/Ekona/docs/dev/features/cartridge.html)
website.

Don't miss the
[formats specifications](https://scenegate.github.io/Ekona/docs/specs/cartridge/cartridge.html)
in case you need to do further research.

And don't hesitate to ask questions in the
[project Discussion site!](https://github.com/SceneGate/Ekona/discussions)

## Build

The project requires to build .NET 8.0 SDK.

To build, test and generate artifacts run:

```sh
# Build and run tests
dotnet run --project build/orchestrator

# (Optional) Create bundles (nuget, zips, docs)
dotnet run --project build/orchestrator -- --target=Bundle
```

To build the documentation only, run:

```sh
dotnet docfx docs/docfx.json --serve
```

To run the performance test with memory and CPU traces:

```sh
dotnet run --project src/Ekona.PerformanceTests/ -c Release -- -f "*<TestName>*" -m -p EP --maxWidth 60
```

## Special thanks

The DS / DSi cartridge format was based on the amazing reverse engineering work
of Martin Korth at [GBATek](https://problemkaputt.de/gbatek.htm). Its
specifications of the hardware of the video controller and I/O ports was also a
great help in additional reverse engineering.
