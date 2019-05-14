// RomHeader.cs
//
// Copyright (c) 2019 SceneGate Team
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
namespace Ekona.Containers.Formats
{
    using System;
    using Yarhl.FileFormat;
    using Yarhl.IO;

    /// <summary>
    /// Header of the ROM.
    /// </summary>
    public class RomHeader : IFormat
    {
        const int MinCartridge = 17;

        /// <summary>
        /// Gets or sets the short game title.
        /// </summary>
        public string GameTitle {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the game code.
        /// </summary>
        public string GameCode {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the maker code.
        /// </summary>
        public string MakerCode {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the unit code.
        /// </summary>
        public byte UnitCode {
            get;
            set;
        }

        public byte EncryptionSeed {
            get;
            set;
        }

        public uint CartridgeSize {
            get;
            set;
        }

        public byte[] Reserved {
            get;
            set;
        }

        public byte RomVersion {
            get;
            set;
        }

        public byte InternalFlags {
            get;
            set;
        }

        public uint Arm9Offset {
            get;
            set;
        }

        public uint Arm9EntryAddress {
            get;
            set;
        }

        public uint Arm9RamAddress {
            get;
            set;
        }

        public uint Arm9Size {
            get;
            set;
        }

        public uint Arm7Offset {
            get;
            set;
        }

        public uint Arm7EntryAddress {
            get;
            set;
        }

        public uint Arm7RamAddress {
            get;
            set;
        }

        public uint Arm7Size {
            get;
            set;
        }

        public uint FntOffset {
            get;
            set;
        }

        public uint FntSize {
            get;
            set;
        }

        public uint FatOffset {
            get;
            set;
        }

        public uint FatSize {
            get;
            set;
        }

        public uint Ov9TableOffset {
            get;
            set;
        }

        public uint Ov9TableSize {
            get;
            set;
        }

        public uint Ov7TableOffset {
            get;
            set;
        }

        public uint Ov7TableSize {
            get;
            set;
        }

        /// <summary>
        /// Control register flags for read.
        /// </summary>
        public uint FlagsRead {
            get;
            set;
        }

        /// <summary>
        /// Control register flags for init.
        /// </summary>
        /// <value></value>
        public uint FlagsInit {
            get;
            set;
        }

        public uint BannerOffset {
            get;
            set;
        }

        /// <summary>
        /// Secure area CRC16 0x4000 - 0x7FFF.
        /// </summary>
        public ushort SecureCRC16 {
            get;
            set;
        }

        public ushort RomTimeout {
            get;
            set;
        }

        public uint Arm9Autoload {
            get;
            set;
        }

        public uint Arm7Autoload {
            get;
            set;
        }

        /// <summary>
        /// Magic number for unencrypted mode.
        /// </summary>
        public ulong SecureDisable {
            get;
            set;
        }

        public uint RomSize {
            get;
            set;
        }

        public uint HeaderSize {
            get;
            set;
        }

        public byte[] Reserved2 {
            get;
            set;
        }

        public byte[] NintendoLogo {
            get;
            set;
        }

        public ushort LogoCRC16 {
            get;
            set;
        }

        public ushort HeaderCRC16 {
            get;
            set;
        }

        public bool SecureCRC {
            get;
            set;
        }

        public bool LogoCRC {
            get;
            set;
        }

        public bool HeaderCRC {
            get;
            set;
        }

        public uint DebugRomOffset {
            get;
            set;
        }

        /// <summary>
        /// version with
        /// </summary>
        /// <value></value>
        public uint DebugSize {
            get;
            set;
        }

        /// <summary>
        /// 0 = none, SIO and 8 MB
        /// </summary>
        /// <value></value>
        public uint DebugRamAddress {
            get;
            set;
        }

        /// <summary>
        /// Zero filled transfered and stored but not used
        /// </summary>
        /// <value></value>
        public uint Reserved3 {
            get;
            set;
        }

        /// <summary>
        /// Rest of the header.
        /// </summary>
        public byte[] Unknown {
            get;
            set;
        }
    }
}
