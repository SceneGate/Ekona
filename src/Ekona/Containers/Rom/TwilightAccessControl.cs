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
using System.Diagnostics.CodeAnalysis;

namespace SceneGate.Ekona.Containers.Rom;

/// <summary>
/// Access control of DSi (Twilight) software.
/// </summary>
[Flags]
[SuppressMessage("", "S4070", Justification = "False positive")]
public enum TwilightAccessControl
{
    /// <summary>
    /// No access control set.
    /// </summary>
    None = 0,

    /// <summary>
    /// Use "common key" -> 0x3080F000 = 0x03FFC600 + 0x00.
    /// </summary>
    CommonClientKey = 1 << 0,

    /// <summary>
    /// Use AES slot B -> 0x0380F010 = 0x03FFC400 + 0x180 and KEY1 unchanged.
    /// </summary>
    AesSlotB = 1 << 1,

    /// <summary>
    /// Use AES slot C -> 0x0380F020 = 0x03FFC400 + 0x190 and KEY2.Y = 0x03FFC400 + 0x1A0.
    /// </summary>
    AesSlotC = 1 << 2,

    /// <summary>
    /// Allow access to SD Card device (I).
    /// </summary>
    SdCard = 1 << 3,

    /// <summary>
    /// Allow access to NAND (device A-H and KEY3 intact).
    /// </summary>
    NandAccess = 1 << 4,

    /// <summary>
    /// Game card power on.
    /// </summary>
    GameCardPowerOn = 1 << 5,

    /// <summary>
    /// Software uses shared2 file from storage SD/eMMC.
    /// </summary>
    Shared2File = 1 << 6,

    /// <summary>
    /// Sign the camera photo JPEG files for launcher (AES slot B).
    /// </summary>
    SignJpegForLauncher = 1 << 7,

    /// <summary>
    /// Game card nitro mode.
    /// </summary>
    GameCardNitroMode = 1 << 8,

    /// <summary>
    /// SSL client certificates (AES slot A) -> KEY0 = 0x03FFC600 + 0x30.
    /// </summary>
    SslClientCert = 1 << 9,

    /// <summary>
    /// Sign the camera photo JPEG files for the user (AES slot B).
    /// </summary>
    SignJpegForUser = 1 << 10,

    /// <summary>
    /// Allow read access for photos.
    /// </summary>
    PhotoReadAccess = 1 << 11,

    /// <summary>
    /// Allow write access for photos.
    /// </summary>
    PhotoWriteAccess = 1 << 12,

    /// <summary>
    /// Allow read access to the SD card.
    /// </summary>
    SdCardReadAccess = 1 << 13,

    /// <summary>
    /// Allow write access to the SD card.
    /// </summary>
    SdCardWriteAccess = 1 << 14,

    /// <summary>
    /// Read access to cartridge save files.
    /// </summary>
    GameCardSaveReadAccess = 1 << 15,

    /// <summary>
    /// Write access to cartridge save files.
    /// </summary>
    GameCardSaveWriteAccess = 1 << 16,

    /// <summary>
    /// Debugger common client key -> 0x0380F000 = 0x03FFC600 + 0x10.
    /// </summary>
    DebuggerCommonClientKey = 1 << 31,
}
