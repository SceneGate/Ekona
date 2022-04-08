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
using System.Linq;

namespace SceneGate.Ekona.Security;

/// <summary>
/// Information of a data hash.
/// </summary>
public class HashInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HashInfo"/>  class.
    /// </summary>
    /// <param name="algoName">The name of the hashing algorithm.</param>
    /// <param name="hash">The hash data.</param>
    public HashInfo(string algoName, byte[] hash)
    {
        AlgorithmName = algoName;
        Hash = hash;
        Status = HashStatus.NotValidated;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HashInfo"/>  class.
    /// </summary>
    /// <param name="algoName">The name of the hashing algorithm.</param>
    /// <param name="hash">The hash data.</param>
    /// <param name="isValid">Value indicating whether this signature is valid.</param>
    public HashInfo(string algoName, byte[] hash, bool isValid)
        : this(algoName, hash)
    {
        Status = isValid ? HashStatus.Valid : HashStatus.Invalid;
    }

    /// <summary>
    /// Gets the hashing algorithm name.
    /// </summary>
    public string AlgorithmName { get; }

    /// <summary>
    /// Gets the hash.
    /// </summary>
    public byte[] Hash { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the hash is null.
    /// </summary>
    public bool IsNull => Hash.All(x => x == 0);

    /// <summary>
    /// Gets or sets the status of the hash.
    /// </summary>
    public HashStatus Status { get; set; }

    /// <summary>
    /// Validate this hash against the provided one.
    /// </summary>
    /// <param name="actualHash">The hash to compare.</param>
    public void Validate(byte[] actualHash) =>
        Status = Hash.SequenceEqual(actualHash) ? HashStatus.Valid : HashStatus.Invalid;

    /// <summary>
    /// Change the hash, like after computing it again.
    /// </summary>
    /// <param name="newHash">The new hash.</param>
    public void ChangeHash(byte[] newHash)
    {
        Hash = newHash;
        Status = HashStatus.Valid;
    }
}
