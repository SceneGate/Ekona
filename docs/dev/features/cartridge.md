# Cartridge / ROM converter and format

Games and software in general that run in a DS or DSi device have the same
binary format. It is the same format for content dumped from a physical
cartridge, digital downloads (_DSiWare_) or digital content (firmware apps like
the menu launcher).

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

[!code-csharp[OpenGame](../../../src/Ekona.Examples/QuickStart.cs?name=OpenGame)]

The converter will use the following converters to transform some content:

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

## Writing a ROM

Any container node (`NodeContainerFormat`) that follows the
[structure defined by the cartridge](#nitrorom-tree) can be converter (packed)
into binary format using the converter
[`NitroRom2Binary`](xref:SceneGate.Ekona.Containers.Rom.NitroRom2Binary). The
input does not need to have
[`NitroRom`](xref:SceneGate.Ekona.Containers.Rom.NitroRom) format.

> [!NOTE]
>
> - _Input format_: `NodeContainerFormat` (like `NitroRom` or a directory)
> - _Output format_: `BinaryFormat`
> - _Parameters_: (_Optional_)

[!code-csharp[WriteGame](../../../src/Ekona.Examples/QuickStart.cs?name=WriteGame)]

> [!TIP]  
> TagsTODO

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

## Secure area encryption (KEY1)

The converters does not decrypt or encrypt the secure area of the ARM9 program
automatically. They will present the ARM9 as it is inside the cartridge. But
they will do encrypt or decrypt the ARM9 with _modcrypt_ if the header specifies
so.

You can decrypt or encrypt the file later by using the class
[`NitroKey1Encryption`](xref:SceneGate.Ekona.Security.NitroKey1Encryption)

[!code-csharp[DecryptEncryptArm9](../../../src/Ekona.Examples/Cartridge.cs?name=DecryptEncryptArm9)]

## Animated icon
