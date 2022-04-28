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

## Program parameters

TODO.
