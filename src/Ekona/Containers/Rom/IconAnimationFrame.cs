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
using System;

namespace SceneGate.Ekona.Containers.Rom;

/// <summary>
/// Frame definition for the ROM animated icon.
/// </summary>
public class IconAnimationFrame
{
    private int bitmapIndex;
    private int paletteIndex;

    /// <summary>
    /// Gets or sets the duration of the frame in milliseconds.
    /// </summary>
    /// <remarks>
    /// The resolution is 60 Hz.
    /// </remarks>
    public int Duration { get; set; }

    /// <summary>
    /// Gets or sets the index of the bitmap for this frame.
    /// </summary>
    /// <remarks>
    /// There are maximum 8 bitmaps.
    /// </remarks>
    public int BitmapIndex {
        get => bitmapIndex;
        set {
            if (value < 0 || value >= Binary2Banner.NumAnimatedImages) {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Animated icons can't have more than 8 bitmaps");
            }

            bitmapIndex = value;
        }
    }

    /// <summary>
    /// Gets or sets the palette to use for the bitmap of this frame.
    /// </summary>
    /// <remarks>
    /// There are maximum 8 palettes.
    /// </remarks>
    public int PaletteIndex {
        get => paletteIndex;
        set {
            if (value < 0 || value >= Binary2Banner.NumAnimatedImages) {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Animated icons can't have more than 8 palettes");
            }

            paletteIndex = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the bitmap should flip horizontally.
    /// </summary>
    public bool FlipHorizontal { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the bitmap should flip vertically.
    /// </summary>
    public bool FlipVertical { get; set; }
}
