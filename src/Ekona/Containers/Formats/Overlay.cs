// Overlay.cs
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
    using Yarhl.FileFormat;
    using Yarhl.FileSystem;
    using Yarhl.IO;

    /// <summary>
    /// Overlay format (dynamic-load code).
    /// </summary>
    public class Overlay : IBinary
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Overlay" /> class
        /// with a new empty stream.
        /// </summary>
        public Overlay()
        {
            Stream = new DataStream();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Overlay" /> class.
        /// </summary>
        /// <param name="stream">Stream with the overlay data.</param>
        public Overlay(DataStream stream)
        {
            Stream = stream;
        }

        /// <summary>
        /// Gets the size of the overlay table entry.
        /// </summary>
        public static uint TableEntrySize {
            get { return 0x20; }
        }

        /// <summary>
        /// Gets the overlay program data.
        /// </summary>
        public DataStream Stream {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the processor type for this overlay.
        /// </summary>
        public ProcessorType Processor {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the ID of the overlay.
        /// </summary>
        public uint OverlayId {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the address where the overlay will be load in the RAM.
        /// </summary>
        public uint RamAddress {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the amount of bytes to load in RAM of the overlay.
        /// </summary>
        public uint RamSize {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the size of the BSS data region.
        /// </summary>
        public uint BssSize {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the static initialization start address.
        /// </summary>
        public uint StaticInitStart {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the static initialization end address.
        /// </summary>
        public uint StaticInitEnd {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the size of the overlay encoded. 0 if no encoding.
        /// </summary>
        public uint EncodedSize {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the overlay is encoding.
        /// </summary>
        public bool IsEncoded {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether thie overlay is digitally signed.
        /// </summary>
        /// <value><c>true</c> if this instance is signed; otherwise, <c>false</c>.</value>
        /// <remarks>
        /// More information here:
        /// http://gbatemp.net/threads/recompressing-an-overlay-file.329576/#post-4387691
        /// </remarks>
        public bool IsSigned {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the address where this overlay will be written.
        /// </summary>
        public uint WriteAddress {
            get;
            set;
        }
    }
}
