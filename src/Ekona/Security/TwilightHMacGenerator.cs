// Copyright(c) 2022 SceneGate
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
using System.IO;
using System.Security.Cryptography;
using SceneGate.Ekona.Containers.Rom;
using SceneGate.Ekona.Security;
using Yarhl.IO;

namespace SceneGate.Ekona.Security;

/// <summary>
/// Generate different hashes, HMAC and signatures of DS and DSi ROMs.
/// </summary>
public class TwilightHMacGenerator
{
    private const int PaddingSize = 512;
    private readonly DsiKeyStore keyStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="TwilightHMacGenerator"/> class.
    /// </summary>
    /// <param name="keyStore">Store with the keys.</param>
    public TwilightHMacGenerator(DsiKeyStore keyStore)
    {
        this.keyStore = keyStore;
    }

    /// <summary>
    /// Generate the whitelist phase 2 HMAC (overlay9).
    /// </summary>
    /// <param name="romStream">ROM stream with the content to generate the hash.</param>
    /// <param name="sectionInfo">Information of different ROM sections.</param>
    /// <returns>HMAC for the whitelist phase 2.</returns>
    /// <exception cref="InvalidOperationException">The key for the whitelist phase 2 is missing.</exception>
    public byte[] GeneratePhase2Hmac(Stream romStream, RomSectionInfo sectionInfo)
    {
        byte[] phase2Key = keyStore.HMacKeyWhitelist12;
        if (phase2Key is not { Length: > 0 }) {
            throw new InvalidOperationException($"Missing {nameof(DsiKeyStore.HMacKeyWhitelist12)} key");
        }

        using HMAC generator = CreateGenerator(phase2Key);
        var reader = new DataReader(romStream);

        // First hash the overlay 9 table info.
        reader.Stream.Position = sectionInfo.Overlay9TableOffset;
        byte[] infoData = reader.ReadBytes(sectionInfo.Overlay9TableSize);
        _ = generator.TransformBlock(infoData, 0, infoData.Length, null, 0);

        // Then hash the FAT entries for overlay 9 files.
        int numOverlays = sectionInfo.Overlay9TableSize / 0x20;
        reader.Stream.Position = sectionInfo.FatOffset;
        byte[] overlaysFat = reader.ReadBytes(numOverlays * 8);
        _ = generator.TransformBlock(overlaysFat, 0, overlaysFat.Length, null, 0);

        // Finally hash each overlay in an fun, unncessary and complex way.
        // Hash each overlay including its hash, without exceeding a maximum size per file.
        // The maximum file size to hash is changing depending how many bytes are hashed already.
        int blocksRead = 0;
        for (int i = 0; i < numOverlays; i++) {
            reader.Stream.Position = sectionInfo.FatOffset + (i * 8);
            uint overlayOffset = reader.ReadUInt32();
            int overlaySize = (int)(reader.ReadUInt32() - overlayOffset).Pad(PaddingSize);

            int remainingOverlays = numOverlays - i;
            int maxSize = ((1 << 0xA) - blocksRead) / remainingOverlays * PaddingSize;
            int hashSize = (overlaySize > maxSize) ? maxSize : overlaySize;

            reader.Stream.Position = overlayOffset;
            byte[] overlayData = reader.ReadBytes(hashSize);

            _ = generator.TransformBlock(overlayData, 0, overlayData.Length, null, 0);
            blocksRead += hashSize / PaddingSize;
        }

        // Dummy but necessary call to get the hash...
        _ = generator.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

        return generator.Hash;
    }

    /// <summary>
    /// Generate the whitelist phase 3 HMAC (banner with titles and icons).
    /// </summary>
    /// <param name="romStream">ROM stream with the content to generate the hash.</param>
    /// <param name="programInfo">Program information to determine if the target is DSi or DS.</param>
    /// <param name="sectionInfo">Information of different ROM sections.</param>
    /// <returns>HMAC for the whitelist phase 3.</returns>
    /// <exception cref="InvalidOperationException">The key for the whitelist phase 3 is missing.</exception>
    public byte[] GeneratePhase3Hmac(Stream romStream, ProgramInfo programInfo, RomSectionInfo sectionInfo)
    {
        bool isDsi = programInfo.UnitCode != DeviceUnitKind.DS;
        byte[] bannerKey = isDsi ? keyStore.HMacKeyDSiGames : keyStore.HMacKeyWhitelist34;

        romStream.Position = sectionInfo.BannerOffset;
        int bannerSize = Binary2Banner.GetSize(romStream);
        return Generate(bannerKey, romStream, sectionInfo.BannerOffset, bannerSize);
    }

    private static HMAC CreateGenerator(byte[] key)
    {
        var hmac = HMAC.Create("HMACSHA1");
        hmac.Key = key;
        return hmac;
    }

    private static byte[] Generate(byte[] key, Stream stream, long offset, long length)
    {
        using var segment = new DataStream(stream, offset, length);
        using HMAC algo = CreateGenerator(key);
        return algo.ComputeHash(segment);
    }
}
