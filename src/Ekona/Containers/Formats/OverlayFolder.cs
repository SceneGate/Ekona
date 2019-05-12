//-----------------------------------------------------------------------
// <copyright file="OverlayFolder.cs" company="none">
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
// <date>01/03/2013</date>
//-----------------------------------------------------------------------
namespace Nitro.Rom
{
    using System;
	using Yarhl;
    using Yarhl.FileSystem;
    using Yarhl.IO;

    /// <summary>
    /// Folder with all the overlays of the same ARM.
    /// </summary>
	public class OverlayFolder : NodeContainerFormat
    {
        private bool isArm9;
        
        private OverlayFolder(bool isArm9)
            : base(isArm9 ? "Overlays9" : "Overlays7")
        {
            this.isArm9 = isArm9;
        }
        
        /// <summary>
        /// Gets a value indicating whether the folder contains overlays from the ARM9 or ARM7.
        /// </summary>
        public bool IsArm9
        {
            get { return this.isArm9; }
        }
        
        /// <summary>
        /// Create a folder with all the overlays of the same ARM.
        /// </summary>
        /// <param name="str">Stream to read the overlay table.</param>
        /// <param name="header">Header of the current ROM.</param>
        /// <param name="isArm9">If must read overlays from the ARM9 or ARM7.</param>
        /// <param name="listFiles">List with all the files in the ROM.</param>
        /// <returns>Folder with overlays.</returns>
		public static OverlayFolder FromTable(DataStream str, RomHeader header, bool isArm9, Node[] listFiles)
        {
            OverlayFolder overlays = new OverlayFolder(isArm9);
            
            int numOverlays;
			if (isArm9) {
				str.Seek(header.Ov9TableOffset, SeekMode.Origin);
                numOverlays = (int)(header.Ov9TableSize / OverlayFile.TableEntrySize);
			} else {
				str.Seek(header.Ov7TableOffset, SeekMode.Origin);
                numOverlays = (int)(header.Ov7TableSize / OverlayFile.TableEntrySize);
            }
            
            for (int i = 0; i < numOverlays; i++)
				overlays.AddFile(OverlayFile.FromTable(str, isArm9, listFiles));
            
            return overlays;
        }
        
        /// <summary>
        /// Write the table of overlays info.
        /// </summary>
        /// <param name="str">Stream to write to.</param>
        /// <param name="header">Header of the ROM to update.</param>
        /// <param name="shiftOffset">Amount of data before the current stream.</param>
		public void WriteTable(DataStream str, RomHeader header, uint shiftOffset)
        {   
            long startOffset = str.Position;
            int numOverlays = 0;   
            
			// For each overlay, writes its info
			foreach (Node file in this.Files) {
                OverlayFile ov = file as OverlayFile;
				if (ov == null)
					continue;

                ov.WriteTable(str);
                numOverlays++;
            }
            
			// Updates RomHeader
			if (this.isArm9) {
                header.Ov9TableSize = (uint)(numOverlays * OverlayFile.TableEntrySize);
                if (numOverlays > 0)
                    header.Ov9TableOffset = (uint)(shiftOffset + startOffset);
                else
                    header.Ov9TableOffset = 0x00;
			} else {
                header.Ov7TableSize = (uint)(numOverlays * OverlayFile.TableEntrySize);
                if (numOverlays > 0)
                    header.Ov7TableOffset = (uint)(shiftOffset + startOffset);
                else
                    header.Ov7TableOffset = 0x00;
            }
        }        
    }
}
