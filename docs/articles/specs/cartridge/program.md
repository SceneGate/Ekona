# Cartridge programs

The DS and DSi have two processors: ARM9 and ARM7. Each processor loads a
different program and run it in parallel. The hardware contains synchronization
mechanism between the two processors and there is an area of the RAM that it's
shared between the two processors.

The developers usually only can write code for the ARM9 processor. The ARM7
processor is reserved for the system code like controlling the hardware (sound,
touchscreen). The compiled object files with the assembly code are usually named
after the processors: `arm9.bin` and `arm7.bin`.

DSi-enhanced and exclusive games contain two additional programs for the same
processors. These files contains DSi exclusive code that it's only load when the
game runs on a DSi console. These files are load into a different RAM address
and are called from the regular program file. It allows to run the program on
regular DS consoles with additional features when it runs in DSi consoles. These
files are usually named `arm9i.bin` and `arm7i.bin`.

The header of the cartridge contains several fields for these files including:

- File offset
- File length
- Load address into the RAM
- Entry point (first line of code to execute)
- [Program parameters](#program-parameters)

## Overlays

The RAM memory of the DS devices is very limited and it needs to hold the
program and its data. To save memory, large code program files are usually split
by features into different files (similar to the concept of libraries). These
files are named _overlays_ and can be load and unload from memory at any moment
from the main (_armX.bin_) program. For instance, some games put all its code
for battles on an separate overlay. This overlay is loaded into memory when the
battle starts and removed when it finishes. Different overlays may be load into
the same RAM address but at different times.

Practically speaking, programs only contain overlays for the ARM9 processor.

### Overlay information table

Before the data of each overlay file, there is a table that contains a set of
fields for each overlay. For each overlay there are 32-bytes as follow:

| Offset | Format | Description                         |
| ------ | ------ | ----------------------------------- |
| 0x00   | uint   | ID                                  |
| 0x04   | uint   | Load RAM address                    |
| 0x08   | uint   | File length to load into RAM        |
| 0x0C   | uint   | BSS data section length             |
| 0x10   | uint   | Static initialization start address |
| 0x14   | uint   | Static initialization end address   |
| 0x18   | uint   | Overlay file ID (same as [0x00])    |
| 0x1C   | uint   | Flags                               |

Flags:

| Bits  | Description                                 |
| ----- | ------------------------------------------- |
| 0-23  | Compressed file length (0 for uncompressed) |
| 24    | If set, the file is compressed              |
| 25    | If set, the overlay is digitally signed     |
| 26-31 | Reserved                                    |

## Secure area

The first 16 KB of the ARM9 program code lands into the _secure area_ section of
the ROM. These bytes must be load using a special command of the cartridge
protocol. Additionally the first 2 KB of the _secure area_ (so of the file) are
_KEY1_ encrypted.

The first 2 KB doesn't usually contain code. Instead its format is:

| Offset | Format      | Description                                   |
| ------ | ----------- | --------------------------------------------- |
| 0x00   | char[8]     | Encryption token `encryObj`                   |
| 0x08   | uint        | Constant `FF DE FF E7 FF DE`                  |
| 0x0E   | ushort      | CRC-16 of the next 0x7F0 bytes.               |
| 0x10   | byte[0x7F0] | Garbage / random bytes with small code blocks |

The console decrypts first the initial 8 bytes. If the result does not match
`encryObj`, then it assumes the content is wrong and corrupts the rest of the
data with the same bytes as the token. If the data match, then overwrites these
characters with `FF DE FF E7 FF DE FF E7` (to hide the token).

The BIOS nor the firmware do not verify the CRC-16. They only verify the first
8-bytes with the token. For this reason, this area is usually used for hooks and
patch code.

### Disable secure area

The secure area can be disabled by setting a special constant value in the
cartridge header at 0x78. The constant is encrypted with
[KEY1](security.md#blowfish-key1) encryption. It's decrypted value is
`NmMdOnly`.

## Program parameters

The ARM9 program contains a table inside the file that defines additional
parameters about the format like the location of _ITCM_ code or the
[_BSS_](https://en.wikipedia.org/wiki/.bss) area. These parameters are used at
runtime to initialize the program before calling the actual program entry-point.

The location of this table is defined in the extended header for DSi games. In
the case of DS games, there are 12 bytes after the ARM9 program file (also known
as _arm9 tail_) that defines additional pointers. These bytes are not included
as part of the length of the ARM9 program file. The format of these bytes is:

| Offset | Format | Description                                           |
| ------ | ------ | ----------------------------------------------------- |
| 0x00   | uint   | Constant: `0x2106C0DE` (_nitrocode_ marker)           |
| 0x04   | uint   | Offset to table parameters (see below)                |
| 0x08   | uint   | Offset to [HMAC-SHA1 table for overlays](security.md) |

If the _nitrocode_ marker is present, then this structure exists. This constant
or marker is used across the _arm9_ program code to identify structures of data
after the compilation phase.

> [!TIP]  
> The constant number is a play of hexadecimal numbers to create the word:
> _nitro code_. _Nitro_ was the project name of the DS device. The meaning of
> the bytes is:
>
> - `2`: _ni_ japanese number
> - `10`: _to_ japanese number
> - `6`: _ro_ japanese number
> - `C0DE`: _code_ with hexadecimal numbers

The format of the program parameter table is:

| Offset | Format   | Description                                           |
| ------ | -------- | ----------------------------------------------------- |
| 0x00   | uint     | First ITCM block info                                 |
| 0x04   | uint     | Last ITCM block info                                  |
| 0x08   | uint     | ITCM data offset                                      |
| 0x0C   | uint     | BSS data offset                                       |
| 0x10   | uint     | BSS data end offset                                   |
| 0x14   | uint     | Compressed program size                               |
| 0x18   | uint     | SDK version with format: major.minor.build            |
| 0x1C   | uint     | _nitrocode_ marker                                    |
| 0x20   | uint     | _nitrocode_ marker in big endian                      |
| 0x24   | string[] | Frameworks used with format `[<company>:<framework>]` |

An _ITCM block info_ consists of two 32-bits integer values: target RAM address
and block size. The data should be copied consecutive starting from _ITCM data
offset_ field value.

> [!NOTE]  
> Programs regenerating a cartridge file should take into account the
> _compressed program size_ field value and update it if the _program file_
> changes and it's compressed value is different.

## Program entrypoint

The entrypoint code for the _ARM9_ program is defined by the SDK. It runs a set
of initialization steps before calling the actual program _main_ function.

These initializations includes:

- Set the program stack address
- Moves program code and data to TCM areas (cache areas that run faster)
- Clean the _BSS_ area
- Decompress the rest of the program file (reverse LZSS compression or BLZ)
