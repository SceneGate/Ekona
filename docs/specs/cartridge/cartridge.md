# Cartridge format

The binary cartridge format (also known as `SRL`) is the way to pack the
information and files for a DS and DSi program. This is the format that physical
cartridge carts have, but also the format used by digital content like DSiWare
and utility programs from the DSi firmware.

> [!NOTE]  
> These documents refer to DS/DSi programs. This also includes games.

## Cartridge memory map

The raw binary format of the cartridge is based on four different regions. It
maps the different commands we need to send to the cartridge chip to retrieve
the data:

| Offset    | Length | Description                                         |
| --------- | ------ | --------------------------------------------------- |
| 0x00      | 0x1000 | Header                                              |
| 0x1000    | 0x3000 | Unknown, not readable                               |
| 0x4000    | 0x8000 | Secure area, first 2 KB with _KEY1_ encryption      |
| 0x8000    | ...    | Data area                                           |
| 0xZZ00000 | 0x3000 | Unknown, not readable                               |
| 0xZZ03000 | 0x4000 | ARM9i secure area, usually with modcrypt encryption |
| 0xZZ07000 | ...    | DSi data area                                       |

## DS / DSi program sections

Practically speaking, official DS / DSi program maps these memory map areas to
the following sections:

- [Header](header.md)
- [Unknown, not readable](#unknown-regions), usually `0x00`
- [ARM9 processor program](program.md) (actual program code):
  - Program code: starts at `0x4000` so first 2 KB are _KEY1_ encrypted.
  - [Program extended parameters](program.md#program-parameters) (DS only)
  - [Overlay table info](program.md#overlay-information-table)
  - [Overlay files](program.md#overlays)
- [ARM7 processor program](program.md) (system code):
  - Program code
  - [Overlay table info](program.md#overlay-information-table)
  - [Overlay files](program.md#overlays) (usually empty)
- [File name table](filesystem.md#file-name-table)
- [File access table](filesystem.md#file-access-table)
- [Banner](banner.md)
- Files data
- Additionally, for DSi programs:
  - Digest HMACs
  - [Unknown, not readable](#unknown-regions)
  - [ARM9i program](program.md)
  - [ARM7i program](program.md)

### Padding

Every section, including each file data is padded to blocks of 512 bytes with
the byte `0xFF`.

On DS programs, the last file is not padded.

A DS program region (nitro) ends after the _digest HMACs_. The additional DSi
program region (twilight) starts with the _unknown region_. Between these two
regions there is a special padding to fill the last block of 1 MB with the byte
`0xFF`.

### Unknown regions

The header contains `0x3000` unknown bytes. These bytes cannot be read as the
cartridge protocol does not support getting data from this address. On digital
programs like firmware utilities (like the launcher), this area seems to have
random bytes.

This is similar to what happen at the DSi program region. It starts with
`0x3000` unknown bytes. These bytes cannot be dumped because the cartridge
protocol silently redirect any read command from these region to `0x8000`. These
means that if we try to dump these `0x3000` bytes, we will get from the physical
cart three times `0x1000` bytes of data from `0x8000` (arm9 after secure area).
