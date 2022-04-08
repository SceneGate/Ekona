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
using Yarhl.FileFormat;
using Yarhl.FileSystem;
using Yarhl.IO;

namespace SceneGate.Ekona.Containers.Rom;

/// <summary>
/// Converter from NitroRom containers into binary format.
/// </summary>
public class NitroRom2Binary :
    IInitializer<Stream>,

    IConverter<NodeContainerFormat, BinaryFormat>
{
    private const int PaddingSize = 0x200;
    private readonly SortedList<int, NodeInfo> nodesById = new SortedList<int, NodeInfo>();
    private readonly SortedList<int, NodeInfo> nodesByOffset = new SortedList<int, NodeInfo>();
    private Stream? initializedOutputStream;
    private DataWriter writer = null!;
    private Node root = null!;
    private RomInfo programInfo = null!;
    private RomSectionInfo sectionInfo = null!;

    /// <summary>
    /// Initializes the converter with the output stream for the next conversion.
    /// </summary>
    /// <param name="parameters">The output stream for the next conversion.</param>
    /// <exception cref="ArgumentNullException">The argument is null.</exception>
    public void Initialize(Stream parameters) =>
        initializedOutputStream = parameters ?? throw new ArgumentNullException(nameof(parameters));

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
        // - ARM9: program, table, overlays
        // - ARM7: program, table, overlays
        // - FNT: main tables, entry tables
        // - FAT: including overlays
        // - Banner
        // - Files
        var binary = new BinaryFormat(initializedOutputStream ?? new DataStream());
        binary.Stream.Position = 0;
        writer = new DataWriter(binary.Stream);

        programInfo = GetChildFormatSafe<RomInfo>("system/info");
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

        WriteHeader();

        // Fill size to the cartridge size
        sectionInfo.RomSize = (uint)writer.Stream.Length;
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

        // TODO: Recalculate HMACs.
        // Set the new copyright logo if it exists
        Node? logoNode = root.Children["system"]?.Children["copyright_logo"];
        if (logoNode is not null) {
            DataStream binaryLogo = logoNode.Stream ?? throw new FormatException("Invalid format for copyright_logo");
            header.CopyrightLogo = new DataReader(binaryLogo).ReadBytes((int)binaryLogo.Length);
        } else {
            header.CopyrightLogo = new byte[156];
        }

        sectionInfo.RomSize = (uint)writer.Stream.Length;

        // We don't calculate the header length but we expect it's preset.
        // It's used inside the converter to pad.
        var binaryHeader = (BinaryFormat)ConvertFormat.With<RomHeader2Binary>(header);
        writer.Stream.Position = 0;
        binaryHeader.Stream.WriteTo(writer.Stream);
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
    }

    private void WritePrograms()
    {
        WriteProgram(true);
        WriteOverlays(true);

        WriteProgram(false);
        WriteOverlays(false);
    }

    private void WriteProgram(bool isArm9)
    {
        string armPath = isArm9 ? "system/arm9" : "system/arm7";

        // TODO: Update ARM9 compressed size before writing.
        // TODO: Write ARM9 tail
        BinaryFormat binaryArm = GetChildFormatSafe<BinaryFormat>(armPath);
        uint armLength = (uint)binaryArm.Stream.Length;
        uint armOffset = armLength > 0 ? (uint)writer.Stream.Position : 0;
        binaryArm.Stream.WriteTo(writer.Stream);
        writer.WritePadding(0xFF, PaddingSize);

        if (isArm9) {
            sectionInfo.Arm9Offset = armOffset;
            sectionInfo.Arm9Size = armLength;
        } else {
            sectionInfo.Arm7Offset = armOffset;
            sectionInfo.Arm7Size = armLength;
        }
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
            uint encodingInfo = info.CompressedSize;
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
        int physicalIndex = 0;

        // Overlays are written like regular files but in a special location.
        // Skip the index here, added later when writing the file that we know the ID.
        string systemPath = $"/{root.Name}/system";
        fileIndex += GetChildSafe("system/overlays9").Children.Count;
        fileIndex += GetChildSafe("system/overlays7").Children.Count;
        physicalIndex = fileIndex;

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
