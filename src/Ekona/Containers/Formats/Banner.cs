// Banner.cs
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
    using System.Text;
    using Yarhl.FileFormat;
    using Yarhl.IO;

    /// <summary>
    /// Represents the banner of a NDS game ROM.
    /// </summary>
    public class Banner : IFormat
    {
        /// <summary>
        /// Gets the size of the banner (with padding).
        /// </summary>
        public static uint Size {
            get { return 0x840 + 0x1C0; }
        }

        /// <summary>
        /// </summary>
        public ushort Version {
            get;
            set;
        }

        /// <summary>
        /// CRC-16 of structure, from 0x20 to 0x83.
        /// </summary>
        /// <value></value>
        public ushort Crc16 {
            get;
            set;
        }

        /// <summary>
        /// CRC-16 of structure, from 0x20 to 0x93, version 2 only.
        /// </summary>
        /// <value></value>
        public ushort Crc16v2 {
            get;
            set;
        }

        public byte[] Reserved {
            get;
            set;
        }

        public byte[] TileData {
            get;
            set;
        }

        public byte[] Palette {
            get;
            set;
        }

        public string JapaneseTitle {
            get;
            set;
        }

        public string EnglishTitle {
            get;
            set;
        }

        public string FrenchTitle {
            get;
            set;
        }

        public string GermanTitle {
            get;
            set;
        }

        public string ItalianTitle {
            get;
            set;
        }

        public string SpanishTitle {
            get;
            set;
        }
    }
}
