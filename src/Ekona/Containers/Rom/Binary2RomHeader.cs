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

            if (header.ProgramInfo.UnitCode != DeviceUnitKind.DS) {
                ReadDsiFields(reader, header);
            }

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

        private static void ReadDsiFields(DataReader reader, RomHeader header)
        {
            DsiProgramInfo info = header.ProgramInfo.DsiInfo;
            RomSectionInfo sections = header.SectionInfo;

            // Pos: 0x180
            reader.Stream.Position = 0x180;
            info.GlobalMemoryBanks = ReadGlobalMemoryBankSettings(reader);

            // Pos: 0x194
            info.LocalMemoryBanksArm9 = ReadLocalMemoryBankSettings(reader);

            // Pos: 0x1A0
            info.LocalMemoryBanksArm7 = ReadLocalMemoryBankSettings(reader);
            info.GlobalMbk9Settings = reader.ReadInt24();
            info.GlobalWramCntSettings = reader.ReadByte();

            // Pos: 0x1B0
            info.Region = reader.ReadUInt32();
            info.AccessControl = reader.ReadUInt32();
            info.Arm7ScfgExt7Setting = reader.ReadUInt32();
            reader.Stream.Position += 4; // ProgramFeatures flags read with DS fields

            // Pos: 0x1C0
            sections.Arm9iOffset = reader.ReadUInt32();
            reader.Stream.Position += 4; // reserved
            info.Arm9iRamAddress = reader.ReadUInt32();
            sections.Arm9iSize = reader.ReadUInt32();

            // Pos: 0x1D0
            sections.Arm7iOffset = reader.ReadUInt32();
            info.SdDeviceListArm7RamAddress = reader.ReadUInt32();
            info.Arm7iRamAddress = reader.ReadUInt32();
            sections.Arm7iSize = reader.ReadUInt32();

            // Pos: 0x1E0
            sections.DigestNitroOffset = reader.ReadUInt32();
            sections.DigestNitroLength = reader.ReadUInt32();
            sections.DigestTwilightOffset = reader.ReadUInt32();
            sections.DigestTwilightLength = reader.ReadUInt32();

            // Pos: 0x1F0
            sections.DigestSectorHashtableOffset = reader.ReadUInt32();
            sections.DigestSectorHashtableLength = reader.ReadUInt32();
            sections.DigestBlockHashtableOffset = reader.ReadUInt32();
            sections.DigestBlockHashtableLength = reader.ReadUInt32();

            // Pos: 0x200
            sections.DigestSectorSize = reader.ReadUInt32();
            sections.DigestBlockSectorCount = reader.ReadUInt32();
            sections.BannerLength = reader.ReadUInt32();
            info.SdShared200Length = reader.ReadByte();
            info.SdShared201Length = reader.ReadByte();
            info.EulaVersion = reader.ReadByte();
            info.UseRatings = reader.ReadByte();

            // Pos: 0x210
            sections.DsiRomLength = reader.ReadUInt32();
            info.SdShared202Length = reader.ReadByte();
            info.SdShared203Length = reader.ReadByte();
            info.SdShared204Length = reader.ReadByte();
            info.SdShared205Length = reader.ReadByte();
            info.Arm9iParametersTableOffset = reader.ReadUInt32();
            info.Arm7iParametersTableOffset = reader.ReadUInt32();

            // Pos: 0x220
            sections.ModcryptArea1Offset = reader.ReadUInt32();
            sections.ModcryptArea1Length = reader.ReadUInt32();
            sections.ModcryptArea2Offset = reader.ReadUInt32();
            sections.ModcryptArea2Length = reader.ReadUInt32();

            // Pos: 0x230
            info.TitleId = reader.ReadUInt64();
            info.SdPublicSaveLength = reader.ReadUInt32();
            info.SdPrivateSaveLength = reader.ReadUInt32();

            // Pos: 0x2F0
            reader.Stream.Position = 0x2F0;
            info.AgeRatingCero = reader.ReadByte();
            info.AgeRatingEsrb = reader.ReadByte();
            reader.Stream.Position++; // reserved
            info.AgeRatingUsk = reader.ReadByte();
            info.AgeRatingPegiEurope = reader.ReadByte();
            reader.Stream.Position++; // reserved
            info.AgeRatingPegiPortugal = reader.ReadByte();
            info.AgeRatingPegiUk = reader.ReadByte();
            info.AgeRatingAgcb = reader.ReadByte();
            info.AgeRatingGrb = reader.ReadByte();
            reader.Stream.Position += 6; // reserved

            // Pos: 0x300
            info.Arm9SecureMac = reader.ReadHMACSHA1();
            info.Arm7Mac = reader.ReadHMACSHA1();
            info.DigestMain = reader.ReadHMACSHA1();
            reader.Stream.Position += 0x14; // Banner HMAC already read with DS fields
            info.Arm9iMac = reader.ReadHMACSHA1();
            info.Arm7iMac = reader.ReadHMACSHA1();
            reader.Stream.Position += 0x28; // HMAC for whitelist DS games
            info.Arm9Mac = reader.ReadHMACSHA1();
        }

        private static void ValidateChecksums(DataStream stream, ProgramInfo info)
        {
            // We don't validate the checksum of the secure area as it's outside the header. Same for HMAC.
            var crcGen = new NitroCrcGenerator();
            info.ChecksumLogo.Validate(crcGen.GenerateCrc16(stream, 0xC0, 0x9C));
            info.ChecksumHeader.Validate(crcGen.GenerateCrc16(stream, 0x00, 0x15E));
        }

        private static GlobalMemoryBankSettings[] ReadGlobalMemoryBankSettings(DataReader reader)
        {
            var settings = new GlobalMemoryBankSettings[5 * 4];
            for (int i = 0; i < settings.Length; i++) {
                byte data = reader.ReadByte();
                settings[i] = new GlobalMemoryBankSettings {
                    Processor = (MemoryBankProcessor)(data & 0x3),
                    OffsetSlot = (byte)((data >> 2) & 0x7),
                    Enabled = (data >> 7) == 1,
                };
            }

            return settings;
        }

        private static LocalMemoryBankSettings[] ReadLocalMemoryBankSettings(DataReader reader)
        {
            var settings = new LocalMemoryBankSettings[3];

            // MBK6 - WRAM A
            uint mbk6 = reader.ReadUInt32();
            settings[0] = new LocalMemoryBankSettings {
                StartAddressSlot = (int)((mbk6 >> 4) & 0xFF),
                ImageSize = (int)((mbk6 >> 12) & 0x3),
                EndAddressSlot = (int)((mbk6 >> 20) & 0x1FF),
            };

            // MBK 7 and 8 - WRAM B and C
            for (int i = 0; i < 2; i++) {
                uint data = reader.ReadUInt32();
                settings[i + 1] = new LocalMemoryBankSettings {
                    StartAddressSlot = (int)((data >> 3) & 0x1FF),
                    ImageSize = (int)((data >> 12) & 0x3),
                    EndAddressSlot = (int)((data >> 19) & 0x3FF),
                };
            }

            return settings;
        }
    }
}
