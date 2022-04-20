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
using System.Linq;
using System.Security.Cryptography;
using SceneGate.Ekona.Containers.Rom;
using Yarhl.FileSystem;
using Yarhl.IO;

namespace SceneGate.Ekona.Security;

/// <summary>
/// Generate different hashes, HMAC and signatures of DS and DSi ROMs.
/// </summary>
public class TwilightHMacGenerator
{
    private const int SecureAreaLength = 16 * 1024;
    private const int PaddingSize = 512;
    private readonly DsiKeyStore keyStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="TwilightHMacGenerator"/> class.
    /// </summary>
    /// <param name="keyStore">Store with the keys.</param>
    public TwilightHMacGenerator(DsiKeyStore keyStore)
    {
        this.keyStore = keyStore ?? throw new ArgumentNullException(nameof(keyStore));
    }

    /// <summary>
    /// Generate the DS whitelist phase 1 HMAC (header, encrypted ARM9 and ARM7).
    /// </summary>
    /// <param name="romStream">ROM stream with the content to generate the hash.</param>
    /// <param name="encryptedArm9">Encrypted ARM9 to use for the hash.</param>
    /// <param name="sectionInfo">Information of different ROM sections.</param>
    /// <returns>HMAC for the whitelist phase 1.</returns>
    /// <exception cref="InvalidOperationException">The key for the whitelist phase 1 is missing.</exception>
    public byte[] GeneratePhase1Hmac(Stream romStream, Stream encryptedArm9, RomSectionInfo sectionInfo)
    {
        byte[] phase1Key = keyStore.HMacKeyWhitelist12;
        if (phase1Key is not { Length: > 0 }) {
            throw new InvalidOperationException($"Missing {nameof(DsiKeyStore.HMacKeyWhitelist12)} key");
        }

        using HMAC generator = CreateGenerator(phase1Key);
        var reader = new DataReader(romStream);

        // First hash the DS header.
        romStream.Position = 0;
        byte[] headerData = reader.ReadBytes(0x160);
        _ = generator.TransformBlock(headerData, 0, headerData.Length, null, 0);

        // Then hash the encrypted ARM9.
        encryptedArm9.Position = 0;
        byte[] arm9Data = new byte[encryptedArm9.Length];
        encryptedArm9.Read(arm9Data);
        _ = generator.TransformBlock(arm9Data, 0, arm9Data.Length, null, 0);

        // Finally hash the ARM7
        romStream.Position = sectionInfo.Arm7Offset;
        byte[] arm7Data = reader.ReadBytes((int)sectionInfo.Arm7Size);
        _ = generator.TransformFinalBlock(arm7Data, 0, arm7Data.Length);

        return generator.Hash;
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

        // Finally hash each overlay in an fun, unnecessary and complex way.
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

    /// <summary>
    /// Generate the HMAC of the stream.
    /// </summary>
    /// <param name="stream">Stream to generate MAC.</param>
    /// <returns>HMAC of the stream.</returns>
    public byte[] GenerateHmac(Stream stream) => Generate(keyStore.HMacKeyDSiGames, stream);

    /// <summary>
    /// Generate the HMAC of the DSi program digest block.
    /// </summary>
    /// <param name="romStream">ROM stream with the content to generate the hash.</param>
    /// <param name="sectionInfo">Information of different ROM sections.</param>
    /// <returns>HMAC of the digest block.</returns>
    public byte[] GenerateDigestBlockHmac(Stream romStream, RomSectionInfo sectionInfo) =>
        Generate(
            keyStore.HMacKeyDSiGames,
            romStream,
            sectionInfo.DigestBlockHashtableOffset,
            sectionInfo.DigestBlockHashtableLength);

    /// <summary>
    /// Generate the HMAC of the ARM9 without secure area.
    /// </summary>
    /// <param name="romStream">ROM stream with the content to generate the hash.</param>
    /// <param name="sectionInfo">Information of different ROM sections.</param>
    /// <returns>HMAC of the ARM9 with no secure area.</returns>
    public byte[] GenerateArm9NoSecureAreaHmac(Stream romStream, RomSectionInfo sectionInfo) =>
        Generate(
            keyStore.HMacKeyDSiGames,
            romStream,
            sectionInfo.Arm9Offset + SecureAreaLength,
            sectionInfo.Arm9Size - SecureAreaLength);

    /// <summary>
    /// Verify if the digest block hashes (to the sector hashes) are valid.
    /// </summary>
    /// <param name="romStream">ROM stream with the content to validate.</param>
    /// <param name="sectionInfo">Information of different ROM sections.</param>
    /// <returns>The status if the digest block area is valid or not.</returns>
    /// <exception cref="EndOfStreamException">The block area is incomplete.</exception>
    public HashStatus VerifyDigestBlock(Stream romStream, RomSectionInfo sectionInfo)
    {
        const int HashLength = 0x14;
        byte[] expectedHash = new byte[HashLength];
        using HMAC generator = CreateGenerator(keyStore.HMacKeyDSiGames);

        uint blockLength = sectionInfo.DigestBlockSectorCount * HashLength;
        byte[] buffer = new byte[blockLength];

        bool result = true;
        int numBlockHashes = (int)sectionInfo.DigestBlockHashtableLength / HashLength;
        for (int i = 0; i < numBlockHashes && result; i++) {
            // Get next hash from digest block area
            romStream.Position = sectionInfo.DigestBlockHashtableOffset + (i * HashLength);
            int read = romStream.Read(expectedHash);
            if (read != HashLength) {
                throw new EndOfStreamException("End of stream reading existing hash");
            }

            // Generate next HMAC hash from digest section area
            romStream.Position = sectionInfo.DigestSectorHashtableOffset + (i * blockLength);
            romStream.Read(buffer);
            byte[] actualHash = generator.ComputeHash(buffer);

            result = expectedHash.SequenceEqual(actualHash);
        }

        return result ? HashStatus.Valid : HashStatus.Invalid;
    }

    /// <summary>
    /// Verify if the digest section content hashes (to the ROM data) are valid.
    /// </summary>
    /// <param name="romStream">ROM stream with the content to validate.</param>
    /// <param name="encryptedArm9">ARM9 stream with encrypted secure area.</param>
    /// <param name="systemNode">Container node with modcrypt decrypted system programs.</param>
    /// <param name="sectionInfo">Information of different ROM sections.</param>
    /// <returns>The status if the digest section is valid or not.</returns>
    /// <exception cref="EndOfStreamException">The section area is incomplete.</exception>
    public HashStatus VerifyDigestSectionContent(Stream romStream, Stream encryptedArm9, Node systemNode, RomSectionInfo sectionInfo)
    {
        bool PositionBetween(long position, uint offset, uint length) =>
            position >= offset && position < offset + length;

        void ReadWithPadding(Stream stream, long position, byte[] buffer)
        {
            stream.Position = position;
            int read = stream.Read(buffer);
            if (read != buffer.Length) {
                Array.Fill<byte>(buffer, 0xFF, read, buffer.Length - read);
            }
        }

        const int HashLength = 0x14;
        byte[] expectedHash = new byte[HashLength];
        using HMAC sha1 = CreateGenerator(keyStore.HMacKeyDSiGames);

        uint nitroBlockIdx = 0;
        uint twilightBlockIdx = 0;
        uint nitroMaxBlocks = sectionInfo.DigestNitroLength / sectionInfo.DigestSectorSize;
        uint twilightMaxBlocks = sectionInfo.DigestTwilightLength / sectionInfo.DigestSectorSize;

        byte[] buffer = new byte[sectionInfo.DigestSectorSize];

        bool result = true;
        int numBlockHashes = (int)sectionInfo.DigestSectorHashtableLength / HashLength;
        for (int i = 0; i < numBlockHashes && result; i++) {
            // Get next hash from digest sector area
            romStream.Position = sectionInfo.DigestSectorHashtableOffset + (i * HashLength);
            int read = romStream.Read(expectedHash);
            if (read != HashLength) {
                throw new EndOfStreamException("End of stream reading existing hash");
            }

            // Generate next hash from the ROM content
            long hashOffset;
            if (nitroBlockIdx < nitroMaxBlocks) {
                hashOffset = sectionInfo.DigestNitroOffset + (nitroBlockIdx * sectionInfo.DigestSectorSize);
                nitroBlockIdx++;
            } else if (twilightBlockIdx < twilightMaxBlocks) {
                hashOffset = sectionInfo.DigestTwilightOffset + (twilightBlockIdx * sectionInfo.DigestSectorSize);
                twilightBlockIdx++;
            } else if (expectedHash.All(x => x == 0)) {
                // Because the length includes padding, we don't know if we reach to the end of hashes.
                break;
            } else {
                throw new EndOfStreamException("Missing ROM content for hashes");
            }

            // We use the nodes from the system folder as the file may have modcrypt and we need them decrypted.
            if (PositionBetween(hashOffset, sectionInfo.Arm9Offset, sectionInfo.Arm9Size)) {
                // Digest hash encrypted secure area, so we use the argument stream
                ReadWithPadding(encryptedArm9, hashOffset - sectionInfo.Arm9Offset, buffer);
            } else if (PositionBetween(hashOffset, sectionInfo.Arm7Offset, sectionInfo.Arm7Size)) {
                ReadWithPadding(systemNode.Children["arm7"].Stream, hashOffset - sectionInfo.Arm7Offset, buffer);
            } else if (PositionBetween(hashOffset, sectionInfo.Arm9iOffset, sectionInfo.Arm9iSize)) {
                ReadWithPadding(systemNode.Children["arm9i"].Stream, hashOffset - sectionInfo.Arm9iOffset, buffer);
            } else if (PositionBetween(hashOffset, sectionInfo.Arm7iOffset, sectionInfo.Arm7iSize)) {
                ReadWithPadding(systemNode.Children["arm7i"].Stream, hashOffset - sectionInfo.Arm7iOffset, buffer);
            } else {
                romStream.Position = hashOffset;
                romStream.Read(buffer);
            }

            byte[] actualHash = sha1.ComputeHash(buffer);
            result = expectedHash.SequenceEqual(actualHash);
        }

        return result ? HashStatus.Valid : HashStatus.Invalid;
    }

    private static HMAC CreateGenerator(byte[] key)
    {
        var hmac = HMAC.Create("HMACSHA1");
        hmac.Key = key;
        return hmac;
    }

    private static byte[] Generate(byte[] key, Stream stream) => Generate(key, stream, 0, stream.Length);

    private static byte[] Generate(byte[] key, Stream stream, long offset, long length)
    {
        using var segment = new DataStream(stream, offset, length);
        using HMAC algo = CreateGenerator(key);
        return algo.ComputeHash(segment);
    }
}
