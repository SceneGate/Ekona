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
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Yarhl.IO;

namespace SceneGate.Ekona.Security;

/// <summary>
/// Generate and verify Twilight HMACs (SHA-1 HMAC).
/// </summary>
public class TwilightHMacGenerator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TwilightHMacGenerator"/>  class.
    /// </summary>
    /// <param name="key">The key to generate HMACs.</param>
    public TwilightHMacGenerator(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);
        Key = key;
    }

    /// <summary>
    /// Gets the HMAC key.
    /// </summary>
    public byte[] Key { get; }

    /// <summary>
    /// Generate an HMAC from the provided data.
    /// </summary>
    /// <param name="stream">The data to generate the HMAC.</param>
    /// <returns>The new HMAC.</returns>
    public byte[] Generate(Stream stream) => Generate(stream, 0, stream.Length);

    /// <summary>
    /// Generate an HMAC from the provided data segment.
    /// </summary>
    /// <param name="stream">The data to generate the HMAC.</param>
    /// <param name="offset">The offset to the data to read.</param>
    /// <param name="length">The length of the data to read.</param>
    /// <returns>The new HMAC.</returns>
    public byte[] Generate(Stream stream, long offset, long length)
    {
        using var segment = new DataStream(stream, offset, length);
        using var algo = HMAC.Create("HMACSHA1");
        algo.Key = Key;
        return algo.ComputeHash(segment);
    }
}
