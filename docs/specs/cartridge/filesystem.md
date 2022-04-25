# Cartridge file system format

The cartridge format contains a file system with a files and directories to
define the boundaries of the data area. Files and directory have also a name and
ID that the game uses to access.

The file system is defined in two sections:

- _File Name Table_ (FNT): defines the hierarchy of directories and files and
  their name.
- _File Access Table_ (FAT): defines the offset and length of each file.

## File Access Table

The start and length of this section is defined in the
[cartridge header](header.md) at `0x48` and `0x4C`.

The table consists of a set of start and end offset for each file. This also
includes the overlays for ARM9 and ARM7 but it does not define the offsets for
the ARM9/ARM7 programs (defined in the header).

The access of the table is by ID defined in the
[File Name Table](#file-name-table)

For each file, there is:

| Offset | Format | Description                |
| ------ | ------ | -------------------------- |
| 0x00   | uint   | Start position of the file |
| 0x04   | uint   | End position of the file   |

Tips:

- Dividing the section length by 8 we can get the total number of files.
- Subtracting the second value to the first one we can get the file length.
- File start offsets have a padding to the cartridge padding block: 512 bytes.
  The padding byte in the file data content is `0xFF`.

## File Name Table

The start and length of this section is defined in the
[cartridge header](header.md) at `0x40` and `0x44`.

The section actually contains two tables:

- Directory definition table: defines the hierarchy between directories and its
  files
- Name entry table: defines the names for files and directories.

### Directory definition table

There are 8 bytes per directory. The entries are sorted by directory ID. To get
the information for the directory ID `0xFnnn`, we would go to
`fnt_offset + ((dir_id & 0x0FFF) * 8)`.

| Offset | Format | Description                                             |
| ------ | ------ | ------------------------------------------------------- |
| 0x00   | uint   | Name entry relative offset for directory and file names |
| 0x04   | ushort | First file ID for the directory, or 0 if empty.         |
| 0x06   | ushort | Directory parent ID or number of directories for root   |

### Name entry table

The section is a sequence of names with metadata with no random order access.
After finding the directory in the
[directory table](#directory-definition-table), jump to this section with the
relative offset and keep reading names until finding an empty token (`0x00`).

The format of the entries is:

| Offset | Format | Description                               |
| ------ | ------ | ----------------------------------------- |
| 0x00   | byte   | Node type (see below)                     |
| 0x01   | string | Name                                      |
| ..     | ushort | Only for directories, ID of the directory |

The _node type_ byte is defined as:

- Bit 7: if set, the next bytes apply to a directory, otherwise it's a file.
- Bits 0-6: length of the name

The name of the files are in order. This means that the first file name is for
the file ID defined in the second field of the
[directory table](#directory-definition-table).

After every file and directory names of the current directory entry, the byte
`0x00` must be present.

## Files ID and order

The files ID are assigned in order following a _depth-first_ recursion of the
file system hierarchy. The overlays for ARM9 (if any) must always be the first
ones, following the ID defined in the overlay table, but their file data is
written in a different section.

The order of the file data in the data area seems to not follow any pattern. It
does not follow the order of the file IDs. Most likely the official SDK was able
to do incremental builds and new files during development were being placed at
the end each time.

> [!TIP]  
> In order to create small patches, tools should try to replicate the original
> order of the files. This is the case of _Ekona_, it adds the tag
> `scenegate.ekona.physical_id` to each file node.
