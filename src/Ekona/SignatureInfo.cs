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
using System.Linq;
using System.Security.Cryptography;
using Yarhl.IO;

namespace SceneGate.Ekona;

/// <summary>
/// Information of cryptographic RSA signatures.
/// </summary>
public class SignatureInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SignatureInfo"/> class.
    /// </summary>
    /// <param name="hashName">The name of the hashing algorithm.</param>
    /// <param name="paddingScheme">The scheme for the signature padding.</param>
    /// <param name="signature">The signature data.</param>
    public SignatureInfo(HashAlgorithmName hashName, RSASignaturePadding paddingScheme, byte[] signature)
    {
        HashName = hashName;
        PaddingScheme = paddingScheme;
        Signature = signature;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SignatureInfo"/> class.
    /// </summary>
    /// <param name="hashName">The name of the hashing algorithm.</param>
    /// <param name="paddingScheme">The scheme for the signature padding.</param>
    /// <param name="signature">The signature data.</param>
    /// <param name="isValid">Value indicating whether this signature is valid.</param>
    public SignatureInfo(HashAlgorithmName hashName, RSASignaturePadding paddingScheme, byte[] signature, bool isValid)
        : this(hashName, paddingScheme, signature)
    {
        IsValid = isValid;
        Validated = true;
    }

    /// <summary>
    /// Gets the hashing algorithm name.
    /// </summary>
    public HashAlgorithmName HashName { get; }

    /// <summary>
    /// Gets the padding scheme.
    /// </summary>
    public RSASignaturePadding PaddingScheme { get; }

    /// <summary>
    /// Gets the signature data.
    /// </summary>
    public byte[] Signature { get; }

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
    /// Gets the RSA keys for the signature.
    /// </summary>
    public RSA Keys { get; private set; }

    /// <summary>
    /// Validate this signature from a stream data.
    /// </summary>
    /// <param name="stream">Stream of the data.</param>
    /// <param name="offset">Offset to the start of signature data.</param>
    /// <param name="length">Length of the data to sign.</param>
    /// <param name="keys">Keys to use to validate the signature.</param>
    public void Validate(DataStream stream, long offset, long length, RSA keys)
    {
        Keys = keys;
        using var segment = new DataStream(stream, offset, length);

        IsValid = Keys.VerifyData(segment, Signature, HashName, PaddingScheme);
        Validated = true;
    }
}
