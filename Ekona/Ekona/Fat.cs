//-----------------------------------------------------------------------
// <copyright file="Fat.cs" company="none">
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
// <date>26/02/2013</date>
//-----------------------------------------------------------------------
namespace Nitro.Rom
{
    using System;
	using System.Collections.Generic;
	using System.Linq;
	using Yarhl;
    using Yarhl.FileFormat;
    using Yarhl.IO;

    /// <summary>
    /// File Allocation Table
    /// </summary>
	public sealed class Fat : Format
    {
        private const int FatEntrySize = 0x08;
        
		private GameFile[] files;
        private uint firstOffset;        // Offset where the first file is.   
        
        #region Properties

        /// <summary>
        /// Gets the size of a <see cref="Fat" /> section.
        /// </summary>
        public uint Size
        {
            get { return (uint)(this.files.Length * FatEntrySize); }
        }
        
        /// <summary>
        /// Gets the relative offset to the first file.
        /// </summary>
        public uint FirstFileOffset
        {
            get { return this.firstOffset; }
        }
        
        #endregion
		   
		public override void Initialize(GameFile file, params object[] parameters)
		{
			base.Initialize(file, parameters);

			if (parameters.Length >= 1) {
				// Get a list with all files...
				GameFolder root = parameters[0] as GameFolder;
				List<GameFile> fileList = new List<GameFile>();
				foreach (FileContainer f in root.GetFilesRecursive(false))
					fileList.Add(f as GameFile);

				// ... and sort them by Id
				fileList.OrderBy(f => (ushort)f.Tags["Id"]);
				this.files = fileList.ToArray();
			}

			if (parameters.Length == 2)
				this.firstOffset = (uint)parameters[1];
		}
		 
        /// <summary>
        /// Write the Fat to a stream.
        /// </summary>
        /// <param name="str">Stream to write to.</param>
		public override void Write(DataStream str)
        {
			DataWriter dw = new DataWriter(str);
            uint offset = (uint)str.Position + this.Size + this.firstOffset;
			offset = offset.Pad(FileSystem.PaddingAddress);
            
			foreach (GameFile file in this.files) {
                OverlayFile overlay = file as OverlayFile;
                
				if (overlay == null) {
                    dw.Write(offset);       // Start offset
					offset += (uint)file.Length;
                    dw.Write(offset);       // End offset
				} else {
                    dw.Write(overlay.WriteAddress);
					dw.Write((uint)(overlay.WriteAddress + overlay.Length));
                }
                
				offset = offset.Pad(FileSystem.PaddingAddress);	// Pad offset
            }
        }
        
		public void WriteFiles(DataStream strOut)
		{
			// Write every file
			foreach (GameFile file in this.files) {
				file.Stream.WriteTo(strOut);
				strOut.WritePadding(FileSystem.PaddingByte, FileSystem.PaddingAddress);
			}
		}

        /// <summary>
        /// Gets the files in the Fat section.
        /// </summary>
        /// <returns>Array with RomFiles instances.</returns>
		public GameFile[] GetFiles()
        {
            return this.files;
        }
           
        /// <summary>
        /// Read a Fat section from a stream.
        /// </summary>
        /// <param name="str">Stream to read from.</param>
		public override void Read(DataStream str)
        {
			this.files = new GameFile[str.Length / FatEntrySize];
			DataReader dr = new DataReader(str);
            
            uint startOffset, endOffset;
			for (ushort i = 0; i < this.files.Length; i++) {
                startOffset = dr.ReadUInt32();
				endOffset   = dr.ReadUInt32();
				this.files[i] = new GameFile(
					string.Empty,	// Name will be added later in FNT
					new DataStream(str.BaseStream, startOffset, endOffset - startOffset)); // TODO: FIX
				this.files[i].Tags["Id"] = i;
            }
            
			if (this.files.Length > 0) {
				if (this.files[0].Stream.Offset > str.Position - this.Size)
					this.firstOffset = (uint)(this.files[0].Stream.Offset - (str.Position - this.Size));
				else
					this.firstOffset = (uint)((str.Position - this.Size) - this.files[0].Stream.Offset);
			} else {
                this.firstOffset = 0xFFFFFFFF;
            }
        }
    }
}
