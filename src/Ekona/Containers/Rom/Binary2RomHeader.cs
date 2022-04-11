// Copyright(c) 2021 SceneGate
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
using System;
using SceneGate.Ekona.Security;
using Yarhl.FileFormat;
using Yarhl.IO;

namespace SceneGate.Ekona.Containers.Rom
{
    /// <summary>
    /// Converter for binary ROM header into an object.
    /// </summary>
    public class Binary2RomHeader : IConverter<IBinary, RomHeader>
    {
        /// <summary>
        /// Gets the offset in the header containing the header size value.
        /// </summary>
        public static int HeaderSizeOffset => 0x84;

        /// <summary>
        /// Convert a binary format into a ROM header object.
        /// </summary>
        /// <param name="source">The stream to read.</param>
        /// <returns>The new ROM header.</returns>
        public RomHeader Convert(IBinary source)
        {
            ArgumentNullException.ThrowIfNull(source);

            source.Stream.Position = 0;
            var reader = new DataReader(source.Stream);
            var header = new RomHeader();

            ReadDsFields(reader, header);
            ValidateChecksums(reader.Stream, header.ProgramInfo);

            return header;
        }

        private static void ReadDsFields(DataReader reader, RomHeader header)
        {
            // Pos: 0x00
            header.ProgramInfo.GameTitle = reader.ReadString(12).Replace("\0", string.Empty);
            header.ProgramInfo.GameCode = reader.ReadString(4);

            // Pos: 0x10
            header.ProgramInfo.MakerCode = reader.ReadString(2);
            header.ProgramInfo.UnitCode = (DeviceUnitKind)reader.ReadByte();
            header.ProgramInfo.EncryptionSeed = reader.ReadByte();
            header.ProgramInfo.CartridgeSize = ProgramInfo.MinimumCartridgeSize * (1 << reader.ReadByte());
            reader.Stream.Position += 7; // reserved
            header.ProgramInfo.DsiCryptoFlags = (DsiCryptoMode)reader.ReadByte();
            header.ProgramInfo.Region = reader.ReadByte();
            header.ProgramInfo.Version = reader.ReadByte();
            header.ProgramInfo.AutoStartFlag = reader.ReadByte();

            // Pos: 0x20
            header.SectionInfo.Arm9Offset = reader.ReadUInt32();
            header.ProgramInfo.Arm9EntryAddress = reader.ReadUInt32();
            header.ProgramInfo.Arm9RamAddress = reader.ReadUInt32();
            header.SectionInfo.Arm9Size = reader.ReadUInt32();

            // Pos: 0x30
            header.SectionInfo.Arm7Offset = reader.ReadUInt32();
            header.ProgramInfo.Arm7EntryAddress = reader.ReadUInt32();
            header.ProgramInfo.Arm7RamAddress = reader.ReadUInt32();
            header.SectionInfo.Arm7Size = reader.ReadUInt32();

            // Pos: 0x40
            header.SectionInfo.FntOffset = reader.ReadUInt32();
            header.SectionInfo.FntSize = reader.ReadUInt32();
            header.SectionInfo.FatOffset = reader.ReadUInt32();
            header.SectionInfo.FatSize = reader.ReadUInt32();

            // Pos: 0x50
            header.SectionInfo.Overlay9TableOffset = reader.ReadUInt32();
            header.SectionInfo.Overlay9TableSize = reader.ReadInt32();
            header.SectionInfo.Overlay7TableOffset = reader.ReadUInt32();
            header.SectionInfo.Overlay7TableSize = reader.ReadInt32();

            // Pos: 0x60
            header.ProgramInfo.FlagsRead = reader.ReadUInt32();
            header.ProgramInfo.FlagsInit = reader.ReadUInt32();
            header.SectionInfo.BannerOffset = reader.ReadUInt32();
            header.ProgramInfo.ChecksumSecureArea = reader.ReadCrc16();
            header.ProgramInfo.SecureAreaDelay = reader.ReadUInt16();

            // Pos: 0x70
            header.ProgramInfo.Arm9Autoload = reader.ReadUInt32();
            header.ProgramInfo.Arm7Autoload = reader.ReadUInt32();
            header.ProgramInfo.SecureDisable = reader.ReadUInt64();

            // Pos: 0x80
            header.SectionInfo.RomSize = reader.ReadUInt32();
            header.SectionInfo.HeaderSize = reader.ReadUInt32();
            header.ProgramInfo.Arm9ParametersTableOffset = reader.ReadUInt32();
            header.ProgramInfo.Arm7ParametersTableOffset = reader.ReadUInt32();

            // Pos: 0x90
            header.ProgramInfo.NitroRegionEnd = reader.ReadUInt16();
            header.ProgramInfo.TwilightRegionStart = reader.ReadUInt16();

            // Pos: 0xC0
            reader.Stream.Position = 0xC0;
            header.CopyrightLogo = reader.ReadBytes(156);
            header.ProgramInfo.ChecksumLogo = reader.ReadCrc16();
            header.ProgramInfo.ChecksumHeader = reader.ReadCrc16();

            // Pos: 0x160
            header.ProgramInfo.DebugRomOffset = reader.ReadUInt32();
            header.ProgramInfo.DebugSize = reader.ReadUInt32();
            header.ProgramInfo.DebugRamAddress = reader.ReadUInt32();

            // Pos: 0x1BF
            reader.Stream.Position = 0x1BF;
            header.ProgramInfo.ProgramFeatures = (DsiRomFeatures)reader.ReadByte();

            // Pos: 0x33C
            reader.Stream.Position = 0x33C;
            header.ProgramInfo.BannerMac = reader.ReadHMACSHA1();

            // Pos: 0x378
            reader.Stream.Position = 0x378;
            header.ProgramInfo.ProgramMac = reader.ReadHMACSHA1();
            header.ProgramInfo.OverlaysMac = reader.ReadHMACSHA1();

            // Pos: 0xF80
            reader.Stream.Position = 0xF80;
            header.ProgramInfo.Signature = reader.ReadSignatureSHA1RSA();
        }

        private static void ValidateChecksums(DataStream stream, ProgramInfo info)
        {
            // We don't validate the checksum of the secure area as it's outside the header. Same for HMAC.
            var crcGen = new NitroCrcGenerator();
            info.ChecksumLogo.Validate(crcGen.GenerateCrc16(stream, 0xC0, 0x9C));
            info.ChecksumHeader.Validate(crcGen.GenerateCrc16(stream, 0x00, 0x15E));
        }
    }
}
