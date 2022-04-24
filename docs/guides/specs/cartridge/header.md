# Cartridge header

A binary cartridge format starts with the program header. It contains
information about the program as well as the address and size of the rest of the
sections of the cartridge format.

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
| 0x1BF  | byte      | [Extended program features](#extended-program-features) |
| 0x33C  | byte[20]  | HMAC-SHA1 of the banner (phase 3)                       |
| 0x378  | byte[20]  | HMAC-SHA1 of the header, ARM9 and ARM7 (phase 1)        |
| 0x38C  | byte[20]  | HMAC-SHA1 of the overlays for ARM9 (phase 2)            |
| 0xF80  | byte[128] | RSA signature of the first 0xE00 bytes of the header    |

### DSi header

Similar to the _DS extended header_, DSi enhanced or exclusive games extend the
header to 0xE00 bytes. They also overwrite some values from the standard DS
header.

| Offset | Format    | Description                                             |
| ------ | --------- | ------------------------------------------------------- |
| 0x1C   | byte      | [DSi crypto mode](#dsi-crypto-mode)                     |
| 0x1D   | byte      | [Program start jump](#program-start-jump)               |
| 0x90   | ushort    | DS cartridge region end (512 KB units)                  |
| 0x92   | ushort    | DSi cartridge region start (512 KB units)               |
| 0x180  | uint[5]   | Global MBK 1 to 5 settings                              |
| 0x194  | uint[3]   | ARM9 local MBK 6 to 8 settings                          |
| 0x1A0  | uint[3]   | ARM7 local MBK 6 to 8 settings                          |
| 0x1AC  | int24     | Global MBK 9 settings                                   |
| 0x1AF  | byte      | WRAM CNT settings                                       |
| 0x1B0  | uint      | [Region](#program-region) lock                          |
| 0x1B4  | uint      | [Access control flags](#access-control-flags)           |
| 0x1B8  | uint      | SCFG extended features ARM7                             |
| 0x1BC  | byte[3]   | Reserved                                                |
| 0x1BF  | byte      | [Extended program features](#extended-program-features) |
| 0x1C0  | uint      | ARM9 DSi program offset                                 |
| 0x1C4  | byte[4]   | Reserved                                                |
| 0x1C8  | uint      | ARM9 DSi program RAM load address                       |
| 0x1CC  | uint      | ARM9 DSi program size                                   |
| 0x1D0  | uint      | ARM7 DSi program offset                                 |
| 0x1D4  | uint      | SD / eMMC device list ARM7 RAM address                  |
| 0x1D8  | uint      | ARM7 DSi program RAM load address                       |
| 0x1DC  | uint      | ARM7 DSi program size                                   |
| 0x1E0  | uint      | Digest DS region start offset                           |
| 0x1E4  | uint      | Digest DS region length                                 |
| 0x1E8  | uint      | Digest DSi region start offset                          |
| 0x1EC  | uint      | Digest DSi region length                                |
| 0x1F0  | uint      | Digest sector hashtable offset                          |
| 0x1F4  | uint      | Digest sector hashtable length                          |
| 0x1F8  | uint      | Digest block hashtable offset                           |
| 0x1FC  | uint      | Digest block hashtable length                           |
| 0x200  | uint      | Digest sector size                                      |
| 0x204  | uint      | Digest block sector count                               |
| 0x208  | uint      | Banner length                                           |
| 0x20C  | byte      | SD / eMMC file `shared2/0000` length                    |
| 0x20D  | byte      | SD / eMMC file `shared2/0001` length                    |
| 0x20E  | byte      | EULA agreement version                                  |
| 0x20F  | byte      | Use ratings                                             |
| 0x210  | uint      | Program size including DSi areas                        |
| 0x214  | byte      | SD / eMMC file `shared/0002` length                     |
| 0x215  | byte      | SD / eMMC file `shared/0003` length                     |
| 0x216  | byte      | SD / eMMC file `shared/0004` length                     |
| 0x217  | byte      | SD / eMMC file `shared/0005` length                     |
| 0x218  | uint      | ARM9 DSi program parameters table relative offset       |
| 0x21C  | uint      | ARM7 DSi program parameters table relative offset       |
| 0x220  | uint      | Modcrypt encrypted area 1 offset                        |
| 0x224  | uint      | Modcrypt encrypted area 1 length                        |
| 0x228  | uint      | Modcrypt encrypted area 2 offset                        |
| 0x22C  | uint      | Modcrypt encrypted area 2 length                        |
| 0x230  | ulong     | Title ID (similar to Wii / 3DS)                         |
| 0x238  | uint      | SD / eMMC `public.sav` length                           |
| 0x23C  | uint      | SD / eMMC `private.sav` length                          |
| 0x240  | byte[176] | Reserved                                                |
| 0x2F0  | byte      | [Age rating](#age-rating) CERO (Japan)                  |
| 0x2F1  | byte      | [Age rating](#age-rating) ESRB (US / Canada)            |
| 0x2F2  | byte      | Reserved                                                |
| 0x2F3  | byte      | [Age rating](#age-rating) USK (Germany)                 |
| 0x2F4  | byte      | [Age rating](#age-rating) PEGI                          |
| 0x2F5  | byte      | Reserved                                                |
| 0x2F6  | byte      | [Age rating](#age-rating) PEGI Portugal                 |
| 0x2F7  | byte      | [Age rating](#age-rating) PEGI UK and BBFC              |
| 0x2F8  | byte      | [Age rating](#age-rating) AGCB (Australia)              |
| 0x2F9  | byte      | [Age rating](#age-rating) GRB (South Korea)             |
| 0x2FA  | byte[6]   | Reserved                                                |
| 0x300  | byte[20]  | HMAC-SHA1 of ARM9 program with encrypted secure area    |
| 0x314  | byte[20]  | HMAC-SHA1 of the ARM7 program                           |
| 0x328  | byte[20]  | HMAC-SHA1 of the digest block area                      |
| 0x33C  | byte[20]  | HMAC-SHA1 of the banner                                 |
| 0x350  | byte[20]  | HMAC-SHA1 of the ARM9 DSi program                       |
| 0x364  | byte[20]  | HMAC-SHA1 of the ARM7 DSi program                       |
| 0x378  | byte[20]  | Not used (only extended DS header)                      |
| 0x38C  | byte[20]  | Not used (only extended DS header)                      |
| 0x3A0  | byte[20]  | HMAC-SHA1 of ARM9 program without secure area           |
| 0x3B4  | ...       | Reserved                                                |
| 0xE00  | ...       | Debug arguments                                         |
| 0xF80  | byte[128] | RSA signature of the first 0xE00 bytes of the header    |

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

### Program region

32-bits flag enumeration:

| Bit | Allowed region if set |
| --- | --------------------- |
| 0   | Japan                 |
| 1   | USA                   |
| 2   | Europe                |
| 3   | Australia             |
| 4   | China                 |
| 5   | Korea                 |
| All | Region free           |

### Access control flags

32-bits flag enumeration:

| Bit | Meaning if set                                         |
| --- | ------------------------------------------------------ |
| 0   | Use common key                                         |
| 1   | Use AES slot B                                         |
| 2   | Use AES slot C                                         |
| 3   | Allow access to SD card device                         |
| 4   | Allow access to NAND                                   |
| 5   | Game card power on                                     |
| 6   | Software uses `shared2` file from storage SD / eMMC    |
| 7   | Sign camera photo JPEG files for launcher (AES slot B) |
| 8   | Game card DS mode                                      |
| 9   | SSL client certificates (AES slot A)                   |
| 10  | Sign camera photo JPEG files for user (AES slot B)     |
| 11  | Read access for photos                                 |
| 12  | Write access for photos                                |
| 13  | Read access to SD card                                 |
| 14  | Write access to SD card                                |
| 15  | Read access to cartridge save files                    |
| 16  | Write access to cartridge save files                   |
| 17  | Use debugger common client key                         |

### Age rating

| Bits | Description                    |
| ---- | ------------------------------ |
| 0-4  | Age                            |
| 5    | Reserved                       |
| 6    | Prohibited in country          |
| 7    | Age rating enabled for country |

## Unknown region

According to the field `[0x84]`, the header size is always `0x4000` bytes.
However, only the first `0x1000` can be read by the DS / DSi device. The region
`0x1000 .. 0x3000` cannot be accessed outside the physical cart.

On digital programs like firmware utilities (like the launcher), this area seems
to have random bytes.
