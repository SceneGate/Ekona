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
namespace SceneGate.Ekona.Containers.Rom;

/// <summary>
/// Target area to modcrypt.
/// </summary>
public enum ModcryptTargetKind
{
    /// <summary>
    /// No area have modcrypt.
    /// </summary>
    None,

    /// <summary>
    /// ARM9 program.
    /// </summary>
    Arm9,

    /// <summary>
    /// ARM7 program.
    /// </summary>
    Arm7,

    /// <summary>
    /// ARM9i (DSi) program.
    /// </summary>
    Arm9i,

    /// <summary>
    /// ARM9i (DSi) secure area program only.
    /// </summary>
    Arm9iSecureArea,

    /// <summary>
    /// ARM7i (DSi) program.
    /// </summary>
    Arm7i,

    /// <summary>
    /// Unknown area.
    /// </summary>
    Unknown,
}
