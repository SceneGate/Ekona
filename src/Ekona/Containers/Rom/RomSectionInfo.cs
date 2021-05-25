// Copyright(c) 2021 SceneGate
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
namespace SceneGate.Ekona.Containers.Rom
{
    /// <summary>
    /// Information about the different sections of the ROM.
    /// </summary>
    public class RomSectionInfo
    {
        /// <summary>
        /// Gets or sets the offset of the ARM-9 program.
        /// </summary>
        public uint Arm9Offset { get; set; }

        /// <summary>
        /// Gets or sets the size of the ARM-9 program.
        /// </summary>
        public uint Arm9Size { get; set; }

        /// <summary>
        /// Gets or sets the offset of the ARM-7 program.
        /// </summary>
        public uint Arm7Offset { get; set; }

        /// <summary>
        /// Gets or sets the size of the ARM-7 program.
        /// </summary>
        public uint Arm7Size { get; set; }

        /// <summary>
        /// Gets or sets the offset to the file name table.
        /// </summary>
        public uint FntOffset { get; set; }

        /// <summary>
        /// Gets or sets the size of the file name table.
        /// </summary>
        public uint FntSize { get; set; }

        /// <summary>
        /// Gets or sets the offset of the file access table.
        /// </summary>
        public uint FatOffset { get; set; }

        /// <summary>
        /// Gets or sets the size of the file access table.
        /// </summary>
        public uint FatSize { get; set; }

        /// <summary>
        /// Gets or sets the offset of the table of overlays for ARM-9 program.
        /// </summary>
        public uint Overlay9TableOffset { get; set; }

        /// <summary>
        /// Gets or sets the size of the table of overlays for ARM-9 program.
        /// </summary>
        public int Overlay9TableSize { get; set; }

        /// <summary>
        /// Gets or sets the offset of the table of overlays for ARM-7 program.
        /// </summary>
        public uint Overlay7TableOffset { get; set; }

        /// <summary>
        /// Gets or sets the size of the table of overlays for ARM-7 program.
        /// </summary>
        public int Overlay7TableSize { get; set; }

        /// <summary>
        /// Gets or sets the offset of the program banner.
        /// </summary>
        public uint BannerOffset { get; set; }

        /// <summary>
        /// Gets or sets the exact size of the ROM.
        /// </summary>
        public uint RomSize { get; set; }

        /// <summary>
        /// Gets or sets the size of the header.
        /// </summary>
        public uint HeaderSize { get; set; }
    }
}
