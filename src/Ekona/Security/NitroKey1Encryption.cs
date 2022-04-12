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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SceneGate.Ekona.Containers.Rom;
using Yarhl.IO;

namespace SceneGate.Ekona.Security;

/// <summary>
/// Encrypt and decrypt with the DS KEY1 algorithm.
/// </summary>
public class NitroKey1Encryption
{
    private const int EncryptedSecureAreaLength = 2 * 1024;
    private static readonly byte[] DestroyedSecureId = { 0xFF, 0xDE, 0xFF, 0xE7, 0xFF, 0xDE, 0xFF, 0xE7 };
    private readonly byte[] keyBuffer = new byte[0x1048];

    public static byte[] DecryptSecureAreaId(byte[] data, string gameCode, DsiKeyStore keys)
    {
        var encryption = new NitroKey1Encryption();
        encryption.Initialize(gameCode, 2, 8, keys.BlowfishDsKey);
        byte[] phase1 = encryption.Encryption(false, data);

        encryption.Initialize(gameCode, 3, 8, keys.BlowfishDsKey);
        return encryption.Encryption(false, phase1);
    }

    public static byte[] EncryptSecureAreaId(byte[] data, string gameCode, DsiKeyStore keys)
    {
        var encryption = new NitroKey1Encryption();
        encryption.Initialize(gameCode, 3, 8, keys.BlowfishDsKey);
        byte[] phase2 = encryption.Encryption(true, data);

        encryption.Initialize(gameCode, 2, 8, keys.BlowfishDsKey);
        return encryption.Encryption(true, phase2);
    }

    public static bool HasEncryptedArm9(DataStream arm9, ProgramInfo info, DsiKeyStore keys)
    {
        byte[] secureAreaId = new byte[8];
        arm9.Position = 0;
        arm9.Read(secureAreaId);
        if (secureAreaId.SequenceEqual(DestroyedSecureId)) {
            return false;
        }

        byte[] decryptedSecureId = DecryptSecureAreaId(secureAreaId, info.GameCode, keys);
        if (Encoding.ASCII.GetString(decryptedSecureId) == "encryObj") {
            return true;
        }

        throw new FormatException("Unknown AMR9 format");
    }

    public static DataStream EncryptArm9(DataStream arm9, string gameCode, DsiKeyStore keys)
    {
        ArgumentNullException.ThrowIfNull(arm9);
        ArgumentNullException.ThrowIfNull(keys);
        ArgumentNullException.ThrowIfNull(gameCode);
        if (gameCode.Length != 4)
            throw new ArgumentException("It must have length 4", nameof(gameCode));
        if (arm9.Length < EncryptedSecureAreaLength)
            throw new ArgumentException("ARM9 must be at least 2 KB", nameof(arm9));

        var encryption = new NitroKey1Encryption();
        var output = new DataStream();
        arm9.WriteTo(output);

        // Only the first 2 KB of the secure area are encrypted.
        using var encryptedArea = new DataStream(output, 0, EncryptedSecureAreaLength);

        // Set the secure area ID (overwritten by BIOS so it's not in the game dumps).
        encryptedArea.Position = 0;
        encryptedArea.Write(Encoding.ASCII.GetBytes("encryObj"), 0, 8);

        // First pass encrypt the whole area
        encryption.Initialize(gameCode, 3, 8, keys.BlowfishDsKey);
        encryption.Encryption(true, encryptedArea);

        // Second pass only re-encrypts the secure area ID
        encryption.Initialize(gameCode, 2, 8, keys.BlowfishDsKey);
        using var secureAreaIdStream = new DataStream(encryptedArea, 0, 8);
        encryption.Encryption(true, secureAreaIdStream);

        return output;
    }

    private void Initialize(string idCodeText, int level, int modulo, byte[] key)
    {
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

    // public DataStream DecryptGameCart(DataStream arm9, ProgramInfo programInfo)
    // {
    //     byte[] dsKey = keys.BlowfishDsKey;
    //
    //     arm9.Position = 0;
    //     var output = new DataStream();

        // Level 1: decrypt header "secure area disable" token.
        // InitKeyCode(programInfo.GameCode, 1, 8, dsKey);
        // ulong disableToken = BitConverter.ToUInt64(Encoding.ASCII.GetBytes("NmMdOnly"));
        // var outSecureDisable = Encryption64Bits(disableToken, true);
        // var inSecureDisable = Encryption64Bits(outSecureDisable, false);

        // byte[] secureAreaInfo = new byte[8];
        // arm9.Read(secureAreaInfo);
        // secureAreaInfo = System.Text.Encoding.ASCII.GetBytes("encryObj");

        // Level 2: decrypt secure area info token (and encrypt cartridge commands)
        // InitKeyCode(programInfo.GameCode, 2, 8, dsKey);

        // byte[] test1 = { 0x1D, 0x9E, 0xBA, 0xC7, 0xBB, 0x0E, 0x9E, 0x6A };
        // byte[] decrypted = Encryption64Bits(test1, false);
        // var text = Encoding.ASCII.GetString(decrypted);
        // byte[] test2 = Encryption64Bits(decrypted, true);

        // var outObj = Encryption64Bits(BitConverter.ToUInt64(secureAreaInfo), true);
        // var outObj2 = Encryption64Bits(outObj, false);
        // var t = Encoding.ASCII.GetString(BitConverter.GetBytes(outObj2));

        // Level 3: decrypt full secure area
        // InitKeyCode(programInfo.GameCode, 3, 8, dsKey);

        // arm9.Position = 0;
        // using var secureAreaStream = new DataStream(arm9, 0, 2 * 1024);
        // EncryptionStream(secureAreaStream, output, false);
        //
        // using var plainArm9 = new DataStream(arm9, 2 * 1024, arm9.Length - 2 * 1024);
        // plainArm9.WriteTo(output);

        // Level 1 with DSi key would encrypt cartridge commands.
    //     return output;
    // }

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

    private void Encryption(bool encrypt, ref uint data0, ref uint data1)
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

    private void Encryption(bool encrypt, DataStream stream)
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

    private byte[] Encryption(bool encrypt, byte[] data)
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

    private uint GetKeyBuffer(int pos) => GetUInt32(keyBuffer, pos);

    private uint GetKeyBuffer(uint pos) => GetKeyBuffer((int)pos);

    private void SetUInt32(byte[] buffer, int pos, uint value)
    {
        byte[] data = BitConverter.GetBytes(value);
        Array.Copy(data, 0, buffer, pos, data.Length);
    }

    private uint GetUInt32(byte[] buffer, int pos) => BitConverter.ToUInt32(buffer, pos);
}
