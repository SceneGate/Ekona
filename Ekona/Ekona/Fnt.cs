//-----------------------------------------------------------------------
// <copyright file="Fnt.cs" company="none">
// Copyright (C) 2019
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
    using System.Text;
	using Yarhl;
    using Yarhl.FileFormat;
    using Yarhl.FileSystem;
    using Yarhl.IO;

    /// <summary>
    /// File Name Table
    /// </summary>
	public sealed class Fnt : Format
    {
        private const int FntEntrySize = 0x08;
        private static readonly Encoding DefaultEncoding = Encoding.GetEncoding("shift_jis");
        
        private Fnt.FntTable[] tables;
		
		public void Initialize(Node file, params object[] parameters)
		{
			base.Initialize(file, parameters);

			if (parameters.Length == 2) {
				this.CreateTables((GameFolder)parameters[0], (int)parameters[1]);
			}
		}
		  
		/// <summary>
		/// Create the folder tree.
		/// </summary>
		/// <param name="files">Files in the tree.</param>
		/// <returns>Root folder</returns>
		public GameFolder CreateTree(Node[] files)
		{
			GameFolder root = new GameFolder("ROM");
			root.Tags["Id"] =  (ushort)this.tables.Length;
			this.CreateTree(root, files);
			return root;
		}

        /// <summary>
        /// Write a <see cref="Fnt" /> section in a stream.
        /// </summary>
        /// <param name="str">Stream to write to.</param>
		public void Write(DataStream str)
        {
			DataWriter dw = new DataWriter(str);
            long baseOffset = str.RelativePosition;

            // Write main tables
			foreach (Fnt.FntTable table in this.tables) {
                dw.Write(table.Offset);
                dw.Write(table.IdFirstFile);
                dw.Write(table.IdParentFolder);
            }
            
            // Write subtables
            foreach (Fnt.FntTable table in this.tables)
                table.Write(str, baseOffset);
        }
                
        /// <summary>
        /// Read a FNT section from a stream.
        /// </summary>
        /// <param name="str">Stream to read from.</param>
		public void Read(DataStream str)
        {
			DataReader dr = new DataReader(str);
			uint fntOffset = (uint)str.Position;

            // Get the number of directories and the offset to subtables
            //  from the main table.
			uint subtablesOffset = dr.ReadUInt32() + fntOffset;
            dr.ReadUInt16();
            ushort numDirs = dr.ReadUInt16();

            this.tables = new Fnt.FntTable[numDirs];
			for (int i = 0; i < numDirs; i++) {
				str.Seek(fntOffset + (i * FntEntrySize), SeekMode.Origin);
                
                // Error, in some cases the number of directories is wrong.
                // Found in FF Four Heroes of Light, Tetris Party deluxe.
				if (str.Position > subtablesOffset) {
                    numDirs = (ushort)i;
                    Array.Resize(ref this.tables, numDirs);
                    break;
                }
                
				FntTable table = new FntTable(
					dr.ReadUInt32(),	// Offset
					dr.ReadUInt16(),	// Id First File
					dr.ReadUInt16());	// Id Parent Folder

                // Read subtable
				str.Seek(fntOffset + table.Offset, SeekMode.Origin);
				table.Read(str);
            
                this.tables[i] = table;
            }
        }
                
		private static int CountDirectories(GameFolder folder)
        {
            int numDirs = folder.Folders.Count;
            
			foreach (GameFolder subfolder in folder.Folders)
                numDirs += CountDirectories(subfolder);
            
            return numDirs;
        }
        
		private static int ReassignFileIds(GameFolder folder, int currentId)
        {
			foreach (Node file in folder.Files)
				file.Tags["Id"] = (ushort)(currentId++);
            
			foreach (GameFolder subfolder in folder.Folders)
                currentId = ReassignFileIds(subfolder, currentId);
            
            return currentId;
        }
        
		private static ushort GetIdFirstFile(GameFolder folder)
        {
            ushort id = 0xFFFF;
            
            // Searchs in files
			foreach (Node file in folder.Files) {
				if ((ushort)file.Tags["Id"] < id)
					id = (ushort)file.Tags["Id"];
            }
            
            // Searchs in subfolders
			foreach (GameFolder subfolder in folder.Folders) {
                ushort fId = GetIdFirstFile(subfolder);
                if (fId < id)
                    id = fId;
            }
            
            return id;
        }
        
		private void CreateTree(GameFolder currentFolder, Node[] listFile)
        {
			int folderId = ((ushort)currentFolder.Tags["Id"] > 0x0FFF) ?
			                (ushort)currentFolder.Tags["Id"] & 0x0FFF : 0;
            
            // Add files
			foreach (ElementInfo fileInfo in this.tables[folderId].Files) {
				listFile[fileInfo.Id].Name = fileInfo.Name;
                currentFolder.AddFile(listFile[fileInfo.Id]);
            }
            
            // Add subfolders
			foreach (ElementInfo folderInfo in this.tables[folderId].Folders) {
				GameFolder subFolder = new GameFolder(folderInfo.Name);
				subFolder.Tags["Id"] =  folderInfo.Id;
                this.CreateTree(subFolder, listFile);
				currentFolder.AddFolder(subFolder);
            }
        }
        
		private void CreateTables(GameFolder root, int firstId)
        {
            int numDirs = CountDirectories(root) + 1;
            this.tables = new Fnt.FntTable[numDirs];

            ReassignFileIds(root, firstId);
            
            // For each directory create its table.
            uint subtableOffset = (uint)(this.tables.Length * Fnt.FntTable.MainTableSize);
            this.CreateTablesRecursive(root, (ushort)numDirs, ref subtableOffset);
        }
        
		private void CreateTablesRecursive(GameFolder currentFolder, ushort parentId, ref uint subtablesOffset)
        {
			int folderId = ((ushort)currentFolder.Tags["Id"] > 0x0FFF) ?
			                (ushort)currentFolder.Tags["Id"] & 0x0FFF : 0;
            
			this.tables[folderId] = new Fnt.FntTable(
				subtablesOffset,
				GetIdFirstFile(currentFolder),
				parentId);
            
            // Set the info values
			foreach (Node file in currentFolder.Files)
				this.tables[folderId].AddFileInfo(file.Name, (ushort)file.Tags["Id"]);
            
			foreach (GameFolder folder in currentFolder.Folders)
				this.tables[folderId].AddFolderInfo(folder.Name, (ushort)folder.Tags["Id"]);
            
            subtablesOffset += (uint)this.tables[folderId].GetInfoSize();
            
			foreach (GameFolder folder in currentFolder.Folders)
                this.CreateTablesRecursive(folder, (ushort)(0xF000 | folderId), ref subtablesOffset);
        }

        private struct ElementInfo
        {
            public string Name { get; set; }
            
            public ushort Id { get; set; }
        }
        
        /// <summary>
        /// Substructure inside of the File Name Table.
        /// </summary>
		private struct FntTable
        {
            private List<ElementInfo> folders;
            private List<ElementInfo> files;
            
            /// <summary>
            /// Initializes a new instance of the <see cref="FntTable" /> class.
            /// </summary>
			public FntTable(uint Offset, ushort idFirstFile, ushort idParentFolder)
				: this()
            {
				this.Offset = Offset;
				this.IdFirstFile = idFirstFile;
				this.IdParentFolder = idParentFolder;
                this.folders = new List<Fnt.ElementInfo>();
				this.files   = new List<Fnt.ElementInfo>();
            }
            
            /// <summary>
            /// Gets the size of the main table data.
            /// </summary>
			public static int MainTableSize {
                get { return 0x08; }
            }
            
            /// <summary>
            /// Gets or sets the relative offset to the folder info.
            /// </summary>
            public uint Offset {
				get;
				private set;
            }

            /// <summary>
            /// Gets or sets the id of the first file in this folder (or in its subfolders).
            /// </summary>
            public ushort IdFirstFile {
				get;
				private set;
            }

            /// <summary>
            /// Gets or sets the id of the parent folder.
            /// </summary>
            public ushort IdParentFolder {
				get;
				private set;
            }

            /// <summary>
            /// Gets the folders of that table.
            /// </summary>
            public List<ElementInfo> Folders 
            {
                get { return this.folders; }
            }
            
            /// <summary>
            /// Gets the files of that table.
            /// </summary>
            public List<ElementInfo> Files
            {
                get { return this.files; }
            }
            
            /// <summary>
            /// Add the info of a new file of that table.
            /// </summary>
            /// <param name="name">File name</param>
            /// <param name="id">File ID</param>
            public void AddFileInfo(string name, ushort id)
            {
                this.files.Add(new ElementInfo() { Name = name, Id = id });
            }
            
            /// <summary>
            /// Add the info of a new folder of that table.
            /// </summary>
            /// <param name="name">Folder name</param>
            /// <param name="id">Folder ID</param>
            public void AddFolderInfo(string name, ushort id)
            {
                this.folders.Add(new ElementInfo() { Name = name, Id = id });
            }
            
            /// <summary>
            /// Gets the size of the table info.
            /// </summary>
            /// <returns>Size of the table info.</returns>
            public int GetInfoSize()
            {
                int size = 1;   // End node type
                
				foreach (ElementInfo info in this.files) {
                    size += 1;  // Node type
                    size += DefaultEncoding.GetByteCount(info.Name);
                }
                
				foreach (ElementInfo info in this.folders) {
                    size += 3;  // Node type + folder ID
                    size += DefaultEncoding.GetByteCount(info.Name);
                }
                
                return size;
            }

			public void Read(DataStream str)
			{
				DataReader dr = new DataReader(str, EndiannessMode.LittleEndian, DefaultEncoding);

				byte nodeType = dr.ReadByte();
				ushort fileId = this.IdFirstFile;

				int nameLength;
				string name;

				// Read until the end of the subtable (reachs 0x00)
				while (nodeType != 0x0) {
					// If the node is a file.
					if (nodeType < 0x80) {
						nameLength = nodeType;
						name = dr.ReadString(nameLength);

						this.AddFileInfo(name, fileId++);
					} else {
						nameLength = nodeType - 0x80;
						name = dr.ReadString(nameLength);
						ushort folderId = dr.ReadUInt16();

						this.AddFolderInfo(name, folderId);
					}

					nodeType = dr.ReadByte();
				}
			}

            public void Write(DataStream str, long baseOffset)
			{
                DataWriter bw = new DataWriter(str);

                // Go to offset if we can. Maybe we are writing in a position
                // that it doesn't exist still (it will write written later)
                // so in this case we fill it with zeros
                long tableOffset = baseOffset + this.Offset;
                if (tableOffset > str.Length)
                    str.WriteTimes(0, tableOffset - str.Length);

                // And finally seek there
                str.Seek(baseOffset + this.Offset, SeekMode.Origin);

				byte nodeType;

				// Write file info
				foreach (ElementInfo info in this.Files) {
					nodeType = (byte)DefaultEncoding.GetByteCount(info.Name); // Name length
					bw.Write(nodeType);
					bw.Write(DefaultEncoding.GetBytes(info.Name));
				}

				// Write folder info
				foreach (ElementInfo info in this.Folders) {
					nodeType = (byte)(0x80 | DefaultEncoding.GetByteCount(info.Name));
					bw.Write(nodeType);
					bw.Write(DefaultEncoding.GetBytes(info.Name));
					bw.Write(info.Id);
				}

				bw.Write((byte)0x00);   // End of info
				bw = null;
			}
        }
    }
}
