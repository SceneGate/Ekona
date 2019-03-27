//-----------------------------------------------------------------------
// <copyright file="Banner.cs" company="none">
// Copyright (C) 2013
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
// <author>pleoNeX</author>
// <email>benito356@gmail.com</email>
// <date>16/02/2013</date>
//-----------------------------------------------------------------------
namespace Ekona.Formats
{
    using System.Text;
    using Yarhl.FileFormat;
    using Yarhl.IO;

    /// <summary>
    /// Represents the banner of a NDS game ROM.
    /// </summary>
    public sealed class Banner : Format
    {

        /// <summary>
        /// Gets the size of the banner (with padding).
        /// </summary>
        public static uint Size
        {
            get { return 0x840 + 0x1C0; }
        }

        public ushort Version // Always 1
        {
            get;
            set;
        }

        public ushort Crc16 // CRC-16 of structure, from 0x20 to 0x83
        {
            get;
            set;
        }

        public ushort Crc16v2 // CRC-16 of structure, from 0x20 to 0x93, version 2 only
        {
            get;
            set;
        }

        public byte[] Reserved // 26 bytes
        {
            get;
            set;
        }

        public byte[] TileData // 512 bytes
        {
            get;
            set;
        }

        public byte[] Palette // 32 bytes
        {
            get;
            set;
        }

        public string JapaneseTitle // 256 bytes
        {
            get;
            set;
        }

        public string EnglishTitle // 256 bytes
        {
            get;
            set;
        }

        public string FrenchTitle // 256 bytes
        {
            get;
            set;
        }

        public string GermanTitle // 256 bytes
        {
            get;
            set;
        }

        public string ItalianTitle // 256 bytes
        {
            get;
            set;
        }

        public string SpanishTitle // 256 bytes
        {
            get;
            set;
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
		public void Write(DataStream str)
        {
            DataWriter dw = new DataWriter(str) 
            {
                DefaultEncoding = Encoding.Unicode,
                Endianness = EndiannessMode.LittleEndian  
            };

            dw.Write(Version);
            dw.Write(Crc16);
            dw.Write(Crc16v2);
            dw.Write(Reserved);
            dw.Write(TileData);
            dw.Write(Palette);
            dw.Write(JapaneseTitle, 0x100);
            dw.Write(EnglishTitle, 0x100);
            dw.Write(FrenchTitle, 0x100);
            dw.Write(GermanTitle, 0x100);
            dw.Write(ItalianTitle, 0x100);
            dw.Write(SpanishTitle, 0x100);

            str.WritePadding(FileSystem.PaddingByte, FileSystem.PaddingAddress);
        }

        /// <summary>
        /// Read a banner from a stream.
        /// </summary>
        /// <param name="str">Stream to read from.</param>
        public void Read(DataStream str)
        {
            DataReader dr = new DataReader(str)
            {
                DefaultEncoding = Encoding.Unicode,
                Endianness = EndiannessMode.LittleEndian
            };

            Version = dr.ReadUInt16();
            Crc16 = dr.ReadUInt16();
            Crc16v2 = dr.ReadUInt16();
            Reserved = dr.ReadBytes(0x1A);
            TileData = dr.ReadBytes(0x200);
            Palette = dr.ReadBytes(0x20);
            JapaneseTitle = dr.ReadString(0x100);
            EnglishTitle = dr.ReadString(0x100);
            FrenchTitle = dr.ReadString(0x100);
            GermanTitle = dr.ReadString(0x100);
            ItalianTitle = dr.ReadString(0x100);
            SpanishTitle = dr.ReadString(0x100);
        }

    }
}
