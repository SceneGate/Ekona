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

namespace SceneGate.Ekona.Containers.Rom;

/// <summary>
/// Converter for ROM header object into binary stream (serialization).
/// </summary>
public class RomHeader2Binary :
    IInitializer<DsiKeyStore>,
    IConverter<RomHeader, BinaryFormat>
{
    private DsiKeyStore keyStore;

    /// <summary>
    /// Initialize the converter with the key store to enable additional features.
    /// </summary>
    /// <remarks>
    /// The key store is used to generate a special token if `DisableSecureArea` is `true`.
    /// Otherwise, it will always write 0. The required key is `BlowfishDsKey`.
    /// </remarks>
    /// <param name="parameters">The converter parameters.</param>
    /// <exception cref="ArgumentNullException">The argument is null.</exception>
    public void Initialize(DsiKeyStore parameters)
    {
        keyStore = parameters ?? throw new ArgumentNullException(nameof(parameters));
    }

    /// <summary>
    /// Serialize a ROM header object into a binary stream.
    /// </summary>
    /// <param name="source">The header to convert.</param>
    /// <returns>The new binary stream.</returns>
    /// <exception cref="ArgumentNullException">The argument is null.</exception>
    public BinaryFormat Convert(RomHeader source)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        var binary = new BinaryFormat();
        var writer = new DataWriter(binary.Stream);

        WriteDsFields(writer, source);

        if (source.ProgramInfo.UnitCode != DeviceUnitKind.DS) {
            WriteDsiFields(writer, source);
        }

        return binary;
    }

    private void WriteDsFields(DataWriter writer, RomHeader source)
    {
        writer.Write(source.ProgramInfo.GameTitle, 12, nullTerminator: false);
        writer.Write(source.ProgramInfo.GameCode, 4, nullTerminator: false);
        writer.Write(source.ProgramInfo.MakerCode, 2, nullTerminator: false);
        writer.Write((byte)source.ProgramInfo.UnitCode);
        writer.Write(source.ProgramInfo.EncryptionSeed);
        double relativeSize = (double)source.ProgramInfo.CartridgeSize / ProgramInfo.MinimumCartridgeSize;
        byte power2Size = (byte)Math.Ceiling(Math.Log2(relativeSize));
        writer.Write(power2Size);

        writer.WriteTimes(0, 7); // reserved
        writer.Write((byte)source.ProgramInfo.DsiCryptoFlags);

        if (source.ProgramInfo.UnitCode == DeviceUnitKind.DS) {
            byte region = 0;
            if (source.ProgramInfo.Region.HasFlag(ProgramRegion.China)) {
                region |= 0x80;
            }

            if (source.ProgramInfo.Region.HasFlag(ProgramRegion.Korea)) {
                region |= 0x40;
            }

            writer.Write(region);
        } else {
            writer.Write((byte)source.ProgramInfo.DsiInfo.StartJumpKind);
        }

        writer.Write(source.ProgramInfo.Version);
        writer.Write(source.ProgramInfo.AutoStartFlag);

        writer.Write(source.SectionInfo.Arm9Offset);
        writer.Write(source.ProgramInfo.Arm9EntryAddress);
        writer.Write(source.ProgramInfo.Arm9RamAddress);
        writer.Write(source.SectionInfo.Arm9Size);
        writer.Write(source.SectionInfo.Arm7Offset);
        writer.Write(source.ProgramInfo.Arm7EntryAddress);
        writer.Write(source.ProgramInfo.Arm7RamAddress);
        writer.Write(source.SectionInfo.Arm7Size);
        writer.Write(source.SectionInfo.FntOffset);
        writer.Write(source.SectionInfo.FntSize);
        writer.Write(source.SectionInfo.FatOffset);
        writer.Write(source.SectionInfo.FatSize);
        writer.Write(source.SectionInfo.Overlay9TableOffset);
        writer.Write(source.SectionInfo.Overlay9TableSize);
        writer.Write(source.SectionInfo.Overlay7TableOffset);
        writer.Write(source.SectionInfo.Overlay7TableSize);

        writer.Write(source.ProgramInfo.FlagsRead);
        writer.Write(source.ProgramInfo.FlagsInit);
        writer.Write(source.SectionInfo.BannerOffset);
        writer.Write(source.ProgramInfo.ChecksumSecureArea.Hash);
        writer.Write(source.ProgramInfo.SecureAreaDelay);
        writer.Write(source.ProgramInfo.Arm9Autoload);
        writer.Write(source.ProgramInfo.Arm7Autoload);

        if (keyStore is { BlowfishDsKey: { Length: > 0 } } && source.ProgramInfo.DisableSecureArea) {
            var encryption = new NitroKey1Encryption(source.ProgramInfo.GameCode, keyStore);
            byte[] token = encryption.GenerateEncryptedDisabledSecureAreaToken();
            writer.Write(token);
        } else {
            writer.WriteTimes(0, 8);
        }

        writer.Write(source.SectionInfo.RomSize);
        writer.Write(source.SectionInfo.HeaderSize);
        writer.Write(source.ProgramInfo.Arm9ParametersTableOffset);
        writer.Write(source.ProgramInfo.Arm7ParametersTableOffset);
        writer.Write(source.ProgramInfo.NitroRegionEnd);
        writer.Write(source.ProgramInfo.TwilightRegionStart);

        writer.WriteTimes(0, 0x2C);
        writer.Write(source.CopyrightLogo);

        var crcGen = new NitroCrcGenerator();
        source.ProgramInfo.ChecksumLogo.ChangeHash(crcGen.GenerateCrc16(writer.Stream, 0xC0, 0x9C));
        writer.Write(source.ProgramInfo.ChecksumLogo.Hash);

        source.ProgramInfo.ChecksumHeader.ChangeHash(crcGen.GenerateCrc16(writer.Stream, 0x00, 0x015E));
        writer.Write(source.ProgramInfo.ChecksumHeader.Hash);

        writer.Write(source.ProgramInfo.DebugRomOffset);
        writer.Write(source.ProgramInfo.DebugSize);
        writer.Write(source.ProgramInfo.DebugRamAddress);

        writer.WriteUntilLength(0, 0x1BF);
        writer.Write((byte)source.ProgramInfo.ProgramFeatures);

        // Write the HMAC if they exist. NitroRom2Binary regenerates them.
        if (source.ProgramInfo.BannerMac is not null) {
            writer.WriteUntilLength(0, 0x33C);
            writer.Write(source.ProgramInfo.BannerMac.Hash);
        }

        if (source.ProgramInfo.NitroProgramMac is not null) {
            writer.WriteUntilLength(0, 0x378);
            writer.Write(source.ProgramInfo.NitroProgramMac.Hash);
        }

        if (source.ProgramInfo.NitroOverlaysMac is not null) {
            writer.WriteUntilLength(0, 0x38C);
            writer.Write(source.ProgramInfo.NitroOverlaysMac.Hash);
        }

        // We only write the signature if it exists (just to have more identical bytes).
        // Unfortunately we can't generate a new one as the private keys are unknown.
        // Custom firmwares like Unlaunch remove the device checks of the signature
        // so actually the content doesn't matter.
        if (source.ProgramInfo.Signature is not null) {
            writer.WriteUntilLength(0, 0xF80);
            writer.Write(source.ProgramInfo.Signature.Hash);
        }

        writer.WriteUntilLength(0, source.SectionInfo.HeaderSize);
    }

    private static void WriteDsiFields(DataWriter writer, RomHeader source)
    {
        DsiProgramInfo info = source.ProgramInfo.DsiInfo;
        RomSectionInfo sections = source.SectionInfo;

        writer.Stream.Position = 0x180;
        WriteGlobalMemoryBankSettings(writer, info.GlobalMemoryBanks);
        WriteLocalMemoryBankSettings(writer, info.LocalMemoryBanksArm9);
        WriteLocalMemoryBankSettings(writer, info.LocalMemoryBanksArm7);
        writer.Write(info.GlobalMbk9Settings | (info.GlobalWramCntSettings << 24));

        writer.Write((uint)source.ProgramInfo.Region);
        writer.Write(info.AccessControl);
        writer.Write(info.Arm7ScfgExt7Setting);
        writer.Stream.Position += 4; // ProgramFeatures written with DS fields

        writer.Write(sections.Arm9iOffset);
        writer.Write(0);
        writer.Write(info.Arm9iRamAddress);
        writer.Write(sections.Arm9iSize);
        writer.Write(sections.Arm7iOffset);
        writer.Write(info.SdDeviceListArm7RamAddress);
        writer.Write(info.Arm7iRamAddress);
        writer.Write(sections.Arm7iSize);

        writer.Write(sections.DigestNitroOffset);
        writer.Write(sections.DigestNitroLength);
        writer.Write(sections.DigestTwilightOffset);
        writer.Write(sections.DigestTwilightLength);
        writer.Write(sections.DigestSectorHashtableOffset);
        writer.Write(sections.DigestSectorHashtableLength);
        writer.Write(sections.DigestBlockHashtableOffset);
        writer.Write(sections.DigestBlockHashtableLength);
        writer.Write(sections.DigestSectorSize);
        writer.Write(sections.DigestBlockSectorCount);

        writer.Write(sections.BannerLength);
        writer.Write((byte)info.SdShared200Length);
        writer.Write((byte)info.SdShared201Length);
        writer.Write(info.EulaVersion);
        writer.Write(info.UseRatings);
        writer.Write(sections.DsiRomLength);
        writer.Write((byte)info.SdShared202Length);
        writer.Write((byte)info.SdShared203Length);
        writer.Write((byte)info.SdShared204Length);
        writer.Write((byte)info.SdShared205Length);
        writer.Write(info.Arm9iParametersTableOffset);
        writer.Write(info.Arm7iParametersTableOffset);

        writer.Write(sections.ModcryptArea1Offset);
        writer.Write(sections.ModcryptArea1Length);
        writer.Write(sections.ModcryptArea2Offset);
        writer.Write(sections.ModcryptArea2Length);

        writer.Write(info.TitleId);
        writer.Write(info.SdPublicSaveLength);
        writer.Write(info.SdPrivateSaveLength);

        writer.Stream.Position = 0x2F0;
        writer.Write(info.AgeRatingCero);
        writer.Write(info.AgeRatingEsrb);
        writer.Write((byte)0);
        writer.Write(info.AgeRatingUsk);
        writer.Write(info.AgeRatingPegiEurope);
        writer.Write((byte)0);
        writer.Write(info.AgeRatingPegiPortugal);
        writer.Write(info.AgeRatingPegiUk);
        writer.Write(info.AgeRatingAgcb);
        writer.Write(info.AgeRatingGrb);

        writer.Stream.Position = 0x300;
        writer.Write(info.Arm9SecureMac.Hash);
        writer.Write(info.Arm7Mac.Hash);
        writer.Write(info.DigestMain.Hash);
        writer.Stream.Position += 0x14; // Banner HMAC read with DS fields
        writer.Write(info.Arm9iMac.Hash);
        writer.Write(info.Arm7iMac.Hash);
        writer.Stream.Position += 0x28; // DS whitelist macs
        writer.Write(info.Arm9Mac.Hash);
    }

    private static void WriteGlobalMemoryBankSettings(DataWriter writer, GlobalMemoryBankSettings[] settings)
    {
        if (settings is not { Length: 5 * 4 }) {
            throw new FormatException("Expecting 20 global memory bank settings");
        }

        for (int i = 0; i < settings.Length; i++) {
            var setting = settings[i];
            int maxProcessor = (i == 0) ? 1 : 3;
            int maxSlot = (i == 0) ? 3 : 7;
            if ((int)setting.Processor > maxProcessor || setting.OffsetSlot > maxSlot) {
                throw new ArgumentException($"Invalid global memory bank setting for {i}");
            }

            int data = (int)setting.Processor;
            data |= setting.OffsetSlot << 2;
            data |= (setting.Enabled ? 1 : 0) << 7;
            writer.Write((byte)data);
        }
    }

    private static void WriteLocalMemoryBankSettings(DataWriter writer, LocalMemoryBankSettings[] settings)
    {
        if (settings is not { Length: 3 }) {
            throw new FormatException("Expecting 3 local memory bank settings");
        }

        uint mbk6 = 0;
        mbk6 |= (uint)(settings[0].StartAddressSlot << 4);
        mbk6 |= (uint)(settings[0].ImageSize << 12);
        mbk6 |= (uint)(settings[0].EndAddressSlot << 20);
        writer.Write(mbk6);

        for (int i = 0; i < 2; i++) {
            var setting = settings[i + 1];
            if (setting.StartAddressSlot > 511 || setting.ImageSize > 3 || setting.EndAddressSlot > 1023) {
                throw new ArgumentException($"Invalid local memory bank setting for {i}");
            }

            uint data = (uint)(setting.StartAddressSlot << 3);
            data |= (uint)(setting.ImageSize << 12);
            data |= (uint)(setting.EndAddressSlot << 19);
            writer.Write(data);
        }
    }
}
