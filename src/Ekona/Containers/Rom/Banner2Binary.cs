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
using System.Text;
using Texim.Colors;
using Texim.Images;
using Texim.Pixels;
using Yarhl.FileFormat;
using Yarhl.FileSystem;
using Yarhl.IO;

namespace SceneGate.Ekona.Containers.Rom;

/// <summary>
/// Convert a container with banner information into a binary stream.
/// </summary>
/// <remarks>
/// <para>Supported versions: 0.1, 0.2, 0.3 and 1.3 (except animated icons).</para>
/// <para>The input container expects to have:</para>
/// <list type="table">
/// <item><term>/info</term><description>Program banner content with Banner format.</description></item>
/// <item><term>/icon</term><description>Program icon with IndexedPaletteImage format.</description></item>
/// </list>
/// </remarks>
public class Banner2Binary : IConverter<NodeContainerFormat, BinaryFormat>
{
    /// <summary>
    /// Write a container banner into a binary format.
    /// </summary>
    /// <param name="source">Banner to serialize into binary format.</param>
    /// <returns>The new serialized binary.</returns>
    public BinaryFormat Convert(NodeContainerFormat source)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        Banner banner = GetFormatSafe<Banner>(source.Root, "info");
        IndexedPaletteImage icon = GetFormatSafe<IndexedPaletteImage>(source.Root, "icon");

        var binary = new BinaryFormat();
        var writer = new DataWriter(binary.Stream) {
            DefaultEncoding = Encoding.Unicode,
        };

        // Write empty header, as we need the data to generate checksum
        writer.WriteTimes(0, 0x20);

        WriteIcon(writer, icon);
        WriteTitles(writer, banner);

        if (banner.Version.Major > 0) {
            WriteAnimatedIcon(writer);
        }

        writer.Stream.Position = 0;
        WriteHeader(writer, banner);

        return binary;
    }

    private static T GetFormatSafe<T>(Node root, string childName)
        where T : class, IFormat
    {
        Node child = root.Children[childName] ?? throw new FormatException($"Missing child '{childName}'");
        return child.GetFormatAs<T>()
            ?? throw new FormatException($"Child '{childName}' has not the expected format: {typeof(T).Name}");
    }

    private static void WriteHeader(DataWriter writer, Banner banner)
    {
        writer.Write((byte)banner.Version.Minor);
        writer.Write((byte)banner.Version.Major);

        writer.WriteComputedCrc16(0x20, 0x820);

        if (banner.Version.Minor > 1) {
            writer.WriteComputedCrc16(0x20, 0x920);
        } else {
            writer.Write((ushort)0x00);
        }

        if (banner.Version.Minor > 2) {
            writer.WriteComputedCrc16(0x20, 0xA20);
        } else {
            writer.Write((ushort)0x00);
        }

        if (banner.Version.Major > 0) {
            writer.WriteComputedCrc16(0x1240, 0x1180);
        } else {
            writer.Write((ushort)0x00);
        }

        writer.WriteTimes(0, 0x16); // reserved
    }

    private static void WriteIcon(DataWriter writer, IndexedPaletteImage icon)
    {
        var swizzling = new TileSwizzling<IndexedPixel>(icon.Width);
        var pixels = swizzling.Swizzle(icon.Pixels);
        writer.Write<Indexed4Bpp>(pixels);

        if (icon.Palettes.Count != 1) {
            throw new FormatException("Invalid number of palettes for icon, expected 1");
        }

        writer.Write<Bgr555>(icon.Palettes[0].Colors);
    }

    private static void WriteTitles(DataWriter writer, Banner banner)
    {
        writer.Write(banner.JapaneseTitle, 0x100);
        writer.Write(banner.EnglishTitle, 0x100);
        writer.Write(banner.FrenchTitle, 0x100);
        writer.Write(banner.GermanTitle, 0x100);
        writer.Write(banner.ItalianTitle, 0x100);
        writer.Write(banner.SpanishTitle, 0x100);

        if (banner.Version.Minor > 1) {
            writer.Write(banner.ChineseTitle, 0x100);
        }

        if (banner.Version.Minor > 2) {
            writer.Write(banner.KoreanTitle, 0x100);
        }
    }

    private static void WriteAnimatedIcon(DataWriter writer)
    {
        // TODO: implement properly
        writer.WriteUntilLength(0xFF, 0x1240);
        writer.Stream.Position = 0x1240;

        writer.WriteTimes(0, 0x1000); // 8 bitmaps
        writer.WriteTimes(0, 0x100); // 8 palettes
        writer.WriteTimes(0, 0x80); // animation sequence
    }
}
