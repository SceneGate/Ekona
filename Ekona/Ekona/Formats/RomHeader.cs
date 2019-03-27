//-----------------------------------------------------------------------
// <copyright file="RomHeader.cs" company="none">
// Copyright (C) 2019
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by 
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful, 
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General Public License for more details. 
//
//   You should have received a copy of the GNU General Public License
//   along with this program.  If not, see "http://www.gnu.org/licenses/". 
// </copyright>
// <author>pleoNeX, priverop</author>
// <email>benito356@gmail.com</email>
// <date>16/02/2019</date>
//-----------------------------------------------------------------------
namespace Ekona.Formats
{
    using System;
    using Yarhl.FileFormat;
    using Yarhl.IO;

    /// <summary>
    /// Header of the ROM.
    /// </summary>
	public sealed class RomHeader : Format
    {
        private const int MinCartridge = 17;
        
        private char[] gameTitle;
        private char[] gameCode;
        private char[] makerCode;
           
        /// <summary>
        /// Gets or sets the short game title.
        /// </summary>
        public string GameTitle
        {
            get { return new string(this.gameTitle); }
            set { this.gameTitle = value.ToCharArray(); }
        }

        /// <summary>
        /// Gets or sets the game code.
        /// </summary>
        public string GameCode
        {
            get { return new string(this.gameCode); }
            set { this.gameCode = value.ToCharArray(); }
        }

        /// <summary>
        /// Gets or sets the maker code.
        /// </summary>
        public string MakerCode
        {
            get { return new string(this.makerCode); }
            set { this.makerCode = value.ToCharArray(); }
        }

        /// <summary>
        /// Gets or sets the unit code.
        /// </summary>
        public byte UnitCode
        {
            get;
            set;
        }

        public byte EncryptionSeed
        {
            get;
            set;
        }

        public uint CartridgeSize // Can change
        {
            get;
            set;
        }

        public byte[] Reserved
        {
            get;
            set;
        }

        public byte RomVersion
        {
            get;
            set;
        }

        public byte InternalFlags
        {
            get;
            set;
        }

        public uint Arm9Offset
        {
            get;
            set;
        }

        public uint Arm9EntryAddress
        {
            get;
            set;
        }

        public uint Arm9RamAddress
        {
            get;
            set;
        }

        public uint Arm9Size // Can change
        {
            get;
            set;
        }

        public uint Arm7Offset
        {
            get;
            set;
        }

        public uint Arm7EntryAddress
        {
            get;
            set;
        }

        public uint Arm7RamAddress
        {
            get;
            set;
        }

        public uint Arm7Size
        {
            get;
            set;
        }

        public uint FntOffset // Can change
        {
            get;
            set;
        }

        public uint FntSize // Can change
        {
            get;
            set;
        }

        public uint FatOffset // Can change
        {
            get;
            set;
        }

        public uint FatSize // Can change
        {
            get;
            set;
        }

        public uint Ov9TableOffset // Can change
        {
            get;
            set;
        }

        public uint Ov9TableSize // Can change
        {
            get;
            set;
        }

        public uint Ov7TableOffset
        {
            get;
            set;
        }

        public uint Ov7TableSize
        {
            get;
            set;
        }

        public uint FlagsRead // Control register flags for read
        {
            get;
            set;
        }

        public uint FlagsInit // Control register flags for init
        {
            get;
            set;
        }

        public uint BannerOffset // Can change
        {
            get;
            set;
        }

        public ushort SecureCRC16 // Secure area CRC16 0x4000 - 0x7FFF
        {
            get;
            set;
        }

        public ushort RomTimeout
        {
            get;
            set;
        }

        public uint Arm9Autoload
        {
            get;
            set;
        }

        public uint Arm7Autoload
        {
            get;
            set;
        }

        public ulong SecureDisable // Magic number for unencrypted mode
        {
            get;
            set;
        }

        public uint RomSize // Can change
        {
            get;
            set;
        }

        public uint HeaderSize // Can change
        {
            get;
            set;
        }

        public byte[] Reserved2 // 56 bytes
        {
            get;
            set;
        }

        public byte[] NintendoLogo // 156 bytes
        {
            get;
            set;
        }

        public ushort LogoCRC16
        {
            get;
            set;
        }

        public ushort HeaderCRC16
        {
            get;
            set;
        }

        public bool SecureCRC
        {
            get;
            set;
        }

        public bool LogoCRC
        {
            get;
            set;
        }

        public bool HeaderCRC
        {
            get;
            set;
        }

        public uint DebugRomOffset // only if debug
        {
            get;
            set;
        }

        public uint DebugSize // version with
        {
            get;
            set;
        }

        public uint DebugRamAddress // 0 = none, SIO and 8 MB
        {
            get;
            set;
        }

        public uint Reserved3 // Zero filled transfered and stored but not used
        {
            get;
            set;
        }

        public byte[] Unknown // Rest of the header
        {
            get;
            set;
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

            gameTitle = dr.ReadChars(12);
            gameCode = dr.ReadChars(4);
            makerCode = dr.ReadChars(2);
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

    }
}
