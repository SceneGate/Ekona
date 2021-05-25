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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using Yarhl.FileFormat;
using Yarhl.FileSystem;
using Yarhl.IO;

namespace SceneGate.Ekona.Containers.Rom
{
    /// <summary>
    /// Converter for binary formats into a NitroRom container.
    /// </summary>
    public class Binary2NitroRom : IConverter<IBinary, NitroRom>
    {
        private DataReader reader;
        private NitroRom rom;
        private RomHeader header;
        private FileAddress[] addresses;

        /// <summary>
        /// Read the internal info of a ROM file.
        /// </summary>
        /// <param name="source">Source binary format to read from.</param>
        /// <returns>The new node container.</returns>
        public NitroRom Convert(IBinary source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            reader = new DataReader(source.Stream);
            rom = new NitroRom();

            ReadHeader();
            ReadBanner();
            ReadFat();
            ReadPrograms();
            ReadFileSystem();

            return rom;
        }

        private void ReadHeader()
        {
            reader.Stream.Seek(Binary2RomHeader.HeaderSizeOffset, SeekOrigin.Begin);
            int headerSize = reader.ReadInt32();
            using var binaryHeader = new BinaryFormat(reader.Stream, 0, headerSize);
            header = (RomHeader)ConvertFormat.With<Binary2RomHeader>(binaryHeader);

            rom.System.Children["info"].ChangeFormat(header.ProgramInfo);

            var binaryStream = DataStreamFactory.FromArray(header.CopyrightLogo, 0, header.CopyrightLogo.Length);
            var binaryLogo = new BinaryFormat(binaryStream);
            rom.System.Children["copyright_logo"].ChangeFormat(binaryLogo);
        }

        private void ReadBanner()
        {
            reader.Stream.Position = header.SectionInfo.BannerOffset;
            int bannerSize = Binary2Banner.GetSize(reader.Stream);
            var binaryBanner = new BinaryFormat(reader.Stream, header.SectionInfo.BannerOffset, bannerSize);
            rom.System.Children["banner"].ChangeFormat(binaryBanner);
            rom.System.Children["banner"].TransformWith<Binary2Banner>();
        }

        private void ReadFat()
        {
            int numFiles = (int)(header.SectionInfo.FatSize / 0x08);
            addresses = new FileAddress[numFiles];

            reader.Stream.Position = header.SectionInfo.FatOffset;
            for (int i = 0; i < numFiles; i++) {
                uint offset = reader.ReadUInt32();
                uint endOffset = reader.ReadUInt32();
                addresses[i] = new FileAddress {
                    Offset = offset,
                    Size = endOffset - offset,
                };
            }
        }

        private void ReadPrograms()
        {
            var arm9 = new BinaryFormat(reader.Stream, header.SectionInfo.Arm9Offset, header.SectionInfo.Arm9Size);
            rom.System.Children["arm9"].ChangeFormat(arm9);
            ReadOverlayTable(
                header.ProgramInfo.Overlays9Info,
                header.SectionInfo.Overlay9TableOffset,
                header.SectionInfo.Overlay9TableSize);

            var arm7 = new BinaryFormat(reader.Stream, header.SectionInfo.Arm7Offset, header.SectionInfo.Arm7Size);
            rom.System.Children["arm7"].ChangeFormat(arm7);
            ReadOverlayTable(
                header.ProgramInfo.Overlays7Info,
                header.SectionInfo.Overlay7TableOffset,
                header.SectionInfo.Overlay7TableSize);
        }

        private void ReadOverlayTable(Collection<OverlayInfo> infos, uint offset, int size)
        {
            reader.Stream.Position = offset;
            int numFiles = size / 0x20;
            for (int i = 0; i < numFiles; i++) {
                var overlayInfo = new OverlayInfo();
                overlayInfo.OverlayId = reader.ReadUInt32();
                overlayInfo.RamAddress = reader.ReadUInt32();
                overlayInfo.RamSize = reader.ReadUInt32();
                overlayInfo.BssSize = reader.ReadUInt32();
                overlayInfo.StaticInitStart = reader.ReadUInt32();
                overlayInfo.StaticInitEnd = reader.ReadUInt32();
                overlayInfo.OverlayId = reader.ReadUInt32();
                uint encodingInfo = reader.ReadUInt32();
                overlayInfo.CompressedSize = encodingInfo & 0x00FFFFFF;
                overlayInfo.IsCompressed = ((encodingInfo >> 24) & 0x01) == 1;
                overlayInfo.IsSigned = ((encodingInfo >> 24) & 0x02) == 2;

                infos.Add(overlayInfo);

                var fileInfo = addresses[overlayInfo.OverlayId];
                var overlay = new BinaryFormat(reader.Stream, fileInfo.Offset, fileInfo.Size);
                rom.System.Children["overlays9"].Add(new Node($"overlay_{i}", overlay));
            }
        }

        private void ReadFileSystem()
        {
            var encoding = Encoding.GetEncoding("shift_jis");
            var directoryQueue = new Queue<(int Id, Node Current)>();

            directoryQueue.Enqueue((0xF000, rom.Data));
            while (directoryQueue.Count > 0) {
                var info = directoryQueue.Dequeue();

                // Read directory sub-entries definition
                reader.Stream.Position = header.SectionInfo.FntOffset + ((info.Id & 0xFFF) * 8);
                uint entriesOffset = reader.ReadUInt32();
                int fileId = reader.ReadUInt16();
                reader.ReadUInt16(); // parent ID

                // Read each entry data
                reader.Stream.Position = header.SectionInfo.FntOffset + entriesOffset;
                byte nodeType = reader.ReadByte();
                while (nodeType != 0) {
                    bool isFile = (nodeType & 0x80) == 0;
                    int nameLength = nodeType & 0x7F;
                    string name = reader.ReadString(nameLength, encoding);

                    Node node;
                    int id;
                    if (isFile) {
                        id = fileId++;
                        var fileInfo = addresses[id];
                        var fileData = new BinaryFormat(reader.Stream, fileInfo.Offset, fileInfo.Size);
                        node = new Node(name, fileData);
                    } else {
                        id = reader.ReadUInt16();
                        node = NodeFactory.CreateContainer(name);
                        directoryQueue.Enqueue((id, node));
                    }

                    node.Tags["scenegate.ekona.id"] = id;
                    info.Current.Add(node);

                    nodeType = reader.ReadByte();
                }
            }
        }

        private struct FileAddress
        {
            public uint Offset { get; init; }

            public uint Size { get; init; }
        }
    }
}
