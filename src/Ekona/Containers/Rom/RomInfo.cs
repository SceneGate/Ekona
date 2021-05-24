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
using Yarhl.FileFormat;

namespace SceneGate.Ekona.Containers.Rom
{
    /// <summary>
    /// Information about the content of the program.
    /// </summary>
    public class RomInfo : IFormat
    {
        /// <summary>
        /// Gets the minimum cartridge size.
        /// </summary>
        public static long MinimumCartridgeSize => 1 << 17;

        /// <summary>
        /// Gets or sets the short game title.
        /// </summary>
        public string GameTitle { get; set; }

        /// <summary>
        /// Gets or sets the game code.
        /// </summary>
        public string GameCode { get; set; }

        /// <summary>
        /// Gets or sets the maker code.
        /// </summary>
        public string MakerCode { get; set; }

        /// <summary>
        /// Gets or sets the unit code.
        /// </summary>
        public byte UnitCode { get; set; }

        public byte EncryptionSeed { get; set; }

        /// <summary>
        /// Gets or sets the size of the ROM cartridge.
        /// </summary>
        public long CartridgeSize { get; set; }

        public byte DsiFlags { get; set; }

        public byte Region { get; set; }

        public byte RomVersion { get; set; }

        public byte AutoStartFlag { get; set; }

        public uint Arm9EntryAddress { get; set; }

        public uint Arm9RamAddress { get; set; }

        public uint Arm7EntryAddress { get; set; }

        public uint Arm7RamAddress { get; set; }

        /// <summary>
        /// Control register flags for read.
        /// </summary>
        public uint FlagsRead { get; set; }

        /// <summary>
        /// Control register flags for init.
        /// </summary>
        public uint FlagsInit { get; set; }

        /// <summary>
        /// Secure area CRC16 0x4000 - 0x7FFF.
        /// </summary>
        public ChecksumInfo<ushort> ChecksumSecureArea { get; set; }

        public ushort SecureAreaDelay { get; set; }

        public uint Arm9Autoload { get; set; }

        public uint Arm7Autoload { get; set; }

        /// <summary>
        /// Magic number for unencrypted mode.
        /// </summary>
        public ulong SecureDisable { get; set; }

        public ChecksumInfo<ushort> ChecksumLogo { get; set; }

        public ChecksumInfo<ushort> ChecksumHeader { get; set; }

        public uint DebugRomOffset { get; set; }

        public uint DebugSize { get; set; }

        public uint DebugRamAddress { get; set; }
    }
}
