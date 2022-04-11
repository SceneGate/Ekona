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
using System.IO;
using SceneGate.Ekona.Security;

namespace SceneGate.Ekona.Containers.Rom;

/// <summary>
/// Additional parameters for the <see cref="NitroRom2Binary"/> converter.
/// </summary>
public class NitroRom2BinaryParams
{
    /// <summary>
    /// Gets or sets the output stream for the converter. If not set, the
    /// converter will create a new one in-memory.
    /// </summary>
    public Stream OutputStream { get; set; }

    /// <summary>
    /// Gets or sets the key store to re-generate the HMAC hashes. If not set,
    /// the hashes are not regenerated.
    /// </summary>
    public DsiKeyStore KeyStore { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the program (arm9) is decompressed.
    /// </summary>
    public bool DecompressedProgram { get; set; }
}
