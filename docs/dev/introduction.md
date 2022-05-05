# _Ekona: DS and DSi formats_

_Ekona_ is a library part of the _SceneGate_ framework that provides support for
DS and DSi file formats.

## Usage

The project provides the following .NET libraries (NuGet packages in nuget.org).
The libraries only support the latest .NET LTS version: **.NET 6.0**.

- [![SceneGate.Ekona](https://img.shields.io/nuget/v/SceneGate.Ekona?label=SceneGate.Ekona&logo=nuget)](https://www.nuget.org/packages/SceneGate.Ekona)
  - `SceneGate.Ekona.Containers.Rom`: DS and DSi cartridge (ROM) format.
  - `SceneGate.Ekona.Security`: hash and encryption algorithms

## Quick start

### Cartridge file system

Let's start by opening a game (ROM) and accessing to its files. We can virtually
_unpack_ the ROM by using the converter
[`Binar2NitroRom`](xref:SceneGate.Ekona.Containers.Rom.Binary2NitroRom). It will
create a tree of _nodes_ that we can use to access to the files.

[!code-csharp[OpenGame](../../src/Ekona.Examples/QuickStart.cs?name=OpenGame)]

> [!NOTE]  
> The converter will not write any file to the disk. It will create a tree of
> nodes that points to different parts of the original game file. If it needs to
> decrypt a file (like `arm9i`), it will create a new _stream_ on memory.

Now we can quickly modify our file even by code!

[!code-csharp[ModifyFile](../../src/Ekona.Examples/QuickStart.cs?name=ModifyFile)]

Finally, to generate a new game (ROM) file we just need one more line of code to
use the [`NitroRom2Binary`](xref:SceneGate.Ekona.Containers.Rom.NitroRom2Binary)
converter.

[!code-csharp[WriteGame](../../src/Ekona.Examples/QuickStart.cs?name=WriteGame)]

> [!TIP]  
> Check-out the [cartridge](features/cartridge.md) section to learn about the
> optional parameters of these converters!

### Cartridge information

Once we have opened a game, we can access to all the information from its header
easily via the `system/info` node.

[!code-csharp[HeaderInfo](../../src/Ekona.Examples/QuickStart.cs?name=HeaderInfo)]

In a similar way, you can access to the information from the banner like the
game title in different languages:

[!code-csharp[BannerTitle](../../src/Ekona.Examples/QuickStart.cs?name=BannerTitle)]

You can also export the game icon. If it's a DSi game, it may even have an
animated icon that you can export as GIF!

[!code-csharp[ExportIcon](../../src/Ekona.Examples/QuickStart.cs?name=ExportIcon)]
