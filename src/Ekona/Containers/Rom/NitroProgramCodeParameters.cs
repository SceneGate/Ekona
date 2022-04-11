// Copyright(c) 2022 SceneGate
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
namespace SceneGate.Ekona.Containers.Rom;

/// <summary>
/// Nitro and twilight parameters related to the code program (arm9).
/// </summary>
public class NitroProgramCodeParameters
{
    /// <summary>
    /// Gets or sets the offsets to the ARM9 parameters table offset in DS programs.
    /// DSi programs have this value in the header.
    /// </summary>
    public uint ProgramParameterOffset { get; set; }

    /// <summary>
    /// Gets or sets the offset to an optional area in the program with additional hashes.
    /// </summary>
    public uint ExtraHashesOffset { get; set; }

    /// <summary>
    /// Gets or sets the ITCM first block info offset.
    /// </summary>
    /// <remarks>
    /// A block info consists in two uint values: output RAM address and size.
    /// After moving the ITCM to the output, is clean so we can reuse the place for BSS.
    /// </remarks>
    public uint ItcmBlockInfoOffset { get; set; }

    /// <summary>
    /// Gets or sets the end offset for the ITCM block info section.
    /// </summary>
    public uint ItcmBlockInfoEndOffset { get; set; }

    /// <summary>
    /// Gets or sets the ITCM input data offset.
    /// </summary>
    public uint ItcmInputDataOffset { get; set; }

    /// <summary>
    /// Gets or sets the BSS offset (non-initialized global/static variables area).
    /// </summary>
    /// <remarks>
    /// Usually this value matches with <see cref="ItcmInputDataOffset"/>
    /// because after moving the ITCM code, we can reuse that area.
    /// </remarks>
    public uint BssOffset { get; set; }

    /// <summary>
    /// Gets or sets the end offset for BSS.
    /// </summary>
    public uint BssEndOffset { get; set; }

    /// <summary>
    /// Gets or sets the program (arm9) decompressed length.
    /// If 0, then the program is not compressed.
    /// </summary>
    public uint DecompressedLength { get; set; }
}
