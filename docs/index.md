# Ekona [![MIT License](https://img.shields.io/badge/license-MIT-blue.svg?style=flat)](https://choosealicense.com/licenses/mit/)

_Ekona_ is a library part of the [_SceneGate_](https://github.com/SceneGate)
framework that provides support for DS and DSi file formats.

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

## Special thanks

The DS / DSi cartridge format was based on the amazing reverse engineering work
of Martin Korth at [GBATek](https://problemkaputt.de/gbatek.htm). Its
specifications of the hardware of the video controller and I/O ports was also a
great help in additional reverse engineering.
