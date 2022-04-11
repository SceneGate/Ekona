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
/// Security mode for DSi ROMs.
/// </summary>
[Flags]
public enum DsiCryptoMode
{
    /// <summary>
    /// ROM contains a TWL-exclusive region (DSi games).
    /// </summary>
    TwlExclusiveRegion = 1 << 0,

    /// <summary>
    /// ROM is encrypted with modcrypt.
    /// </summary>
    Modcrypted = 1 << 1,

    /// <summary>
    /// ROM modcryption uses debug key.
    /// </summary>
    ModcryptKeyDebug = 1 << 2,

    /// <summary>
    /// Disable debug features.
    /// </summary>
    DisableDebug = 1 << 3,
}
