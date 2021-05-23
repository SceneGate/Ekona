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
using System.Data.HashFunction;
using System.Data.HashFunction.CRC;
using System.IO;
using System.Text;
using Yarhl.FileFormat;
using Yarhl.IO;

namespace SceneGate.Ekona.Containers.Rom
{
    /// <summary>
    /// Convert a binary format into a <see cref="Banner"/> instance.
    /// </summary>
    public class Binary2Banner : IConverter<IBinary, Banner>
    {
        /// <summary>
        /// Gets the serialized size of the banner including padding.
        /// </summary>
        /// <param name="stream">The stream to analyze.</param>
        /// <remarks>
        /// The stream must be in the start of the banner.
        /// The position is restored to the start of the banner.
        /// </remarks>
        /// <returns>The expected size of the binary banner.</returns>
        public static int GetSize(Stream stream)
        {
            byte minor = (byte)stream.ReadByte();
            byte major = (byte)stream.ReadByte();
            stream.Position -= 2;

            var version = new Version(major, minor);
            return version switch {
                { Major: 0, Minor: 1 } => 0x0840,
                { Major: 0, Minor: 2 } => 0x0940,
                { Major: 0, Minor: 3 } => 0x0A40,
                { Major: 1, Minor: 3 } => 0x23C0,
                _ => throw new NotSupportedException(),
            };
        }

        /// <summary>
        /// Read a banner from a binary format.
        /// </summary>
        /// <param name="source">Source binary to read from.</param>
        /// <returns>The new deserialized banner.</returns>
        public Banner Convert(IBinary source)
        {
            var reader = new DataReader(source.Stream) {
                DefaultEncoding = Encoding.Unicode,
            };

            var banner = new Banner();
            ushort versionData = reader.ReadUInt16();
            banner.Version = new Version(versionData >> 8, versionData & 0xFF);
            banner.ChecksumBase = ValidateChecksum(reader.ReadUInt16(), source.Stream, 0x20, 0x820);

            if (banner.Version.Minor > 1) {
                banner.ChecksumChinese = ValidateChecksum(reader.ReadUInt16(), source.Stream, 0x20, 0x920);
            } else {
                source.Stream.Position += 2;
            }

            if (banner.Version.Minor > 2) {
                banner.ChecksumKorean = ValidateChecksum(reader.ReadUInt16(), source.Stream, 0x20, 0xA20);
            } else {
                source.Stream.Position += 2;
            }

            if (banner.Version.Major > 0) {
                banner.ChecksumAnimatedIcon = ValidateChecksum(reader.ReadUInt16(), source.Stream, 0x1240, 0x1180);
            } else {
                source.Stream.Position += 2;
            }

            source.Stream.Position += 0x16; // reserved
            banner.IconPixels = reader.ReadBytes(0x200); // TODO: Read using Texim
            banner.IconPalette = reader.ReadBytes(0x20);
            banner.JapaneseTitle = reader.ReadString(0x100).Replace("\0", string.Empty);
            banner.EnglishTitle = reader.ReadString(0x100).Replace("\0", string.Empty);
            banner.FrenchTitle = reader.ReadString(0x100).Replace("\0", string.Empty);
            banner.GermanTitle = reader.ReadString(0x100).Replace("\0", string.Empty);
            banner.ItalianTitle = reader.ReadString(0x100).Replace("\0", string.Empty);
            banner.SpanishTitle = reader.ReadString(0x100).Replace("\0", string.Empty);

            if (banner.Version.Minor > 1) {
                banner.ChineseTitle = reader.ReadString(0x100).Replace("\0", string.Empty);
            }

            if (banner.Version.Minor > 2) {
                banner.KoreanTitle = reader.ReadString(0x100).Replace("\0", string.Empty);
            }

            // TODO: Support animated icon.
            return banner;
        }

        private static ChecksumInfo<ushort> ValidateChecksum(ushort expected, DataStream stream, long offset, int length)
        {
            using var segment = new DataStream(stream, offset, length);

            ICRC crc = CRCFactory.Instance.Create(CRCConfig.MODBUS);
            IHashValue hash = crc.ComputeHash(segment);
            ushort actual = (ushort)(hash.Hash[0] | (hash.Hash[1] << 8));

            return new ChecksumInfo<ushort> {
                Expected = expected,
                Actual = actual,
            };
        }
    }
}
