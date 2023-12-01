# Cartridge / ROM converter and format

Games and software in general that run in a DS or DSi device have the same
binary format. It is the same format for content dumped from a physical
cartridge, digital downloads (_DSiWare_) or digital content (firmware apps like
the menu launcher).

The converters
[`Binary2NitroRom`](xref:SceneGate.Ekona.Containers.Rom.Binary2NitroRom) and
[`NitroRom2Binary`](xref:SceneGate.Ekona.Containers.Rom.NitroRom2Binary) support
DS and DSi software.

You can find the technical details of the format specification
[here](../../specs/cartridge/cartridge.md).

## Reading a ROM

The format has information about the software and contains a list of files. This
format can be converted / unpacked into a tree _nodes_ with the converter
[`Binary2NitroRom`](xref:SceneGate.Ekona.Containers.Rom.Binary2NitroRom).

> [!NOTE]
>
> - _Input format_: `IBinary` (like `BinaryFormat` from a file)
> - _Output format_: [`NitroRom`](xref:SceneGate.Ekona.Containers.Rom.NitroRom)
>   (inherits from `NodeContainerFormat`)
> - _Parameters_: (_Optional_)
>   [`DsiKeyStore`](xref:SceneGate.Ekona.Security.DsiKeyStore)

[!code-csharp[OpenGame](../../../../src/Ekona.Examples/QuickStart.cs?name=OpenGame)]

The converter will use the following converters to transform some binary data
into the specific format:

- Header using
  [`Binary2RomHeader`](xref:SceneGate.Ekona.Containers.Rom.Binary2RomHeader)
- Banner using
  [`Binary2Banner`](xref:SceneGate.Ekona.Containers.Rom.Binary2Banner)

If the converter parameter is present and it contains the required keys, then
converter will also verify the hashes (HMAC) and signature of the binary
cartridge. If it is missing, it will skip the verification.

> [!WARNING]  
> The verification of the hashes may take some extra time. In DSi games there
> are hashes that verify the full content of the cartridge. The time it is
> proportional to the size of the binary ROM.

Some DSi games may have
[modcrypt](../../specs/cartridge/security.md#modcrypt-aes-ctr) encryption. In
that case the converter will also decrypt the files. This operation does not
require keys.

The data nodes contains two _tags_ that are used when
[writing back the ROM](#writing-a-rom):

- `scenegate.ekona.id`: Index in the file system. Some times used by software to
  access files.
- `scenegate.ekona.physical_id`: Index of the file in the data block.

## Writing a ROM

Any container node (`NodeContainerFormat`) that follows the
[structure defined by the cartridge](#nitrorom-tree) can be converter (packed)
into the ROM / cartridge binary format using the converter
[`NitroRom2Binary`](xref:SceneGate.Ekona.Containers.Rom.NitroRom2Binary).

> [!NOTE]
>
> - _Input format_: `NodeContainerFormat` (like `NitroRom` or a directory)
> - _Output format_: `BinaryFormat`
> - _Parameters_: (_Optional_)
>   [`NitroRom2BinaryParams`](xref:SceneGate.Ekona.Containers.Rom.NitroRom2BinaryParams)
>   - `OutputStream`: `Stream` to write the output ROM data. If not provided,
>     the converter will create a `Stream` in memory (caution).
>   - `KeyStore`: keys to use to re-generate the HMACs from the header and
>     digest hashes. If not provided, the hashes will be not be generated. They
>     may be _zero bytes_ or the ones from the original ROM if already set.
>   - `DecompressedProgram`: set it if the provided _arm9_ program file is
>     decompressed with BLZ compression. In that case, the converter will update
>     the compressed length in the _arm9_ program with zero. Otherwise it will
>     set the current _arm9_ program length.

[!code-csharp[WriteGame](../../../../src/Ekona.Examples/QuickStart.cs?name=WriteGame)]

The converter will use the following converters to transform the program and
banner information into binary data:

- Header using
  [`RomHeader2Binary`](xref:SceneGate.Ekona.Containers.Rom.RomHeader2Binary)
- Banner using
  [`Banner2Binary`](xref:SceneGate.Ekona.Containers.Rom.Banner2Binary)

> [!WARNING]  
> If you are generating a big ROM, pass the parameter with an `OutputStream`
> from a disk file. Otherwise the output data may take a lot of RAM memory.

The nodes from the container given as a input must have the formats and names
specified in [`NitroRom` tree](#nitrorom-tree). This means every game file must
be already in `IBinary` (`BinaryFormat`).

> [!NOTE]  
> When the `KeyStore` parameter is set, the hashes are re-generated. This
> operation may take some extra time and it is not needed. Even if the hashes
> won't be valid anymore, emulators and custom firmwares like _unlaunch_ skip
> the verification. In any case, the signature is not possible to generate
> (unknown private key) so the re-generated ROM won't be valid even if the keys
> are provided.

The file IDs are assigned by following a depth-first approach. If every node
contains the tag `scenegate.ekona.id` then its value is used instead.

The file data is written following the order of the file IDs. If every node
contains the tag `scenegate.ekona.physical_id`, then this order is used.

> [!TIP]  
> To reduce the size of patches, ensure that the nodes contains the
> `scenegate.ekona.physical_id` tags, so the data is written in the same order.
> You can do that by always applying changes to the file system read from a
> cartridge, instead of creating it from scratch.

## `NitroRom` tree

The [`NitroRom`](xref:SceneGate.Ekona.Containers.Rom.NitroRom) format inherits
from `NodeContainerFormat` and just provide helper properties to access to some
nodes easily. You can access to the data and files via its properties or
navigating the nodes.

The structure of the tree nodes is defined and required by some converters. It
follows this structure from the root node (returned node by converter):

- `system`: (container) ROM information and program files.
  - `info`: ([`ProgramInfo`](xref:SceneGate.Ekona.Containers.Rom.ProgramInfo))
    Program information.
  - `copyright_logo`: (`BinaryFormat`) Header copyright logo.
  - `banner`: (container) Program banner
    - `info`: ([`Banner`](xref:SceneGate.Ekona.Containers.Rom.Banner)) Program
      banner content (titles and checksums).
    - `icon`: (`IndexedPaletteImage`) Program icon.
    - `animated`: (container) Animated icons on DSi software.
      - `bitmap{0-7}`: (`IndexedImage`) Bitmaps for the animated icon frames.
      - `palettes`: (`PaletteCollection`) Palettes for the animated icon frames.
      - `animation`:
        ([`IconAnimationSequence`](xref:SceneGate.Ekona.Containers.Rom.IconAnimationSequence))
        Animation information for the icon.
  - `arm9`: (`BinaryFormat`) Executable for the ARM9 processor.
  - `overlays9`: (container) Overlay directory for ARM9 processor.
    - `overlay_{i}`: (`BinaryFormat`) Library overlay for ARM9 processor.
  - `arm7`: (`BinaryFormat`) Executable for the ARM7 processor.
  - `overlays7`: (container) Overlay directory for ARM7 processor.
    - `overlay_{i}`: (`BinaryFormat`) Library overlay for ARM7 processor.
- `data`: (container) Root directory for program data files
  - Every node under this container will be either another container or a
    `BinaryFormat`.

## Secure area encryption (KEY1)

The converters does not decrypt or encrypt the secure area of the ARM9 program
automatically. They will present the ARM9 as it is inside the cartridge. But
they will do encrypt or decrypt the ARM9 with _modcrypt_ if the header specifies
so.

You can decrypt or encrypt the file later by using the class
[`NitroKey1Encryption`](xref:SceneGate.Ekona.Security.NitroKey1Encryption)

[!code-csharp[DecryptEncryptArm9](../../../../src/Ekona.Examples/Cartridge.cs?name=DecryptEncryptArm9)]

## Animated icon

DSi software brings an animated icon. You can export this icon into standard GIF
format by also using the [Texim](https://github.com/SceneGate/Texim) library.
You can use the property
[`SupportAnimatedIcon`](xref:SceneGate.Ekona.Containers.Rom.Banner.SupportAnimatedIcon)
to know if the banner contains the animated icon.

The converter
[`IconAnimation2AnimatedImage`](xref:SceneGate.Ekona.Containers.Rom.IconAnimation2AnimatedImage)
can convert the `animated` node from the banner into the `AnimatedFullImage`
type from _Texim_. You can use later its converters to convert to GIF format.

> [!NOTE]
>
> - _Input format_: `NodeContainerFormat`
> - _Output format_: `AnimatedFullImage`
> - _Parameters_: none

[!code-csharp[ExportIconGif](../../../../src/Ekona.Examples/Cartridge.cs?name=ExportIconGif)]

> [!NOTE]  
> It is not possible to import the icon from a GIF file, only export. If you
> want to modify the animated icon, edit each frame and information. For
> instance, export each bitmap with a palette into PNG format and the animation
> information as a JSON/YAML file.
