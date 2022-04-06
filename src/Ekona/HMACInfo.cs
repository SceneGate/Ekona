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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Yarhl.IO;

namespace SceneGate.Ekona;

/// <summary>
/// Information for a cryptographic HMAC.
/// </summary>
[SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Follow BCL names")]
public class HMACInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HMACInfo"/>  class.
    /// </summary>
    /// <param name="hashName">The name of the hashing algorithm.</param>
    /// <param name="hash">The hash data.</param>
    public HMACInfo(string hashName, byte[] hash)
    {
        HashName = hashName;
        Hash = hash;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HMACInfo"/>  class.
    /// </summary>
    /// <param name="hashName">The name of the hashing algorithm.</param>
    /// <param name="hash">The hash data.</param>
    /// <param name="isValid">Value indicating whether this signature is valid.</param>
    public HMACInfo(string hashName, byte[] hash, bool isValid)
        : this(hashName, hash)
    {
        IsValid = isValid;
        Validated = true;
    }

    /// <summary>
    /// Gets the hashing algorithm name.
    /// </summary>
    public string HashName { get; }

    /// <summary>
    /// Gets the hash.
    /// </summary>
    public byte[] Hash { get; }

    /// <summary>
    /// Gets a value indicating whether this signature has been validated.
    /// </summary>
    public bool Validated { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this signature is valid.
    /// Use only if Validated is true.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Validate this HMAC against the provided data segment.
    /// </summary>
    /// <param name="stream">The stream to validate the data.</param>
    /// <param name="key">The key to calculate and verify the HMAC.</param>
    public void Validate(DataStream stream, byte[] key) => Validate(stream, 0, stream.Length, key);

    /// <summary>
    /// Validate this HMAC against the provided data segment.
    /// </summary>
    /// <param name="stream">The stream to validate the data.</param>
    /// <param name="offset">The offset of the region of the data where the HMAC applies.</param>
    /// <param name="length">The length of the region of data.</param>
    /// <param name="key">The key to calculate and verify the HMAC.</param>
    public void Validate(DataStream stream, long offset, long length, byte[] key)
    {
        using var segment = new DataStream(stream, offset, length);
        using var algo = System.Security.Cryptography.HMAC.Create(HashName);
        algo.Key = key;
        byte[] actualHash = algo.ComputeHash(segment);

        Validated = true;
        IsValid = Hash.SequenceEqual(actualHash);
    }
}
