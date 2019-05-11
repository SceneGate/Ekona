//-----------------------------------------------------------------------
// <copyright file="Rom.cs" company="none">
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
    using Yarhl.FileSystem;

    /* ROM sections:
     * 
     * Header (0x0000-0x4000)
     * ARM9 Binary
     *   |_ARM9
     *   |_ARM9 tail data
     *   |_ARM9 Overlays Tables
     *   |_ARM9 Overlays
     * ARM7 Binary
     *   |_ARM7
     *   |_ARM7 Overlays Tables
     *   |_ARM7 Overlays
     * FNT (File Name Table)
     *   |_Main tables
     *   |_Subtables (names)
     * FAT (File Allocation Table)
     *   |_Files offset
     *     |_Start offset
     *     |_End offset
     * Banner
     *   |_Header 0x20
     *   |_Icon (Bitmap + palette) 0x200 + 0x20
     *   |_Game titles (Japanese, English, French, German, Italian, Spanish) 6 * 0x100
     * Files...
     */

    /// <summary>
    /// Class to manage internally a ROM file.
    /// </summary>
    public sealed class Rom : NodeContainerFormat
    {       
        public RomHeader Header
        {
            get;
            set;
        }

        public Banner Banner
        {
            get;
            set;
        }

        public NodeContainerFormat FileSystem
        {
            get;
            set;
        }

    }
}
