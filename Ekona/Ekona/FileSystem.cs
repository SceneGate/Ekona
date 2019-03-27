//-----------------------------------------------------------------------
// <copyright file="FileSystem.cs" company="none">
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
// <date>16/02/2013</date>
//-----------------------------------------------------------------------
namespace Ekona.Formats
{
    using Yarhl.FileFormat;
    using Yarhl.FileSystem;
    using Yarhl.IO;

    /// <summary>
    /// Represents the file system of a NDS game ROM.
    /// </summary>
	public sealed class FileSystem : NodeContainerFormat
    {        
        private RomHeader header;
		private Node root;         // Parent folder with ROM files.
		private Node sysFolder;    // Folder with ARM, overlays and info files.
             
        /// <summary>
        /// Gets the padding used in the ROM.
        /// </summary>
        public static int PaddingAddress
        {
            get { return 0x200; }
        }
        
        /// <summary>
        /// Gets the padding byte used in the ROM.
        /// </summary>
        public static byte PaddingByte
        {
            get { return 0xFF; }
        }
        
        /// <summary>
        /// Gets the parent folder of the game.
        /// </summary>
		public Node Root
        {
            get { return this.root; }
        }
        
        /// <summary>
        /// Gets the folder with system files.
        /// </summary>
		public Node SystemFolder
        {
            get { return this.sysFolder; }
        }

		public override void Initialize(GameFile file, params object[] parameters)
		{
			base.Initialize(file, parameters);

			if (parameters.Length == 1)
				this.header = (RomHeader)parameters[0];
		}

		/// <summary>
		/// Read the file system of the ROM and create the folder tree.
		/// </summary>
		/// <param name="str">Stream to read the file system.</param>
		public override void Read(DataStream str)
		{
			// Read File Allocation Table
			// I'm creating a new DataStream variable because Fat class needs to know its length.
			DataStream fatStr = new DataStream(str, this.header.FatOffset, this.header.FatSize);
			Fat fat = new Fat();
			fat.Read(fatStr);
			fatStr.Dispose();

			// Read File Name Table
			str.Seek(header.FntOffset, SeekMode.Origin);
			Fnt fnt = new Fnt();
			fnt.Read(str);

			// Get root folder
			this.root = fnt.CreateTree(fat.GetFiles());

			// Get ARM and Overlay files
			this.sysFolder = new GameFolder("System");

			this.sysFolder.AddFile(ArmFile.FromStream(str, this.header, true));
			this.sysFolder.AddFolder(OverlayFolder.FromTable(str, this.header, true, fat.GetFiles()));

			this.sysFolder.AddFile(ArmFile.FromStream(str, this.header, false));
			this.sysFolder.AddFolder(OverlayFolder.FromTable(str, this.header, false, fat.GetFiles()));
		}
	
        /// <summary>
        /// Write the file system to the stream.
        /// </summary>
        /// <param name="str">Stream to write to.</param>
		public override void Write(DataStream str)
        {
            // The order is: ARM9 - Overlays9 - ARM7 - Overlays7 - FNT - FAT
			this.WriteArm(str, true);	// Write ARM9
			this.WriteArm(str, false);	// Write ARM7
            
			// To get the first ROM file ID
            int numOverlays = this.CountOverlays(false) + this.CountOverlays(true);
            
			// Create a new File Name Table...
			Fnt fnt = new Fnt();
			fnt.Initialize(null, this.root, numOverlays);

			// ... and writes it
            this.header.FntOffset = (uint)(this.header.HeaderSize + str.Position);
            long fntStartOffset = str.Position;
            fnt.Write(str);
            this.header.FntSize = (uint)(str.Position - fntStartOffset);
			str.WritePadding(FileSystem.PaddingByte, FileSystem.PaddingAddress);
            
			// Create a temp dir with every file to be register in the FAT (Game + System)
			GameFolder tmpFolder = new GameFolder(string.Empty);
			tmpFolder.AddFolders(this.sysFolder.Folders);	// Overlay folders
			tmpFolder.AddFolder(this.root);					// Game files
            
			// Write File Allocation Table
			Fat fat = new Fat();
			fat.Initialize(null, tmpFolder, Banner.Size + this.header.HeaderSize);	// First file offset after banner
			this.header.FatOffset = (uint)(this.header.HeaderSize + str.Position);
			fat.Write(str);
			str.WritePadding(FileSystem.PaddingByte, FileSystem.PaddingAddress);
			this.header.FatSize = fat.Size;
        }
                
        /// <summary>
		/// Writes every ROM file (except System files) into the Stream.
        /// </summary>
		/// <param name="strOut">Output stream.</param>
		public void WriteFiles(DataStream strOut)
        {  
			// Uses the Fat class because there it's donde the implementation
			// to sorted them                      
			Fat fat = new Fat();
			fat.Initialize(null, this.root);
			fat.WriteFiles(strOut);
        }
             
		private void WriteArm(DataStream str, bool isArm9)
        {
            // Write the ARM file.
			foreach (GameFile file in this.sysFolder.Files) {
                ArmFile arm = file as ArmFile;
				if (arm == null || arm.IsArm9 != isArm9)
					continue;

				// Writes to this Stream but sets the its address after RomHeader
                arm.UpdateAndWrite(str, this.header, this.header.HeaderSize);
				str.WritePadding(FileSystem.PaddingByte, FileSystem.PaddingAddress);
            }
            
			// Writes the overlay table and overlays
			foreach (GameFolder folder in this.sysFolder.Folders) {
                OverlayFolder overlayFolder = folder as OverlayFolder;
				if (overlayFolder == null || overlayFolder.IsArm9 != isArm9)
					continue;

				// Writes overlay info
                overlayFolder.WriteTable(str, this.header, this.header.HeaderSize);
				str.WritePadding(FileSystem.PaddingByte, FileSystem.PaddingAddress);
                
				// Write overlays
				foreach (GameFile file in overlayFolder.Files) {
					OverlayFile overlay = file as OverlayFile;
					if (overlay == null)
						continue;

					overlay.WriteAddress = (uint)(this.header.HeaderSize + str.Position);
					overlay.Stream.WriteTo(str);
					str.WritePadding(FileSystem.PaddingByte, FileSystem.PaddingAddress);
				}
            }
        }
        
        private int CountOverlays(bool isArm9)
        {
            int count = 0;
            
			foreach (GameFolder folder in this.sysFolder.Folders) {
                OverlayFolder overlayFolder = folder as OverlayFolder;
                if (overlayFolder == null || overlayFolder.IsArm9 != isArm9)                   
                    continue;
                
				foreach (GameFile file in overlayFolder.Files) {
                    if (file is OverlayFile)
                        count++;
                }
            }
            
            return count;
        }

    }
}