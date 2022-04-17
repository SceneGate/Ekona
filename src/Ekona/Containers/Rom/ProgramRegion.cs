// Copyright (c) 2022 SceneGate

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

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
/// Supported regions for the program.
/// </summary>
[Flags]
public enum ProgramRegion : uint
{
    /// <summary>
    /// Typical Nintendo supported regions (DS only).
    /// </summary>
    Normal = 0,

    /// <summary>
    /// Japan region (DSi only).
    /// </summary>
    Japan = 1 << 0,

    /// <summary>
    /// USA region (DSi only).
    /// </summary>
    Usa = 1 << 1,

    /// <summary>
    /// Europe region (DSi only).
    /// </summary>
    Europe = 1 << 2,

    /// <summary>
    /// Australia region (DSi only).
    /// </summary>
    Australia = 1 << 3,

    /// <summary>
    /// China region.
    /// </summary>
    China = 1 << 4,

    /// <summary>
    /// Korea region.
    /// </summary>
    Korea = 1 << 5,

    /// <summary>
    /// Region free, support on all possible regions.
    /// </summary>
    RegionFree = 0xFFFFFFFF,
}
