// ArmEncodeFieldFinder.cs
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
namespace Ekona.Containers.Converters
{
    using System;
    using Yarhl.IO;
    using Ekona.Containers.Formats;

    /// <summary>
    /// Find the constant with the decoded size of the ARM.
    /// </summary>
    public static class ArmEncodeFieldFinder
    {
        const int SecureAreaSize = 0x4000;

        // First ARM instructions of the BLZ decoder
        static readonly uint[] DecoderOps = new uint[]
        {
            0xE9100006, 0xE0802002, 0xE0403C21, 0xE3C114FF,
            0xE0401001, 0xE1A04002
        };

        // Number of bytes from the header to the first instruction in DecoderOps
        const int DecoderShift = 0x0C;

        /// <summary>
        /// Searchs the encoded size address.
        /// </summary>
        /// <returns>The encoded size address. 0 if not found. 1 if game is homebrew.</returns>
        public static uint SearchEncodedSizeAddress(Arm arm)
        {
            // Steps to find the ARM9 size address that we need to change
            // in order to fix the BLZ decoded error.
            // 0º Check the game is not homebrew.
            // 1º Get ARM9 entry address.
            // 2º From that point and while we're in the secure zone,
            //    search the decode_BLZ routine.
            // 3º Search previous BL (jump) instruction that call the decoder.
            // 4º Search instructions before it that loads R0 (parameter of decode_BLZ).
            DataReader reader = new DataReader(arm.Stream);

            // 0º
            // TODO
            // if (this.Tags.ContainsKey("_GameCode_") && (string)this.Tags["_GameCode_"] == "####") {
            //     return 0x01;
            // }

            // 1º
            uint entryAddress = arm.EntryAddress - arm.RamAddress;

            // 2º
            arm.Stream.Seek(entryAddress, SeekMode.Start);
            uint decoderAddress = SearchDecoder(arm.Stream);
            if (decoderAddress == 0x00) {
                throw new FormatException("Invalid decoder address");
            }

            // 3º & 4º
            arm.Stream.Seek(entryAddress, SeekMode.Start);
            uint baseOffset = SearchBaseOffset(arm.Stream, decoderAddress);
            if (baseOffset == 0x00) {
                throw new FormatException("Invalid base offset");
            }

            // Get relative address (not RAM address)
            arm.Stream.Seek(baseOffset, SeekMode.Start);
            uint sizeAddress = reader.ReadUInt32() + 0x14;	// Size is at 0x14 from that address
            sizeAddress -= arm.RamAddress;

            return sizeAddress;
        }

        static uint SearchDecoder(DataStream stream)
        {
            DataReader reader = new DataReader(stream);
            long startPosition = stream.Position;

            uint decoderAddress = 0x00;
            while (stream.Position - startPosition < SecureAreaSize && decoderAddress == 0x00)
            {
                long loopPosition = stream.Position;

                // Compare instructions to see if it's the routing we want
                bool found = true;
                for (int i = 0; i < DecoderOps.Length && found; i++) {
                    if (reader.ReadUInt32() != DecoderOps[i]) {
                            found = false;
                    }
                }

                if (found)
                    decoderAddress = (uint)loopPosition - DecoderShift; // Get start of routine
                else
                    stream.Seek(loopPosition + 4, SeekMode.Start);  // Go to next instruction
            }

            return decoderAddress;
        }

        static uint SearchBaseOffset(DataStream stream, uint decoderAddress)
        {
            DataReader reader = new DataReader(stream);
            uint instr;

            // Search the instruction: BL DecoderAddress
            // Where DecoderAddress=(PC+8+nn*4)
            bool found = false;
            while (stream.Position < decoderAddress && !found)
            {
                instr = reader.ReadUInt32();
                if ((instr & 0xFF000000) == 0xEB000000) {
                    uint shift = instr & 0x00FFFFFF;
                    shift = 4 + shift * 4;

                    // Check if that jump goes to the correct routine
                    if (stream.Position + shift == decoderAddress)
                        found = true;
                }
            }

            // Search for the Load instruction, btw LDR R1=[PC+ZZ].
            // Usually two instruction before.
            stream.Seek(-0x0C, SeekMode.Current);
            uint baseOffset = 0x00;
            instr = reader.ReadUInt32();
            if ((instr & 0xFFFF0000) == 0xE59F0000)
                baseOffset = (uint)stream.Position + (instr & 0xFFF) + 4;

            // If not found... Should we continue looking above instructions?
            // I run a test with > 500 games and at the moment it is always there
            return baseOffset;
        }
    }
}