// Binary2NitroRom.cs
//
// Copyright (c) 2019 SceneGate Team
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
namespace Ekona.Containers.Converters
{
    using Ekona.Containers.Formats;
    using System;
    using Yarhl.IO;
    using Yarhl.FileFormat;
    using Yarhl.FileSystem;

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// ROM sections:
    ///   Header (0x0000-0x4000)
    ///   ARM9 Binary
    ///    |_ARM9
    ///    |_ARM9 tail data
    ///    |_ARM9 Overlays Tables
    ///    |_ARM9 Overlays
    ///   ARM7 Binary
    ///    |_ARM7
    ///    |_ARM7 Overlays Tables
    ///    |_ARM7 Overlays
    ///   FNT (File Name Table)
    ///    |_Main tables
    ///    |_Subtables (names)
    ///   FAT (File Allocation Table)
    ///    |_Files offset
    ///    |_Start offset
    ///    |_End offset
    ///   Banner
    ///    |_Header 0x20
    ///    |_Icon (Bitmap + palette) 0x200 + 0x20
    ///    |_Game titles (Japanese, English, French, German, Italian, Spanish) 6 * 0x100
    ///   Files...
    /// </remarks>
    public class Binary2NitroRom :
        IConverter<BinaryFormat, NitroRom>,
        IConverter<NitroRom, BinaryFormat>
    {
        /// <summary>
        /// Read the internal info of a ROM file.
        /// </summary>
        /// <param name="str">Stream to read from.</param>
        public NitroRom Convert(BinaryFormat source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            DataStream stream = source.Stream;

            NitroRom rom = new NitroRom();

            // Read header
            stream.Seek(0, SeekMode.Start);
            rom.Header = new RomHeader();
            rom.Header.Read(str);

            // Read banner
            var bannerStream = new DataStream(str, rom.Header.BannerOffset, Banner.Size);
            var bannerFile = new Node("Banner", new BinaryFormat(bannerStream));
            rom.Banner = new Banner();
            rom.Banner.Initialize(bannerFile);
            rom.Banner.Read();

            // Read file system: FAT and FNT
            rom.FileSystem = new FileSystem();
            rom.FileSystem.Initialize(null, rom.Header);
            rom.FileSystem.Read(str);

            // Assign common tags (they will be assigned recursively)
            rom.File.Tags["_Device_"] = "NDS";
            rom.File.Tags["_MakerCode_"] = rom.Header.MakerCode;
            rom.File.Tags["_GameCode_"] = rom.Header.GameCode;

            // Get the ROM folders with files and system files.
            rom.FileSystem.SystemFolder.AddFile(bannerFile);

            rom.File.AddFolder(rom.FileSystem.Root);
            rom.File.AddFolder(rom.FileSystem.SystemFolder);

            return rom;
        }

        /// <summary>
        /// Write a new ROM data.
        /// </summary>
        /// <param name="str">Stream to write to.</param>
        public BinaryFormat Convert(NitroRom source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            BinaryFormat binary = new BinaryFormat();

            DataStream headerStr = new DataStream(new System.IO.MemoryStream(), 0, 0);
            DataStream fileSysStr = new DataStream(new System.IO.MemoryStream(), 0, 0);
            DataStream bannerStr = new DataStream(new System.IO.MemoryStream(), 0, 0);

            source.Root.Write(fileSysStr);

            //source.Banner.UpdateCrc();
            source.Banner.Write(bannerStr);

            source.Header.BannerOffset = (uint)(source.Header.HeaderSize + fileSysStr.Length);
            //source.Header.UpdateCrc();
            source.Header.Write(headerStr);

            headerStr.WriteTo(binary.Stream);
            fileSysStr.WriteTo(binary.Stream);
            bannerStr.WriteTo(binary.Stream);
            source.Root.WriteFiles(binary.Stream);
            binary.Stream.WriteUntilLength(FileSystem.PaddingByte, (int)source.Header.CartridgeSize);

            headerStr.Dispose();
            fileSysStr.Dispose();
            bannerStr.Dispose();

            return binary;
        }

        
        //public void UpdateCrc()
        //{
        //	// Write temporaly the header
        //	DataStream data = new DataStream(new System.IO.MemoryStream(), 0, 0);
        //	this.Write(data);

        //	data.Seek(0, SeekMode.Origin);
        //	this.HeaderCRC16 = Yarhl.Utils.Checksums.Crc16.Run(data, 0x15E);
        //	this.HeaderCRC   = true;

        //	data.Dispose();

        //	// The checksum of the logo won't be calculated again
        //	// since it must have always the original value if we
        //	// want to boot the game correctly (0xCF56)
        //}

        /// <summary>
        /// Write the header of a NDS game ROM.
        /// </summary>
        /// <param name="str">Stream to write the header.</param>
        public void Write(DataStream str)
        {
            DataWriter dw = new DataWriter(str);

            dw.Write(GameTitle);            // At 0x00
            dw.Write(GameCode);
            dw.Write(MakerCode);            // At 0x10
            dw.Write(UnitCode);
            dw.Write(EncryptionSeed);
            dw.Write((byte)(Math.Log(CartridgeSize, 2) - MinCartridge));
            dw.Write(Reserved);
            dw.Write(RomVersion);
            dw.Write(InternalFlags);
            dw.Write(Arm9Offset);           // At 0x20
            dw.Write(Arm9EntryAddress);
            dw.Write(Arm9RamAddress);
            dw.Write(Arm9Size);
            dw.Write(Arm7Offset);           // At 0x30
            dw.Write(Arm7EntryAddress);
            dw.Write(Arm7RamAddress);
            dw.Write(Arm7Size);
            dw.Write(FntOffset);            // At 0x40
            dw.Write(FntSize);
            dw.Write(FatOffset);
            dw.Write(FatSize);
            dw.Write(Ov9TableOffset);       // At 0x50
            dw.Write(Ov9TableSize);
            dw.Write(Ov7TableOffset);
            dw.Write(Ov7TableSize);
            dw.Write(FlagsRead);
            dw.Write(FlagsInit);
            dw.Write(BannerOffset);
            dw.Write(SecureCRC16);
            dw.Write(RomTimeout);
            dw.Write(Arm9Autoload);
            dw.Write(Arm7Autoload);
            dw.Write(SecureDisable);
            dw.Write(RomSize);
            dw.Write(HeaderSize);
            dw.Write(Reserved2);
            dw.Write(NintendoLogo);
            dw.Write(LogoCRC16);
            dw.Write(HeaderCRC16);
            dw.Write(DebugRomOffset);
            dw.Write(DebugSize);
            dw.Write(DebugRamAddress);
            dw.Write(Reserved3);
            dw.Write(Unknown);
        }

        /// <summary>
        /// Read a the header from a NDS game ROM.
        /// </summary>
        /// <param name="str">Stream with the ROM. Must be at the correct position.</param>
        public void Read(DataStream str)
        {
            long startPosition = str.Position;
            DataReader dr = new DataReader(str);

            GameTitle = dr.ReadString(12);
            GameCode = dr.ReadString(4);
            MakerCode = dr.ReadString(2);
            UnitCode = dr.ReadByte();
            EncryptionSeed = dr.ReadByte();
            CartridgeSize = (uint)(1 << (MinCartridge + dr.ReadByte()));
            Reserved = dr.ReadBytes(9);
            RomVersion = dr.ReadByte();
            InternalFlags = dr.ReadByte();
            Arm9Offset = dr.ReadUInt32();
            Arm9EntryAddress = dr.ReadUInt32();
            Arm9RamAddress = dr.ReadUInt32();
            Arm9Size = dr.ReadUInt32();
            Arm7Offset = dr.ReadUInt32();
            Arm7EntryAddress = dr.ReadUInt32();
            Arm7RamAddress = dr.ReadUInt32();
            Arm7Size = dr.ReadUInt32();
            FntOffset = dr.ReadUInt32();
            FntSize = dr.ReadUInt32();
            FatOffset = dr.ReadUInt32();
            FatSize = dr.ReadUInt32();
            Ov9TableOffset = dr.ReadUInt32();
            Ov9TableSize = dr.ReadUInt32();
            Ov7TableOffset = dr.ReadUInt32();
            Ov7TableSize = dr.ReadUInt32();
            FlagsRead = dr.ReadUInt32();
            FlagsInit = dr.ReadUInt32();
            BannerOffset = dr.ReadUInt32();
            SecureCRC16 = dr.ReadUInt16();
            RomTimeout = dr.ReadUInt16();
            Arm9Autoload = dr.ReadUInt32();
            Arm7Autoload = dr.ReadUInt32();
            SecureDisable = dr.ReadUInt64();
            RomSize = dr.ReadUInt32();
            HeaderSize = dr.ReadUInt32();
            Reserved2 = dr.ReadBytes(56);
            NintendoLogo = dr.ReadBytes(156);
            LogoCRC16 = dr.ReadUInt16();
            HeaderCRC16 = dr.ReadUInt16();
            DebugRomOffset = dr.ReadUInt32();
            DebugSize = dr.ReadUInt32();
            DebugRamAddress = dr.ReadUInt32();
            Reserved3 = dr.ReadUInt32();

            int unknownSize = (int)(HeaderSize - (str.Position - startPosition));
            Unknown = dr.ReadBytes(unknownSize);
        }

        
        //public void UpdateCrc()
        //{
        //	// Write temporaly the banner
        //	DataStream data = new DataStream(new System.IO.MemoryStream(), 0, 0);
        //	Write(data);

        //	data.Seek(0x20, SeekMode.Start);
        //	Crc16 = Yarhl.Utils.Checksums.Crc16.Run(data, 0x0820);

        //	if (Version == 2) {
        //		data.Seek(0x20, SeekMode.Start);
        //		Crc16v2 = Yarhl.Utils.Checksums.Crc16.Run(data, 0x0920);
        //	}

        //	data.Dispose();
        //}

        /// <summary>
        /// Write the banner to a stream.
        /// </summary>
        /// <param name="str">Stream to write to.</param>
		// public void Write(DataStream str)
        // {
        //     DataWriter dw = new DataWriter(str) {
        //         DefaultEncoding = Encoding.Unicode,
        //         Endianness = EndiannessMode.LittleEndian
        //     };

        //     dw.Write(Version);
        //     dw.Write(Crc16);
        //     dw.Write(Crc16v2);
        //     dw.Write(Reserved);
        //     dw.Write(TileData);
        //     dw.Write(Palette);
        //     dw.Write(JapaneseTitle, 0x100);
        //     dw.Write(EnglishTitle, 0x100);
        //     dw.Write(FrenchTitle, 0x100);
        //     dw.Write(GermanTitle, 0x100);
        //     dw.Write(ItalianTitle, 0x100);
        //     dw.Write(SpanishTitle, 0x100);

        //     str.WritePadding(FileSystem.PaddingByte, FileSystem.PaddingAddress);
        // }

        /// <summary>
        /// Read a banner from a stream.
        /// </summary>
        /// <param name="str">Stream to read from.</param>
        // public void Read(DataStream str)
        // {
        //     DataReader dr = new DataReader(str) {
        //         DefaultEncoding = Encoding.Unicode,
        //         Endianness = EndiannessMode.LittleEndian
        //     };

        //     Version = dr.ReadUInt16();
        //     Crc16 = dr.ReadUInt16();
        //     Crc16v2 = dr.ReadUInt16();
        //     Reserved = dr.ReadBytes(0x1A);
        //     TileData = dr.ReadBytes(0x200);
        //     Palette = dr.ReadBytes(0x20);
        //     JapaneseTitle = dr.ReadString(0x100);
        //     EnglishTitle = dr.ReadString(0x100);
        //     FrenchTitle = dr.ReadString(0x100);
        //     GermanTitle = dr.ReadString(0x100);
        //     ItalianTitle = dr.ReadString(0x100);
        //     SpanishTitle = dr.ReadString(0x100);
        // }
    }
}
