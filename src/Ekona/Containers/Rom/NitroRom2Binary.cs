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
#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using SceneGate.Ekona.Security;
using Yarhl.FileFormat;
using Yarhl.FileSystem;
using Yarhl.IO;

namespace SceneGate.Ekona.Containers.Rom;

/// <summary>
/// Converter from NitroRom containers into binary format.
/// </summary>
public class NitroRom2Binary :
    IInitializer<NitroRom2BinaryParams>,
    IConverter<NodeContainerFormat, BinaryFormat>
{
    private const int PaddingSize = 0x200;
    private const int TwilightPaddingSize = 0x400;
    private const int SecureAreaLength = 16 * 1024;
    private readonly SortedList<int, NodeInfo> nodesById = new SortedList<int, NodeInfo>();
    private readonly SortedList<int, NodeInfo> nodesByOffset = new SortedList<int, NodeInfo>();
    private Stream? initializedOutputStream;
    private DsiKeyStore? keyStore;
    private bool decompressedProgram;
    private DataWriter writer = null!;
    private Node root = null!;
    private ProgramInfo programInfo = null!;
    private RomSectionInfo sectionInfo = null!;

    /// <summary>
    /// Initializes the converter with the output stream for the next conversion.
    /// </summary>
    /// <param name="parameters">The output stream for the next conversion.</param>
    /// <exception cref="ArgumentNullException">The argument is null.</exception>
    public void Initialize(NitroRom2BinaryParams parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        initializedOutputStream = parameters.OutputStream;
        keyStore = parameters.KeyStore;
        decompressedProgram = parameters.DecompressedProgram;
    }

    /// <summary>
    /// Serializes a ROM container into NDS binary format.
    /// </summary>
    /// <param name="source">The ROM container to serialize.</param>
    /// <returns>Serialized binary stream.</returns>
    /// <exception cref="ArgumentNullException">The argument is null.</exception>
    public BinaryFormat Convert(NodeContainerFormat source)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        root = source.Root;

        // Binary order is:
        // - Header
        // - Unknown
        // - ARM9: program, table, overlays
        // - ARM7: program, table, overlays
        // - FNT: main tables, entry tables
        // - FAT: including overlays
        // - Banner
        // - Files
        // - (DSi) Digest sector
        // - (DSi) Digest block
        // - Padding to round up to 16 MB (XX000000).
        // ---- Twilight ROM content ----
        // - Unknown, probably padding but due to cartridge protocol 3x 0x8000..0x9000
        // - ARM9i
        // - ARM7i
        var binary = new BinaryFormat(initializedOutputStream ?? new DataStream());
        binary.Stream.Position = 0;
        writer = new DataWriter(binary.Stream);

        programInfo = GetChildFormatSafe<ProgramInfo>("system/info");
        sectionInfo = new RomSectionInfo {
            HeaderSize = 0x4000,
        };

        // Write the header at the end, when we got all the new offsets and sizes
        writer.WriteTimes(0, sectionInfo.HeaderSize);

        // Sort nodes by ID
        PopulateNodeInfo();

        WritePrograms();

        WriteFileSystem();

        // Prefill FAT and Banner so we can write the FAT at the same time as the files
        int numFiles = nodesById.Count(e => !e.Value.Node.IsContainer);
        sectionInfo.FatOffset = (uint)writer.Stream.Position;
        sectionInfo.FatSize = (uint)(numFiles * 8);
        writer.WriteTimes(0, numFiles * 8); // FAT
        writer.WritePadding(0xFF, PaddingSize);

        Banner banner = GetChildFormatSafe<Banner>("system/banner/info");
        sectionInfo.BannerOffset = (uint)writer.Stream.Position;
        writer.WriteTimes(0, Binary2Banner.GetSize(banner.Version));
        writer.WritePadding(0xFF, PaddingSize);

        WriteFatAndFiles();

        WriteBanner();

        if (programInfo.UnitCode != DeviceUnitKind.DS) {
            // TODO: Generate actual digest #12
            WriteEmptyDigest();
        }

        sectionInfo.RomSize = (uint)writer.Stream.Length;

        if (programInfo.UnitCode != DeviceUnitKind.DS) {
            // Padding to start twilight section
            writer.WritePadding(0xFF, 0x100000);

            WriteTwilightPrograms();
            sectionInfo.DigestTwilightOffset = sectionInfo.Arm9iOffset;

            sectionInfo.DsiRomLength = (uint)writer.Stream.Length;

            // TODO: Encrypt arm9/arm7/arm9i/arm7i #11
            var modcrypt1 = FakeModcryptEncrypt(programInfo.DsiInfo.ModcryptArea1Target);
            var modcrypt2 = FakeModcryptEncrypt(programInfo.DsiInfo.ModcryptArea2Target);
            (sectionInfo.ModcryptArea1Offset, sectionInfo.ModcryptArea1Length) = modcrypt1;
            (sectionInfo.ModcryptArea2Offset, sectionInfo.ModcryptArea2Length) = modcrypt2;
        }

        WriteHeader();

        // Fill size to the cartridge size
        writer.WriteUntilLength(0xFF, programInfo.CartridgeSize);

        return binary;
    }

    private Node GetChildSafe(string path) =>
        Navigator.SearchNode(root, path) ?? throw new FileNotFoundException("Child not found", path);

    private T GetChildFormatSafe<T>(string path)
        where T : class, IFormat
    {
        Node child = GetChildSafe(path);
        return child.GetFormatAs<T>() ?? throw new FormatException(
           $"Invalid child format '{path}'. " +
           $"Expected {typeof(T).FullName}, got: {child.Format?.GetType().FullName}");
    }

    private void WriteHeader()
    {
        var header = new RomHeader {
            ProgramInfo = programInfo,
            SectionInfo = sectionInfo,
        };

        // Set the new copyright logo if it exists
        Node? logoNode = root.Children["system"]?.Children["copyright_logo"];
        if (logoNode is not null) {
            DataStream binaryLogo = logoNode.Stream ?? throw new FormatException("Invalid format for copyright_logo");
            header.CopyrightLogo = new DataReader(binaryLogo).ReadBytes((int)binaryLogo.Length);
        } else {
            header.CopyrightLogo = new byte[156];
        }

        GenerateSignatures();

        // We don't calculate the header length but we expect it's preset.
        // It's used inside the converter to pad.
        var binaryHeader = (IBinary)ConvertFormat.With<RomHeader2Binary>(header);
        writer.Stream.Position = 0;
        binaryHeader.Stream.WriteTo(writer.Stream);
    }

    private void GenerateSignatures()
    {
        if (keyStore is null) {
            return;
        }

        bool isDsi = programInfo.UnitCode != DeviceUnitKind.DS;
        var key1Encryption = new NitroKey1Encryption(programInfo.GameCode, keyStore);
        var hashGenerator = new TwilightHMacGenerator(keyStore);
        var crcGenerator = new NitroCrcGenerator();

        // Get ARM9 encrypted (or encrypt it) since it's used for several CRC / hashes
        DataStream arm9 = GetChildFormatSafe<IBinary>("system/arm9").Stream;
        using DataStream encryptedArm9 = key1Encryption.HasEncryptedArm9(arm9)
            ? new DataStream(arm9)
            : key1Encryption.EncryptArm9(arm9);

        // Header secure area CRC
        byte[] secureAreaCrc = crcGenerator.GenerateCrc16(encryptedArm9, 0, SecureAreaLength);
        programInfo.ChecksumSecureArea.ChangeHash(secureAreaCrc);

        // HMAC for phase 1 and 2 of DS games
        bool generatePhase12 = programInfo.ProgramFeatures.HasFlag(DsiRomFeatures.NitroProgramSigned);
        if (generatePhase12 && keyStore.HMacKeyWhitelist12 is { Length: > 0 }) {
            byte[] phase1Hash = hashGenerator.GeneratePhase1Hmac(writer.Stream, encryptedArm9, sectionInfo);
            programInfo.NitroProgramMac.ChangeHash(phase1Hash);

            byte[] phase2Hash = hashGenerator.GeneratePhase2Hmac(writer.Stream, sectionInfo);
            programInfo.NitroOverlaysMac.ChangeHash(phase2Hash);
        }

        // HMAC for banner of DS (phase 3) and DSi games
        byte[] bannerKey = isDsi ? keyStore.HMacKeyDSiGames : keyStore.HMacKeyWhitelist34;
        bool generateBannerHmac = isDsi || programInfo.ProgramFeatures.HasFlag(DsiRomFeatures.NitroBannerSigned);
        if (generateBannerHmac && bannerKey is { Length: > 0 }) {
            byte[] bannerHash = hashGenerator.GeneratePhase3Hmac(writer.Stream, programInfo, sectionInfo);
            programInfo.BannerMac.ChangeHash(bannerHash);
        }

        if (isDsi && keyStore.HMacKeyDSiGames is { Length: > 0 }) {
            byte[] arm9EncryptedHash = hashGenerator.GenerateEncryptedArm9Hmac(encryptedArm9);
            programInfo.DsiInfo.Arm9SecureMac.ChangeHash(arm9EncryptedHash);

            byte[] arm7Hash = hashGenerator.GenerateArm7Hmac(writer.Stream, sectionInfo);
            programInfo.DsiInfo.Arm7Mac.ChangeHash(arm7Hash);

            byte[] digestHash = hashGenerator.GenerateDigestBlockHmac(writer.Stream, sectionInfo);
            programInfo.DsiInfo.DigestMain.ChangeHash(digestHash);

            // TODO: After modcrypt implementation HMAC for ARM9i and ARM7i #11
            byte[] arm9Hash = hashGenerator.GenerateArm9NoSecureAreaHmac(writer.Stream, sectionInfo);
            programInfo.DsiInfo.Arm9Mac.ChangeHash(arm9Hash);
        }

        // While we could add the code to sign the data with a provided private key...
        // In practice it won't ever happen that we know the actual private key.
        // We could modify the NAND with our own public/private key but at that point
        // just replacing the verification sign code is easier with launch, so I won't add that dead code.
    }

    private void WriteBanner()
    {
        if (root.Children["system"]?.Children["banner"] is null) {
            return;
        }

        writer.Stream.Position = sectionInfo.BannerOffset;
        GetChildSafe("system/banner")
            .TransformWith<Banner2Binary>()
            .Stream!.WriteTo(writer.Stream);

        writer.Stream.Position = sectionInfo.BannerOffset;
        sectionInfo.BannerLength = (uint)Binary2Banner.GetSize(writer.Stream);
    }

    private void WritePrograms()
    {
        WriteProgram(true);
        WriteProgramCodeParameters();
        WriteOverlays(true);

        WriteProgram(false);
        WriteOverlays(false);
    }

    private void WriteTwilightPrograms()
    {
        // First 0x3000 bytes
        // Unknown but let's replicate what the dumper gets
        byte[] unknownTwilightData = new byte[0x1000];
        writer.Stream.Position = 0x8000;
        writer.Stream.Read(unknownTwilightData);

        writer.Stream.Seek(0, SeekOrigin.End);
        writer.Write(unknownTwilightData);
        writer.Write(unknownTwilightData);
        writer.Write(unknownTwilightData);

        // ARM9i
        IBinary arm9i = GetChildFormatSafe<IBinary>("system/arm9i");
        sectionInfo.Arm9iSize = (uint)arm9i.Stream.Length;
        sectionInfo.Arm9iOffset = (uint)writer.Stream.Length;
        arm9i.Stream.WriteTo(writer.Stream);
        writer.WritePadding(0xFF, TwilightPaddingSize);

        // ARM7i
        IBinary arm7i = GetChildFormatSafe<IBinary>("system/arm7i");
        sectionInfo.Arm7iSize = (uint)arm7i.Stream.Length;
        sectionInfo.Arm7iOffset = (uint)writer.Stream.Length;
        arm7i.Stream.WriteTo(writer.Stream);
        writer.WritePadding(0xFF, TwilightPaddingSize);
    }

    private void WriteProgram(bool isArm9)
    {
        string armPath = isArm9 ? "system/arm9" : "system/arm7";

        IBinary binaryArm = GetChildFormatSafe<IBinary>(armPath);
        uint armLength = (uint)binaryArm.Stream.Length;
        uint armOffset = armLength > 0 ? (uint)writer.Stream.Position : 0;
        binaryArm.Stream.WriteTo(writer.Stream);
        writer.WritePadding(0xFF, PaddingSize);

        if (isArm9) {
            sectionInfo.Arm9Offset = armOffset;
            sectionInfo.Arm9Size = armLength;
            if (programInfo.ProgramCodeParameters is not null) {
                programInfo.ProgramCodeParameters.CompressedLength = decompressedProgram ? 0 : armLength;
            }
        } else {
            sectionInfo.Arm7Offset = armOffset;
            sectionInfo.Arm7Size = armLength;
        }
    }

    private void WriteProgramCodeParameters()
    {
        NitroProgramCodeParameters? programParams = programInfo.ProgramCodeParameters;
        if (programParams is null) {
            return;
        }

        writer.Stream.PushCurrentPosition();

        // DS games don't have this header value with ARM9 param table offset.
        // Instead, the ARM9 has a "tail" with three uint: the nitrocode, the offset and an offset to hashes.
        uint paramsOffset = programInfo.Arm9ParametersTableOffset;
        if (programParams.ProgramParameterOffset != 0) {
            writer.Stream.Position = sectionInfo.Arm9Offset + sectionInfo.Arm9Size;
            writer.Write(NitroRom.NitroCode);
            writer.Write(programParams.ProgramParameterOffset);
            writer.Write(programParams.ExtraHashesOffset);
            paramsOffset = programParams.ProgramParameterOffset;
        }

        // This ARM9 code parameters are inside the ARM9.
        // Usually, we can find the nitrocode (or their DSi variants) before and after.
        // DSi games has more (unknown) parameters.
        writer.Stream.Position = sectionInfo.Arm9Offset + paramsOffset;
        writer.Write(programParams.ItcmBlockInfoOffset);
        writer.Write(programParams.ItcmBlockInfoEndOffset);
        writer.Write(programParams.ItcmInputDataOffset);
        writer.Write(programParams.BssOffset);
        writer.Write(programParams.BssEndOffset);

        if (programParams.CompressedLength > 0) {
            writer.Write(programParams.CompressedLength + programInfo.Arm9RamAddress);
        } else {
            writer.Write(0);
        }

        writer.Write((ushort)programParams.SdkVersion.Build);
        writer.Write((byte)programParams.SdkVersion.Minor);
        writer.Write((byte)programParams.SdkVersion.Major);

        writer.Stream.PopPosition();
    }

    private void WriteOverlays(bool isArm9)
    {
        string overlayPath = isArm9 ? "system/overlays9" : "system/overlays7";
        Node overlays = GetChildSafe(overlayPath);

        int tableSize = overlays.Children.Count * 0x20;
        uint tableOffset = tableSize > 0 ? (uint)writer.Stream.Position : 0;
        Collection<OverlayInfo> overlaysInfo;
        if (isArm9) {
            sectionInfo.Overlay9TableOffset = tableOffset;
            sectionInfo.Overlay9TableSize = tableSize;
            overlaysInfo = programInfo.Overlays9Info;
        } else {
            sectionInfo.Overlay7TableOffset = tableOffset;
            sectionInfo.Overlay7TableSize = tableSize;
            overlaysInfo = programInfo.Overlays7Info;
        }

        if (overlaysInfo.Count != overlays.Children.Count) {
            throw new InvalidOperationException("Number of overlay info doesn't match number of files");
        }

        // Prefill table so we can write overlays at the same time
        writer.Stream.PushCurrentPosition();
        writer.WriteTimes(0, tableSize);
        writer.WritePadding(0xFF, PaddingSize);
        writer.Stream.PopPosition();

        for (int i = 0; i < overlays.Children.Count; i++) {
            OverlayInfo info = overlaysInfo[i];
            DataStream overlay = overlays.Children[i].Stream ?? throw new FormatException($"Overlay {i} is not binary");
            info.CompressedSize = (uint)overlay.Length;

            writer.Write(info.OverlayId);
            writer.Write(info.RamAddress);
            writer.Write(info.RamSize);
            writer.Write(info.BssSize);
            writer.Write(info.StaticInitStart);
            writer.Write(info.StaticInitEnd);
            writer.Write(info.OverlayId);
            uint encodingInfo = info.IsCompressed ? info.CompressedSize : 0;
            encodingInfo |= (uint)((info.IsCompressed ? 1 : 0) << 24);
            encodingInfo |= (uint)((info.IsSigned ? 1 : 0) << 25);
            writer.Write(encodingInfo);

            writer.Stream.PushToPosition(0, SeekOrigin.End);
            uint overlayOffset = (uint)writer.Stream.Position;
            var nodeInfo = new NodeInfo {
                IsOverlay = true,
                Node = overlays.Children[i],
                Offset = overlayOffset,
                Id = (int)info.OverlayId,
                PhysicalId = (int)info.OverlayId,
            };
            nodesById.Add(nodeInfo.Id, nodeInfo);
            nodesByOffset.Add(nodeInfo.PhysicalId, nodeInfo);

            overlay.WriteTo(writer.Stream);
            writer.WritePadding(0xFF, PaddingSize);
            writer.Stream.PopPosition();
        }

        writer.Stream.Seek(0, SeekOrigin.End);
    }

    private void WriteFileSystem()
    {
        var encoding = Encoding.GetEncoding("shift_jis");

        uint fntOffset = (uint)writer.Stream.Length;
        sectionInfo.FntOffset = (uint)writer.Stream.Position;

        // Prefill directory tables, so we can write entry tables at the same time
        int numDirectories = nodesById.Count(e => e.Value.Node.IsContainer);
        writer.WriteTimes(0, 8 * numDirectories);

        foreach ((int id, NodeInfo info) in nodesById) {
            if (!info.Node.IsContainer) {
                continue;
            }

            long entryTableOffset = writer.Stream.Length - fntOffset;
            long dirTableOffset = fntOffset + ((id & 0x0FFF) * 8);
            writer.Stream.Position = dirTableOffset;

            int firstFileId = Navigator.IterateNodes(info.Node, NavigationMode.DepthFirst)
                .FirstOrDefault(n => !n.IsContainer)
                ?.Tags["scenegate.ekona.id"] ?? 0;
            int parentId = id == 0xF000 ? numDirectories : info.Node.Parent?.Tags["scenegate.ekona.id"];

            writer.Write((uint)entryTableOffset);
            writer.Write((ushort)firstFileId);
            writer.Write((ushort)parentId);

            writer.Stream.Position = fntOffset + entryTableOffset;
            foreach (Node child in info.Node.Children) {
                int nodeType = child.IsContainer ? 0x80 : 0x00;
                nodeType |= encoding.GetByteCount(child.Name);
                writer.Write((byte)nodeType);

                writer.Write(child.Name, nullTerminator: false, encoding);

                if (child.IsContainer) {
                    writer.Write((ushort)child.Tags["scenegate.ekona.id"]);
                }
            }

            writer.Write((byte)0x00);
        }

        sectionInfo.FntSize = (uint)(writer.Stream.Length - sectionInfo.FntOffset);
        writer.Stream.Seek(0, SeekOrigin.End);
        writer.WritePadding(0xFF, PaddingSize);
    }

    private void WriteFatAndFiles()
    {
        uint offset = (uint)writer.Stream.Length;

        foreach ((int id, NodeInfo info) in nodesByOffset) {
            if (info.Node.IsContainer) {
                continue;
            }

            uint endOffset;
            if (info.IsOverlay) {
                // Overlays are written after overlay table, not in the file data section.
                offset = info.Offset;
                endOffset = (uint)(info.Offset + info.Node.Stream!.Length);
            } else {
                writer.Stream.Position = offset;
                info.Node.Stream!.WriteTo(writer.Stream);
                endOffset = (uint)writer.Stream.Position;

                // Do not pad last file because ROM size doesn't include it
                if (id != nodesByOffset.Count - 1) {
                    writer.WritePadding(0xFF, PaddingSize);
                }
            }

            writer.Stream.Position = sectionInfo.FatOffset + (info.Id * 8);
            writer.Write(offset);
            writer.Write(endOffset);

            offset = (uint)writer.Stream.Length;
        }
    }

    private void PopulateNodeInfo()
    {
        nodesById.Clear();
        nodesByOffset.Clear();

        int fileIndex = 0;
        int dirIndex = 0;

        // Overlays are written like regular files but in a special location.
        // Skip the index here, added later when writing the file that we know the ID.
        string systemPath = $"/{root.Name}/system";
        fileIndex += GetChildSafe("system/overlays9").Children.Count;
        fileIndex += GetChildSafe("system/overlays7").Children.Count;
        int physicalIndex = fileIndex;

        foreach (Node node in Navigator.IterateNodes(root, NavigationMode.DepthFirst)) {
            // Skip system as they are written in a special way
            if (node.Path.StartsWith(systemPath, StringComparison.Ordinal)) {
                continue;
            }

            var info = new NodeInfo {
                IsOverlay = false,
                Node = node,
                Offset = 0,
            };

            if (!node.IsContainer && node.Stream is null) {
                throw new FormatException("Child is not binary");
            }

            bool hasId = node.Tags.ContainsKey("scenegate.ekona.id");
            if (hasId) {
                info.Id = node.Tags["scenegate.ekona.id"];
            } else {
                info.Id = node.IsContainer ? 0xF000 | dirIndex++ : fileIndex++;
                node.Tags["scenegate.ekona.id"] = info.Id;
            }

            nodesById.Add(info.Id, info);

            if (!node.IsContainer) {
                bool hasPhysicalId = node.Tags.ContainsKey("scenegate.ekona.physical_id");
                if (hasPhysicalId) {
                    info.PhysicalId = node.Tags["scenegate.ekona.physical_id"];
                } else {
                    info.PhysicalId = physicalIndex++;
                    node.Tags["scenegate.ekona.physical_id"] = info.PhysicalId;
                }

                nodesByOffset.Add(info.PhysicalId, info);
            }
        }
    }

    private void WriteEmptyDigest()
    {
        uint sha1HashLength = 0x14;
        sectionInfo.DigestBlockSectorCount = 0x20;
        sectionInfo.DigestSectorSize = 0x400;

        // Digest needs padding to its sector size, so pad ROM content to it.
        writer.Stream.Seek(0, SeekOrigin.End);
        writer.WritePadding(0xFF, (int)sectionInfo.DigestSectorSize);

        sectionInfo.DigestNitroOffset = sectionInfo.Arm9Offset;
        sectionInfo.DigestNitroLength = (uint)(writer.Stream.Length - sectionInfo.DigestNitroOffset);

        // Twilight digest offset will be set later, once we know this size.
        uint arm9iLength = (uint)GetChildFormatSafe<IBinary>("system/arm9i").Stream.Length.Pad(TwilightPaddingSize);
        uint arm7iLength = (uint)GetChildFormatSafe<IBinary>("system/arm7i").Stream.Length.Pad(TwilightPaddingSize);
        sectionInfo.DigestTwilightLength = arm9iLength + arm7iLength;

        uint numSectorHashes = (sectionInfo.DigestNitroLength + sectionInfo.DigestTwilightLength) / sectionInfo.DigestSectorSize;
        uint sectorHashLength = numSectorHashes * sha1HashLength;

        uint numBlockHashes = numSectorHashes.Pad((int)sectionInfo.DigestBlockSectorCount) / sectionInfo.DigestBlockSectorCount;
        uint blockHashLength = numBlockHashes * sha1HashLength;

        sectionInfo.DigestSectorHashtableOffset = (uint)writer.Stream.Length;
        sectionInfo.DigestSectorHashtableLength = sectorHashLength.Pad(PaddingSize);
        writer.WriteTimes(0xCA, sectorHashLength);
        writer.WritePadding(0, PaddingSize);

        sectionInfo.DigestBlockHashtableOffset = (uint)writer.Stream.Length;
        sectionInfo.DigestBlockHashtableLength = blockHashLength;
        writer.WriteTimes(0xFE, blockHashLength);
        writer.WritePadding(0xFF, PaddingSize);
    }

    private (uint, uint) FakeModcryptEncrypt(ModcryptTargetKind target)
    {
        return target switch {
            ModcryptTargetKind.None => (0, 0),
            ModcryptTargetKind.Arm9 => (sectionInfo.Arm9Offset, sectionInfo.Arm9Size),
            ModcryptTargetKind.Arm7 => (sectionInfo.Arm7Offset, sectionInfo.Arm7Size),
            ModcryptTargetKind.Arm9i => (sectionInfo.Arm9iOffset, sectionInfo.Arm9iSize),
            ModcryptTargetKind.Arm9iSecureArea => (sectionInfo.Arm9iOffset, SecureAreaLength),
            ModcryptTargetKind.Arm7i => (sectionInfo.Arm7iOffset, sectionInfo.Arm7iSize),
            _ => throw new NotSupportedException($"Unsupported modcrypt area: {target}"),
        };
    }

    private struct NodeInfo
    {
        public Node Node { get; init; }

        public bool IsOverlay { get; init; }

        public int Id { get; set; }

        public int PhysicalId { get; set; }

        public uint Offset { get; init; }
    }
}
#nullable restore
