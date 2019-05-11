//-----------------------------------------------------------------------
// <copyright file="OverlayFile.cs" company="none">
// Copyright (C) 2013
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by 
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful, 
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General Public License for more details. 
//
//   You should have received a copy of the GNU General Public License
//   along with this program.  If not, see "http://www.gnu.org/licenses/". 
// </copyright>
// <author>pleoNeX</author>
// <email>benito356@gmail.com</email>
// <date>28/02/2013</date>
//-----------------------------------------------------------------------
namespace Ekona
{
    using Yarhl.FileSystem;
    using Yarhl.IO;

    /// <summary>
    /// Represents an overlay file.
    /// </summary>
	public class OverlayFile : Node
    {
		private OverlayFile(Node baseFile, bool isArm9)
			: base(string.Empty, baseFile.Stream)
        {
			this.Tags["Id"] = baseFile.Tags["Id"];
			this.Name = "Overlay" + (isArm9 ? "9" : "7") + "_" + this.Tags["Id"].ToString() + ".bin";
        }
                
        #region Properties
        
        /// <summary>
        /// Gets the size of the overlay table entry.
        /// </summary>
        public static uint TableEntrySize
        {
            get { return 0x20; }
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
        /// Info from here: http://gbatemp.net/threads/recompressing-an-overlay-file.329576/#post-4387691
        /// </summary>
        /// <value><c>true</c> if this instance is signed; otherwise, <c>false</c>.</value>
        public bool IsSigned {
            get;
            set;
        }
        
        /// <summary>
        /// Gets or sets the address where this overlay will be written
        /// </summary>
        public uint WriteAddress {
			get;
			set;
        }
        
        #endregion
        
        /// <summary>
        /// Create a new overlay file from the info in the overlay table.
        /// </summary>
        /// <param name="str">Stream to read the table.</param>
        /// <param name="listFiles">List of files where the overlay must be.</param>
        /// <returns>Overlay file.</returns>
		public static OverlayFile FromTable(DataStream str, bool isArm9, Node[] listFiles)
        {
			DataReader dr = new DataReader(str);
            
			str.Seek(0x18, SeekMode.Current);
            uint fileId = dr.ReadUInt32();
			str.Seek(-0x1C, SeekMode.Current);
            
			OverlayFile overlay = new OverlayFile(listFiles[fileId], isArm9);
			overlay.OverlayId       = dr.ReadUInt32();
			overlay.RamAddress      = dr.ReadUInt32();
			overlay.RamSize         = dr.ReadUInt32();
			overlay.BssSize         = dr.ReadUInt32();
            overlay.StaticInitStart = dr.ReadUInt32();
			overlay.StaticInitEnd   = dr.ReadUInt32();
            dr.ReadUInt32();    // File ID again
			uint encodingInfo   = dr.ReadUInt32();
            overlay.EncodedSize = encodingInfo & 0x00FFFFFF;
            overlay.IsEncoded   = ((encodingInfo >> 24) & 0x01) == 1;
            overlay.IsSigned    = ((encodingInfo >> 24) & 0x02) == 2;
            
            return overlay;
        }
        
        /// <summary>
        /// Write the table info of this overlay.
        /// </summary>
        /// <param name="str">Stream to write to.</param>
		public void WriteTable(DataStream str)
        {
			DataWriter dw = new DataWriter(str);
            
			this.EncodedSize = (uint)this.Length;
			uint encodingInfo = this.EncodedSize;
            encodingInfo |= (uint)((this.IsEncoded ? 1 : 0) << 24);
            encodingInfo |= (uint)((this.IsSigned  ? 2 : 0) << 24);

			dw.Write(this.OverlayId);
			dw.Write(this.RamAddress);
			dw.Write(this.RamSize);
			dw.Write(this.BssSize);
			dw.Write(this.StaticInitStart);
			dw.Write(this.StaticInitEnd);
			dw.Write((uint)(ushort)this.Tags["Id"]);
            dw.Write(encodingInfo);
        }
    }
}
