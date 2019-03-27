//-----------------------------------------------------------------------
// <copyright file="ArmFile.cs" company="none">
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
namespace Nitro.Rom
{
    using System;
    using System.Collections.Generic;
    using Yarhl.FileFormat;
    using Yarhl.FileSystem;
    using Yarhl.IO;
    
    /// <summary>
    /// Description of ArmFile.
    /// </summary>
	public class ArmFile : Node
    {        
        private byte[] unknownTail;
        
		private ArmFile(string name, DataStream str)
			: base(name, str)
		{
		}   
		     
        /// <summary>
        /// Gets a value indicating whether represents the ARM9 or ARM7.
        /// </summary>
        public bool IsArm9 {
			get;
			private set;
        }
        
        /// <summary>
        /// Gets or sets the entry address of the ARM.
        /// </summary>
        public uint EntryAddress {
			get;
			set;
        }
        
        /// <summary>
        /// Gets or sets the auto-load address of the ARM.
        /// </summary>
        public uint Autoload {
			get;
			set;
        }
        
        /// <summary>
        /// Gets or sets the address of the ARM in the RAM.
        /// </summary>
        public uint RamAddress {
			get;
			set;
        }
        
        /// <summary>
        /// Create a new ARM file from the info of the ROM header.
        /// </summary>
        /// <param name="str">Stream to read the unknown tail.</param> 
        /// <param name="header">Header of the current ROM.</param>
        /// <param name="romPath">Path to the current ROM.</param>
        /// <param name="isArm9">Indicates if must create an ARM9 or ARM7 file.</param>
        /// <returns>ARM file.</returns>
		public static ArmFile FromStream(DataStream str, RomHeader header, bool isArm9)
        {
            ArmFile arm;

			if (isArm9) {
				arm = new ArmFile("ARM9.bin", new DataStream(str, header.Arm9Offset, header.Arm9Size));
				arm.IsArm9 = true;
				arm.EntryAddress = header.Arm9EntryAddress;
				arm.Autoload     = header.Arm9Autoload;
				arm.RamAddress   = header.Arm9RamAddress;
			} else {
				arm = new ArmFile("ARM7.bin", new DataStream(str, header.Arm7Offset, header.Arm7Size));
				arm.IsArm9 = false;
                arm.EntryAddress = header.Arm7EntryAddress;
				arm.Autoload     = header.Arm7Autoload;
				arm.RamAddress   = header.Arm7RamAddress;
            }
            
            // Set the unknown tail
			if (isArm9) {   
                // It's after the ARM9 file.
				str.Seek(header.Arm9Offset + header.Arm9Size, SeekMode.Origin);
                
				// Read until reachs padding byte
                List<byte> tail = new List<byte>();
                /*byte b = (byte)str.ReadByte();
				while (b != 0xFF) {
                    tail.Add(b);
                    b = (byte)str.ReadByte();
                }*/
                
                arm.unknownTail = tail.ToArray();
            }
            
            return arm;
        }
        
        /// <summary>
        /// Update and write the Arm file
        /// </summary>
        /// <param name="str">Stream to write</param>
        /// <param name="header">Rom header to update.</param>
        /// <param name="shiftOffset">Amount of data before that stream.</param>
		public void UpdateAndWrite(DataStream str, RomHeader header, uint shiftOffset)
        {
			if (this.IsArm9) {
				header.Arm9Autoload     = this.Autoload;
				header.Arm9EntryAddress = this.EntryAddress;
				header.Arm9RamAddress   = this.RamAddress;
				header.Arm9Size         = (uint)this.Length;
				if (this.Length > 0x00)
                    header.Arm9Offset = (uint)(shiftOffset + str.Position);
                else
                    header.Arm9Offset = 0x00;
			} else {
				header.Arm7Autoload     = this.Autoload;
				header.Arm7EntryAddress = this.EntryAddress;
				header.Arm7RamAddress   = this.RamAddress;
				header.Arm7Size         = (uint)this.Length;
				if (this.Length > 0x00)
                    header.Arm7Offset = (uint)(shiftOffset + str.Position);
                else
                    header.Arm7Offset = 0x00;
            }
            
            // Write the file
			this.Stream.WriteTo(str);
            
			if (this.IsArm9) {
				// Update encoded size if it was set
				uint encodeSize = this.SearchEncodedSizeAddress();
				if (encodeSize != 0 && encodeSize != 1) {
					this.Stream.Seek(encodeSize, SeekMode.Origin);
					if (new DataReader(this.Stream).ReadUInt32() != 0x00) {
						str.Seek(encodeSize, SeekMode.Origin);
						new DataWriter(str).Write((uint)(this.Length + this.RamAddress));
						str.Seek(0, SeekMode.End);
					}
				}
			}

            // Write the unknown tail
            if (this.unknownTail != null)
                str.Write(this.unknownTail, 0, this.unknownTail.Length);
        }
        
        /// <summary>
        /// Gets the unknown tail bytes.
        /// </summary>
        /// <returns>ARM tail</returns>
        public byte[] GetTail()
        {
            return this.unknownTail;
        }

		#region ARM9 Encode Size Finder

		private const int SecureAreaSize = 0x4000;

		// First ARM instructions of the BLZ decoder
		private static readonly uint[] DecoderOps = new uint[]  
		{
			0xE9100006, 0xE0802002, 0xE0403C21, 0xE3C114FF,
			0xE0401001, 0xE1A04002
		};

		// Number of bytes from the header to the first instruction in DecoderOps
		private const int DecoderShift = 0x0C;

		/// <summary>
		/// Searchs the encoded size address.
		/// </summary>
		/// <returns>The encoded size address. 0 if not found. 1 if game is homebrew.</returns>
		private uint SearchEncodedSizeAddress()
		{
			/*
     	 	 * Steps to find the ARM9 size address that we need to change
     	 	 * in order to fix the BLZ decoded error.
     	 	 * 
     	 	 * 0º Check the game is not homebrew.
     	 	 * 1º Get ARM9 entry address.
     	 	 * 2º From that point and while we're in the secure zone,
     	 	 *    search the decode_BLZ routine.
     	 	 * 3º Search previous BL (jump) instruction that call the decoder.
     	 	 * 4º Search instructions before it that loads R0 (parameter of decode_BLZ).
     	 	 */

			DataReader reader = new DataReader(this.Stream);

			// 0º
			if (this.Tags.ContainsKey("_GameCode_") && (string)this.Tags["_GameCode_"] == "####")
				return 0x01;

			// 1º
			uint entryAddress = this.EntryAddress - this.RamAddress;

			// 2º
			this.Stream.Seek(entryAddress, SeekMode.Origin);
			uint decoderAddress = SearchDecoder();
			if (decoderAddress == 0x00) {
				Console.WriteLine("INVALID decoder address.");
				return 0x00;
			}

			// 3º & 4º
			this.Stream.Seek(entryAddress, SeekMode.Origin);
			uint baseOffset = SearchBaseOffset(decoderAddress);
			if (baseOffset == 0x00) {
				Console.WriteLine("INVALID base offset.");
				return 0x00;
			}

			// Get relative address (not RAM address)
			this.Stream.Seek(baseOffset, SeekMode.Origin);
			uint sizeAddress = reader.ReadUInt32() + 0x14;	// Size is at 0x14 from that address
			sizeAddress -= this.RamAddress;

			return sizeAddress;
		}

		private uint SearchDecoder()
		{
			DataReader reader = new DataReader(this.Stream);
			long startPosition = this.Stream.Position;

			uint decoderAddress = 0x00;
			while (this.Stream.Position - startPosition < SecureAreaSize && decoderAddress == 0x00)
			{
				long loopPosition = this.Stream.RelativePosition;

				// Compare instructions to see if it's the routing we want
				bool found = true;
				for (int i = 0; i < DecoderOps.Length && found; i++) {
					if (reader.ReadUInt32() != DecoderOps[i]) {
							found = false;
					}
				}

				if (found)
					decoderAddress = (uint)loopPosition - DecoderShift;		// Get start of routine
				else
					this.Stream.Seek(loopPosition + 4, SeekMode.Origin);	// Go to next instruction
			}

			return decoderAddress;
		}

		private uint SearchBaseOffset(uint decoderAddress)
		{
			DataReader reader = new DataReader(this.Stream);
			uint instr;

			// Search the instruction: BL DecoderAddress
			// Where DecoderAddress=(PC+8+nn*4)
			bool found = false;
			while (this.Stream.RelativePosition < decoderAddress && !found)
			{
				instr = reader.ReadUInt32();
				if ((instr & 0xFF000000) == 0xEB000000) {
					uint shift = instr & 0x00FFFFFF;
					shift = 4 + shift * 4;

					// Check if that jump goes to the correct routine
					if (this.Stream.RelativePosition + shift == decoderAddress)
						found = true;
				}
			}

			// Search for the Load instruction, btw LDR R1=[PC+ZZ].
			// Usually two instruction before.
			this.Stream.Seek(-0x0C, SeekMode.Current);
			uint baseOffset = 0x00;
			instr = reader.ReadUInt32();
			if ((instr & 0xFFFF0000) == 0xE59F0000)
				baseOffset = (uint)this.Stream.RelativePosition + (instr & 0xFFF) + 4;

			// If not found... Should we continue looking above instructions?
			// I run a test with > 500 games and at the moment it is always there

			return baseOffset;
		}
		
		#endregion
    }
}
