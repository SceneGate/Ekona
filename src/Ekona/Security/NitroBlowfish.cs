// Copyright (c) 2022 SceneGate

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
#nullable enable
using System;
using System.Text;
using Yarhl.IO;

namespace SceneGate.Ekona.Security;

/// <summary>
/// Encrypt and decrypt with the blowfish based algorithm used in DS.
/// </summary>
public class NitroBlowfish
{
    private readonly byte[] keyBuffer = new byte[KeyLength];

    /// <summary>
    /// Gets the required length of the key.
    /// </summary>
    public static int KeyLength => 0x1048;

    /// <summary>
    /// Initialize the algorithm.
    /// </summary>
    /// <param name="idCodeText">The ID code to initialize the key.</param>
    /// <param name="level">The level of key initialization.</param>
    /// <param name="modulo">The modulo to initialize the key.</param>
    /// <param name="key">The key data of 0x1048 bytes.</param>
    public void Initialize(string idCodeText, int level, int modulo, byte[] key)
    {
        ArgumentNullException.ThrowIfNull(idCodeText);
        ArgumentNullException.ThrowIfNull(key);
        if (idCodeText.Length != 4)
            throw new ArgumentException("Length must be 4", nameof(idCodeText));
        if (key.Length != KeyLength)
            throw new ArgumentException($"Length must be {KeyLength}", nameof(key));

        Array.Copy(key, keyBuffer, key.Length);
        uint[] keyCode = new uint[3];

        byte[] idCodeBin = Encoding.ASCII.GetBytes(idCodeText);
        uint idCode = BitConverter.ToUInt32(idCodeBin);
        keyCode[0] = idCode;
        keyCode[1] = idCode >> 1;
        keyCode[2] = idCode << 1;

        if (level >= 1) {
            ApplyKeyCode(modulo, keyCode);
        }

        if (level >= 2) {
            ApplyKeyCode(modulo, keyCode);
        }

        keyCode[1] <<= 1;
        keyCode[2] >>= 1;

        if (level >= 3) {
            ApplyKeyCode(modulo, keyCode);
        }
    }

    /// <summary>
    /// Encrypt or decrypt 64-bits.
    /// </summary>
    /// <param name="encrypt">If set to `true`, the data is encrypted, otherwise it's decrypted.</param>
    /// <param name="data0">The first pair of 32-bits to (d)encrypt.</param>
    /// <param name="data1">The second pair of 32-bits to (d)encrypt.</param>
    public void Encryption(bool encrypt, ref uint data0, ref uint data1)
    {
        uint y = data0;
        uint x = data1;

        int iStart = encrypt ? 0 : 0x11;
        int iEnd = encrypt ? 0x0F : 0x02;
        int iDiff = encrypt ? 1 : -1;
        for (int i = iStart; i != iEnd + iDiff; i += iDiff) {
            uint z = GetKeyBuffer(i * 4) ^ x;
            x = GetKeyBuffer(0x048 + (((z >> 24) & 0xFF) * 4));
            x += GetKeyBuffer(0x448 + (((z >> 16) & 0xFF) * 4));
            x ^= GetKeyBuffer(0x848 + (((z >> 8) & 0xFF) * 4));
            x += GetKeyBuffer(0xC48 + (((z >> 0) & 0xFF) * 4));
            x ^= y;
            y = z;
        }

        uint xorX = GetKeyBuffer(encrypt ? 0x40 : 0x04);
        data0 = x ^ xorX;

        uint xorY = GetKeyBuffer(encrypt ? 0x44 : 0x00);
        data1 = y ^ xorY;
    }

    public void Encryption(bool encrypt, DataStream stream)
    {
        var reader = new DataReader(stream);
        var writer = new DataWriter(stream);

        stream.Position = 0;
        while (stream.Position + 8 <= stream.Length) {
            uint data0 = reader.ReadUInt32();
            uint data1 = reader.ReadUInt32();

            Encryption(encrypt, ref data0, ref data1);

            stream.Position -= 8;
            writer.Write(data0);
            writer.Write(data1);
        }
    }

    public byte[] Encryption(bool encrypt, byte[] data)
    {
        byte[] output = new byte[data.Length];

        using DataStream inputStream = DataStreamFactory.FromArray(data);
        var reader = new DataReader(inputStream);

        for (int i = 0; i + 8 <= data.Length; i += 8) {
            uint data0 = reader.ReadUInt32();
            uint data1 = reader.ReadUInt32();

            Encryption(encrypt, ref data0, ref data1);

            Array.Copy(BitConverter.GetBytes(data0), 0, output, i, 4);
            Array.Copy(BitConverter.GetBytes(data1), 0, output, i + 4, 4);
        }

        return output;
    }

    private void ApplyKeyCode(int modulo, uint[] keyCode)
    {
        uint GetMixValue(uint[] array, int bytePos)
        {
            uint val = 0;
            for (int i = 3; i >= 0; i--) {
                val <<= 8;

                int pos = bytePos + i;
                int bPos = pos / 4;
                int bitPos = (pos % 4) * 8;
                val |= (byte)(array[bPos] >> (24 - bitPos));
            }

            return val;
        }

        Encryption(true, ref keyCode[1], ref keyCode[2]);
        Encryption(true, ref keyCode[0], ref keyCode[1]);

        for (int i = 0; i <= 0x44; i += 4) {
            uint xorValue = GetMixValue(keyCode, i % modulo);
            uint value = GetUInt32(keyBuffer, i);
            SetUInt32(keyBuffer, i, value ^ xorValue);
        }

        uint scratch0 = 0;
        uint scratch1 = 0;
        for (int i = 0; i <= 0x1040; i += 8) {
            Encryption(true, ref scratch0, ref scratch1);
            SetUInt32(keyBuffer, i, scratch1);
            SetUInt32(keyBuffer, i + 4, scratch0);
        }
    }

    private uint GetKeyBuffer(int pos) => GetUInt32(keyBuffer, pos);

    private uint GetKeyBuffer(uint pos) => GetKeyBuffer((int)pos);

    private void SetUInt32(byte[] buffer, int pos, uint value)
    {
        byte[] data = BitConverter.GetBytes(value);
        Array.Copy(data, 0, buffer, pos, data.Length);
    }

    private uint GetUInt32(byte[] buffer, int pos) => BitConverter.ToUInt32(buffer, pos);
}
