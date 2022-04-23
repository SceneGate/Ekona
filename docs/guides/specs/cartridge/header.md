# Cartridge header

A binary cartridge format starts with the program header. It contains
information about the program as well as the address and size of the rest of the
sections of the cartridge format.

> [!NOTE]  
> This document refers to DS/DSi programs. This also includes games.

## Specification

### Base header

Every DS and DSi program has the following header fields consisting in the first
0x160 bytes of the cartridge.

| Offset | Format    | Description                                            |
| ------ | --------- | ------------------------------------------------------ |
| 0x00   | char[12]  | Program short title                                    |
| 0x0C   | char[4]   | Program unique code                                    |
| 0x10   | char[2]   | Developer unique code                                  |
| 0x12   | byte      | Console, 0: DS, 2: DSi enhanced, 3: DSi exclusive      |
| 0x13   | byte      | KEY2 encryption seed select (0 to 7)                   |
| 0x14   | byte      | Cartridge size: 128 KB \* 2^n                          |
| 0x15   | byte[8]   | Reserved: zero                                         |
| 0x1D   | byte      | Region, 0: base, 0x40: Korea, 0x80: China              |
| 0x1E   | byte      | Program version                                        |
| 0x1F   | byte      | Autostart flag, bit2: skip "Press button" after health |
| 0x20   | uint      | ARM9 program offset                                    |
| 0x24   | uint      | ARM9 program entrypoint RAM address                    |
| 0x28   | uint      | ARM9 program load RAM address                          |
| 0x2C   | uint      | ARM9 program size                                      |
| 0x30   | uint      | ARM7 program offset                                    |
| 0x34   | uint      | ARM7 program entrypoint RAM address                    |
| 0x38   | uint      | ARM7 program load RAM address                          |
| 0x3C   | uint      | ARM7 program size                                      |
| 0x40   | uint      | File name table offset                                 |
| 0x44   | uint      | File name table size                                   |
| 0x48   | uint      | File address table offset                              |
| 0x4C   | uint      | File address table size                                |
| 0x50   | uint      | Overlay table info offset for ARM9                     |
| 0x54   | uint      | Overlay table info size for ARM9                       |
| 0x58   | uint      | Overlay table info offset for ARM7                     |
| 0x5C   | uint      | Overlay table info size for ARM7                       |
| 0x60   | uint      | IO port 0x40001A4 flags for normal commands            |
| 0x64   | uint      | IO port 0x40001A4h flags for KEY1 commands             |
| 0x68   | uint      | Banner offset                                          |
| 0x6C   | ushort    | CRC-16 for encrypted secure area of ARM9 program       |
| 0x6E   | ushort    | ARM9 program secure area delay (131kHz units)          |
| 0x70   | uint      | ARM9 program auto-load list hook RAM address           |
| 0x74   | uint      | ARM7 program auto-load list hook RAM address           |
| 0x78   | byte[8]   | Encrypted KEY1 value to disable secure area            |
| 0x80   | uint      | Program size (without cartridge final padding)         |
| 0x84   | uint      | Header size                                            |
| 0x88   | byte[56]  | Reserved                                               |
| 0xC0   | byte[156] | Copyright logo                                         |
| 0x15C  | ushort    | CRC-16 for copyright logo                              |
| 0x15E  | ushort    | CRC-16 for first 0x15E header bytes                    |
| 0x160  | uint      | Debugging data offset                                  |
| 0x164  | uint      | Debugging data size                                    |
| 0x168  | uint      | Debugging data RAM load address                        |

### DS extended header

DS games released after the DSi contain an extended header used to verify its
authenticity and avoid piracy with flashcards. These fields are only load on DSi
devices.

| Offset | Format    | Description                                             |
| ------ | --------- | ------------------------------------------------------- |
| 0x88   | uint      | Offset to parameters offset in ARM9 program             |
| 0x8C   | uint      | Offset to parameters offset in ARM7 program             |
| 0x18F  | byte      | [Extended program features](#extended-program-features) |
| 0x33C  | byte[20]  | HMAC-SHA1 of the banner (phase 3)                       |
| 0x378  | byte[20]  | HMAC-SHA1 of the header, ARM9 and ARM7 (phase 1)        |
| 0x38C  | byte[20]  | HMAC-SHA1 of the overlays for ARM9 (phase 2)            |
| 0xF80  | byte[128] | RSA signature of the first 0xE00 bytes of the header    |

### DSi header

Similar to the _DS extended header_, DSi enhanced or exclusive games extend the
header to 0xE00 bytes. They also overwrite some values from the standard DS
header.

| Offset | Format  | Description                               |
| ------ | ------- | ----------------------------------------- |
| 0x1C   | byte    | [DSi crypto mode](#dsi-crypto-mode)       |
| 0x1D   | byte    | [Program start jump](#program-start-jump) |
| 0x90   | ushort  | DS cartridge region end (512 KB units)    |
| 0x92   | ushort  | DSi cartridge region start (512 KB units) |
| 0x180  | uint[5] | Global MBK 1 to 5 settings                |
| 0x194  | uint[3] | ARM9 local MBK 6 to 8 settings            |
| 0x1A0  | uint[3] | ARM7 local MBK 6 to 8 settings            |
| 0x1AC  | int24   | Global MBK 9 settings                     |
| 0x1AF  | byte    | WRAM CNT settings                         |

TODO...

## Detailed types

### Extended program features

8-bits flag enumeration:

| Bit | Meaning if set                                                    |
| --- | ----------------------------------------------------------------- |
| 0   | Use touchscreen and sound controllers in DSi mode                 |
| 1   | Require to accept EULA agreement                                  |
| 2   | Launcher uses icon from banner.sav instead of cartridge banner    |
| 3   | Launcher shows Wi-Fi connection icon                              |
| 4   | Launcher shows DS wireless icon                                   |
| 5   | Header contains HMAC of the banner                                |
| 6   | Header contains HMAC and RSA signature of the header and programs |
| 7   | Developer application                                             |

### DSi crypto mode

8-bits flag enumeration:

| Bit | Meaning if set                                           |
| --- | -------------------------------------------------------- |
| 0   | Program contains DSi exclusive region area (arm9i/arm7i) |
| 1   | Cartridge contains areas with modcrypt encryption        |
| 2   | Modcrypt encryption uses the debug key                   |
| 3   | Disable debug features                                   |

### Program start jump

Regular enumeration:

| Value | Description                                         |
| ----- | --------------------------------------------------- |
| 0     | Normal jump copying header in the RAM               |
| 1     | Special / temporary launch used for the system menu |
