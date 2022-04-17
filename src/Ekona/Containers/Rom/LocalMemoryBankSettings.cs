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
/// DSi extended memory local (to a processor) memory bank settings (MBK 6 to 8).
/// </summary>
public class LocalMemoryBankSettings
{
    /// <summary>
    /// Gets or sets the start address slot.
    /// </summary>
    /// <remarks>
    /// Final address is: 0x03000000 + value * (0x010000 for MBK6 or 0x8000 for 7-8).
    /// </remarks>
    public int StartAddressSlot { get; set; }

    /// <summary>
    /// Gets or sets the image size kind.
    /// </summary>
    /// <remarks>
    /// According to existing research:
    /// MBK6: 0 or 1 = 64 KB /slot0, 2 = 128 KB / slot0-2, 3 = 256 KB / slot0-3
    /// MBK7-8: 0 = 32 KB / slot0, 1 = 64 KB / slot0-1, 2 = 128 KB / slot0-3, 3 = 256 KB / slot0-7
    /// </remarks>
    public int ImageSize { get; set; }

    /// <summary>
    /// Gets or sets the end address slot.
    /// </summary>
    /// <remarks>
    /// Final address is: 0x03000000 + value * (0x010000 for MBK6 or 0x8000 for 7-8) - 1.
    /// </remarks>
    public int EndAddressSlot { get; set; }
}
