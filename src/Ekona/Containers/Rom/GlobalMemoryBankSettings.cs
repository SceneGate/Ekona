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
/// DSi extended memory global memory bank settings (MBK 1 to 5).
/// </summary>
public class GlobalMemoryBankSettings
{
    /// <summary>
    /// Gets or sets the processor reserved for this memory.
    /// </summary>
    /// <remarks>
    /// For MBK1 only ARM9 or ARM7.
    /// For MBK2-3: also DSP for code.
    /// For MBK4-5: also DSP for data.
    /// </remarks>
    public MemoryBankProcessor Processor { get; set; }

    /// <summary>
    /// Gets or sets the offset slot in blocks.
    /// </summary>
    /// <remarks>
    /// For MBK1: 64 KB units.
    /// For MBK2-5: 32 KB units.
    /// </remarks>
    public byte OffsetSlot { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this memory bank is enabled.
    /// </summary>
    public bool Enabled { get; set; }
}
