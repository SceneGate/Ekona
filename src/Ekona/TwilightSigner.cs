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
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Yarhl.IO;

namespace SceneGate.Ekona
{
    /// <summary>
    /// Signer for DSi data.
    /// </summary>
    public class TwilightSigner
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TwilightSigner"/> class.
        /// </summary>
        /// <param name="publicModulus">The modulus of the public key.</param>
        public TwilightSigner(byte[] publicModulus)
        {
            // In DSi always constant: 65537
            Exponent = new byte[] { 0x01, 0x00, 0x01 };

            // prepend 0 to avoid treating as negative number
            PublicModulus = new byte[] { 0 }.Concat(publicModulus).ToArray();
        }

        /// <summary>
        /// Gets the exponent part of the key.
        /// </summary>
        public byte[] Exponent { get; }

        /// <summary>
        /// Gets the modulus part of the public key.
        /// </summary>
        public byte[] PublicModulus { get; }

        /// <summary>
        /// Verify a signature.
        /// </summary>
        /// <param name="signature">Signature to verify.</param>
        /// <param name="data">Data to verify the signature against.</param>
        /// <returns>The vailidity of the signature.</returns>
        public HashStatus VerifySignature(byte[] signature, Stream data) =>
            VerifySignature(signature, data, 0, data.Length);

        /// <summary>
        /// Verify a signature.
        /// </summary>
        /// <param name="signature">Signature to verify.</param>
        /// <param name="data">Data to verify the signature against.</param>
        /// <param name="offset">Offset to the data to verify the signature.</param>
        /// <param name="length">Length of the data to verify.</param>
        /// <returns>The validity of the signature.</returns>
        public HashStatus VerifySignature(byte[] signature, Stream data, long offset, long length)
        {
            var exponent = new Org.BouncyCastle.Math.BigInteger(Exponent);
            var modulus = new Org.BouncyCastle.Math.BigInteger(PublicModulus);
            AsymmetricKeyParameter pubKey = new RsaKeyParameters(false, modulus, exponent);

            // We can't use standard libraries or methods because the signature
            // is the raw hash (with PKCS #7 1.5 padding). It is NOT encoded with ASN.1
            // as specified in PKCS #1.
            // That's why we do manually comparison:
            // 1. Decript signature with RSA public key
            var rsaCipher = CipherUtilities.GetCipher("RSA/NONE/PKCS1Padding");
            rsaCipher.Init(false, pubKey);
            byte[] expectedHash = rsaCipher.DoFinal(signature);

            // 2. Calculate actual SHA-1 hash
            using var segment = new DataStream(data, offset, length);
            using var sha1 = SHA1.Create();
            byte[] actualHash = sha1.ComputeHash(segment);

            // 3. Must be equal expected and actual.
            return actualHash.SequenceEqual(expectedHash) ? HashStatus.Valid : HashStatus.Invalid;
        }
    }
}
