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
namespace SceneGate.Ekona.Security;

/// <summary>
/// Store of DSi (and new DS games) keys.
/// </summary>
public class DsiKeyStore
{
    /// <summary>
    /// Gets or sets the key for the Blowfish (KEY1) encryption of DS mode.
    /// </summary>
    /// <remarks>
    /// The key can be found in the DS ARM7 BIOS from 0x30 to 0x1077.
    /// It starts with `99 D5 20 5F` and has 0x40 bytes.
    /// </remarks>
    public byte[] BlowfishDsKey { get; set; }

    /// <summary>
    /// Gets or sets the HMAC key for the DS whitelist phases 1 and 2.
    /// </summary>
    /// <remarks>
    /// The key can be found in the DSi launcher application ARM9.
    /// For instance, at position 0270EC90h of the RAM.
    /// It starts with `61 BD DD 72` and has 0x40 bytes.
    /// </remarks>
    public byte[] HMacKeyWhitelist12 { get; set; }

    /// <summary>
    /// Gets or sets the HMAC key for the DS whitelist phases 3 and 4.
    /// </summary>
    /// <remarks>
    /// The key can be found in the DSi launcher application ARM9.
    /// For instance, at position 0270ECD0h of the RAM.
    /// It starts with `85 29 48 F3` and has 0x40 bytes.
    /// </remarks>
    public byte[] HMacKeyWhitelist34 { get; set; }

    /// <summary>
    /// Gets or sets the HMAC key used in DSi games (like banner HMAC) but also
    /// in some DS games to verify the (compressed) overlays in download play load mode.
    /// </summary>
    /// <remarks>
    /// The key can be found inside the ARM9 of most DS and DSi games and in the launcher.
    /// It has 0x40 bytes and starts with `21 06 C0 DE BA 98`.
    /// It seems to start with the "nitrocode" token and it would be the second one in the ARM9.
    /// </remarks>
    public byte[] HMacKeyDSiGames { get; set; }

    /// <summary>
    /// Gets or sets the modulus of the RSA public key used to sign DSi retail games.
    /// </summary>
    /// <remarks>
    /// The data can be found in the ARM9 BIOS of the DSi at position 0x8974.
    /// It starts with `95 6F 79 0D` and has 0x80 bytes.
    /// </remarks>
    public byte[] PublicModulusRetailGames { get; set; }
}
