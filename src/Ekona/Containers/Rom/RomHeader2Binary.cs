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
using Yarhl.FileFormat;
using Yarhl.IO;

namespace SceneGate.Ekona.Containers.Rom;

/// <summary>
/// Converter for ROM header object into binary stream (serialization).
/// </summary>
public class RomHeader2Binary : IConverter<RomHeader, BinaryFormat>
{
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

        writer.Write(source.ProgramInfo.GameTitle, 12, nullTerminator: false);
        writer.Write(source.ProgramInfo.GameCode, 4, nullTerminator: false);
        writer.Write(source.ProgramInfo.MakerCode, 2, nullTerminator: false);
        writer.Write((byte)source.ProgramInfo.UnitCode);
        writer.Write(source.ProgramInfo.EncryptionSeed);
        double relativeSize = (double)source.ProgramInfo.CartridgeSize / RomInfo.MinimumCartridgeSize;
        byte power2Size = (byte)Math.Ceiling(Math.Log2(relativeSize));
        writer.Write(power2Size);

        writer.WriteTimes(0, 7); // reserved
        writer.Write((byte)source.ProgramInfo.DsiFlags);
        writer.Write(source.ProgramInfo.Region);
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
        writer.Write(source.ProgramInfo.ChecksumSecureArea.Expected); // not calculated as we can't have secure area
        writer.Write(source.ProgramInfo.SecureAreaDelay);
        writer.Write(source.ProgramInfo.Arm9Autoload);
        writer.Write(source.ProgramInfo.Arm7Autoload);
        writer.Write(source.ProgramInfo.SecureDisable);
        writer.Write(source.SectionInfo.RomSize);
        writer.Write(source.SectionInfo.HeaderSize);
        writer.Write(source.ProgramInfo.Arm9ParametersTableOffset);
        writer.Write(source.ProgramInfo.Arm7ParametersTableOffset);
        writer.Write(source.ProgramInfo.NitroRegionEnd);
        writer.Write(source.ProgramInfo.TwilightRegionStart);

        writer.WriteTimes(0, 0x2C);
        writer.Write(source.CopyrightLogo);
        writer.WriteComputedCrc16(0xC0, 0x9C);
        writer.WriteComputedCrc16(0x00, 0x15E);

        writer.Write(source.ProgramInfo.DebugRomOffset);
        writer.Write(source.ProgramInfo.DebugSize);
        writer.Write(source.ProgramInfo.DebugRamAddress);

        writer.WriteUntilLength(0, 0x1BF);
        writer.Write((byte)source.ProgramInfo.DsiRomFeatures);

        // Write the HMAC if they exist. NitroRom2Binary regenerates them.
        if (source.ProgramInfo.BannerMac is not null) {
            writer.WriteUntilLength(0, 0x33C);
            writer.Write(source.ProgramInfo.BannerMac.Hash);
        }

        if (source.ProgramInfo.HeaderMac is not null) {
            writer.WriteUntilLength(0, 0x378);
            writer.Write(source.ProgramInfo.HeaderMac.Hash);
        }

        if (source.ProgramInfo.FatMac is not null) {
            writer.WriteUntilLength(0, 0x38C);
            writer.Write(source.ProgramInfo.FatMac.Hash);
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

        return binary;
    }
}
