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
using System.Data.HashFunction;
using System.Data.HashFunction.CRC;
using System.Linq;
using System.Security.Cryptography;
using Yarhl.IO;

namespace SceneGate.Ekona.Security;

/// <summary>
/// Internal method extensions.
/// </summary>
internal static class IOExtensions
{
    /// <summary>
    /// Read a CRC-16 value and verify it matches the region of data.
    /// </summary>
    /// <param name="reader">The data reader with the stream to verify.</param>
    /// <returns>The information about the checksum verification.</returns>
    public static HashInfo ReadCrc16(this DataReader reader)
    {
        return new HashInfo("CRC16-MODBUS", reader.ReadBytes(2));
    }

    /// <summary>
    /// Read a SHA-1 HMAC.
    /// </summary>
    /// <param name="reader">The data reader with the stream to read the HMAC.</param>
    /// <returns>The information about the HMAC verification.</returns>
    public static HashInfo ReadHmacSha1(this DataReader reader)
    {
        byte[] hmac = reader.ReadBytes(0x14);
        return new HashInfo("HMAC-SHA1", hmac);
    }

    /// <summary>
    /// Read a RSA-SHA1 signature.
    /// </summary>
    /// <param name="reader">The data reader with the stream to read the signature.</param>
    /// <returns>The information about the signature.</returns>
    public static HashInfo ReadSignatureSha1Rsa(this DataReader reader)
    {
        byte[] signature = reader.ReadBytes(128);
        return new HashInfo("RawRSA-SHA1", signature);
    }
}
