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
            source.Stream.Position = 0;
            var reader = new DataReader(source.Stream);

            var header = new RomHeader();
            header.ProgramInfo.GameTitle = reader.ReadString(12).Replace("\0", string.Empty);
            header.ProgramInfo.GameCode = reader.ReadString(4);
            header.ProgramInfo.MakerCode = reader.ReadString(2);
            header.ProgramInfo.UnitCode = reader.ReadByte();
            header.ProgramInfo.EncryptionSeed = reader.ReadByte();
            header.ProgramInfo.CartridgeSize = RomInfo.MinimumCartridgeSize * (1 << reader.ReadByte());

            source.Stream.Position += 7; // reserved
            header.ProgramInfo.DsiFlags = reader.ReadByte();
            header.ProgramInfo.Region = reader.ReadByte();
            header.ProgramInfo.RomVersion = reader.ReadByte();
            header.ProgramInfo.AutoStartFlag = reader.ReadByte();

            header.SectionInfo.Arm9Offset = reader.ReadUInt32();
            header.ProgramInfo.Arm9EntryAddress = reader.ReadUInt32();
            header.ProgramInfo.Arm9RamAddress = reader.ReadUInt32();
            header.SectionInfo.Arm9Size = reader.ReadUInt32();
            header.SectionInfo.Arm7Offset = reader.ReadUInt32();
            header.ProgramInfo.Arm7EntryAddress = reader.ReadUInt32();
            header.ProgramInfo.Arm7RamAddress = reader.ReadUInt32();
            header.SectionInfo.Arm7Size = reader.ReadUInt32();
            header.SectionInfo.FntOffset = reader.ReadUInt32();
            header.SectionInfo.FntSize = reader.ReadUInt32();
            header.SectionInfo.FatOffset = reader.ReadUInt32();
            header.SectionInfo.FatSize = reader.ReadUInt32();
            header.SectionInfo.Overlay9TableOffset = reader.ReadUInt32();
            header.SectionInfo.Overlay9TableSize = reader.ReadUInt32();
            header.SectionInfo.Overlay7TableOffset = reader.ReadUInt32();
            header.SectionInfo.Overlay7TableSize = reader.ReadUInt32();

            header.ProgramInfo.FlagsRead = reader.ReadUInt32();
            header.ProgramInfo.FlagsInit = reader.ReadUInt32();
            header.SectionInfo.BannerOffset = reader.ReadUInt32();
            header.ProgramInfo.ChecksumSecureArea = new ChecksumInfo<ushort>(reader.ReadUInt16());
            header.ProgramInfo.SecureAreaDelay = reader.ReadUInt16();
            header.ProgramInfo.Arm9Autoload = reader.ReadUInt32();
            header.ProgramInfo.Arm7Autoload = reader.ReadUInt32();
            header.ProgramInfo.SecureDisable = reader.ReadUInt64();
            header.SectionInfo.RomSize = reader.ReadUInt32();
            header.SectionInfo.HeaderSize = reader.ReadUInt32();

            source.Stream.Position += 0x38;
            header.CopyrightLogo = reader.ReadBytes(156);
            header.ProgramInfo.ChecksumLogo = reader.ValidateCrc16(0xC0, 0x9C);
            header.ProgramInfo.ChecksumHeader = reader.ValidateCrc16(0x00, 0x15E);

            header.ProgramInfo.DebugRomOffset = reader.ReadUInt32();
            header.ProgramInfo.DebugSize = reader.ReadUInt32();
            header.ProgramInfo.DebugRamAddress = reader.ReadUInt32();

            return header;
        }
    }
}
