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
/// ROM features for DSi or new DS games.
/// </summary>
[Flags]
public enum DsiRomFeatures
{
    /// <summary>
    /// TSC touchscreen / sound controller for DSi.
    /// </summary>
    DsiTouchscreenSound = 1 << 0,

    /// <summary>
    /// Require to accept the EULA agreement.
    /// </summary>
    RequireEula = 1 << 1,

    /// <summary>
    /// Use in the launcher the icon from banner.sav instead of the ROM banner.
    /// </summary>
    SaveBannerIcon = 1 << 2,

    /// <summary>
    /// Show in the launcher the Wi-Fi connection icon.
    /// </summary>
    ShowWifiIcon = 1 << 3,

    /// <summary>
    ///  Show in the launcher the DS Wireless icon.
    /// </summary>
    ShowWirelessIcon = 1 << 4,

    /// <summary>
    /// ROM contains an HMAC of the icon.
    /// </summary>
    BannerHmac = 1 << 5,

    /// <summary>
    /// ROM contains an HMAC and RSA signature of the header.
    /// </summary>
    SignedHeader = 1 << 6,

    /// <summary>
    /// ROM is a developer application.
    /// </summary>
    DeveloperApp = 1 << 7,
}
