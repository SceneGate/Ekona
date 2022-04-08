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
using System.IO;
using Yarhl.IO;

namespace SceneGate.Ekona.Security;

/// <summary>
/// Generate CRC compatible with Nitro devices.
/// </summary>
public class NitroCrcGenerator
{
    /// <summary>
    /// Generate a CRC16-MODBUS of the data.
    /// </summary>
    /// <param name="stream">The data to read.</param>
    /// <returns>The CRC of the data in little endian.</returns>
    public byte[] GenerateCrc16(Stream stream) => GenerateCrc16(stream, 0, stream.Length);

    /// <summary>
    /// Generate a CRC16-MODBUS of the data.
    /// </summary>
    /// <param name="stream">The data to read.</param>
    /// <param name="offset">The offset to start reading data.</param>
    /// <param name="length">The length of the data.</param>
    /// <returns>The CRC of the data in little endian.</returns>
    public byte[] GenerateCrc16(Stream stream, long offset, long length)
    {
        using var segment = new DataStream(stream, offset, length);

        ICRC crc = CRCFactory.Instance.Create(CRCConfig.MODBUS);
        IHashValue hash = crc.ComputeHash(segment);
        return new byte[] { hash.Hash[0], hash.Hash[1] };
    }
}
