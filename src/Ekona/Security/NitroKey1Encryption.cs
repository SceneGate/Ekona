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
    private const string DisableSecureAreaToken = "NmMdOnly";
    private const string SecureAreaId = "encryObj";
    private const int EncryptedSecureAreaLength = 2 * 1024;
    private static readonly byte[] DestroyedSecureId = { 0xFF, 0xDE, 0xFF, 0xE7, 0xFF, 0xDE, 0xFF, 0xE7 };

    private readonly string gameCode;
    private readonly DsiKeyStore keyStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="NitroKey1Encryption" /> class.
    /// </summary>
    /// <param name="gameCode"></param>
    /// <param name="keyStore"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public NitroKey1Encryption(string gameCode, DsiKeyStore keyStore)
    {
        this.gameCode = gameCode ?? throw new ArgumentNullException(nameof(gameCode));
        this.keyStore = keyStore ?? throw new ArgumentNullException(nameof(keyStore));

        if (gameCode.Length != 4)
            throw new ArgumentException("It must have length 4", nameof(gameCode));
    }

    public bool HasDisabledSecureArea(byte[] token)
    {
        ArgumentNullException.ThrowIfNull(token);
        if (token.Length != 8)
            throw new ArgumentException("Length must be 8", nameof(token));

        var encryption = new NitroBlowfish();
        encryption.Initialize(gameCode, 1, 8, keyStore.BlowfishDsKey);

        byte[] decrypted = encryption.Encryption(false, token);
        return Encoding.ASCII.GetString(decrypted) == DisableSecureAreaToken;
    }

    public byte[] GenerateEncryptedDisabledSecureAreaToken()
    {
        var encryption = new NitroBlowfish();
        encryption.Initialize(gameCode, 1, 8, keyStore.BlowfishDsKey);

        byte[] decrypted = Encoding.ASCII.GetBytes(DisableSecureAreaToken);
        return encryption.Encryption(true, decrypted);
    }

    public byte[] DecryptSecureAreaId(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length != 8)
            throw new ArgumentException("Length must be 8", nameof(data));

        var encryption = new NitroBlowfish();
        encryption.Initialize(gameCode, 2, 8, keyStore.BlowfishDsKey);
        byte[] phase1 = encryption.Encryption(false, data);

        encryption.Initialize(gameCode, 3, 8, keyStore.BlowfishDsKey);
        return encryption.Encryption(false, phase1);
    }

    public byte[] EncryptSecureAreaId(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length != 8)
            throw new ArgumentException("Length must be 8", nameof(data));

        var encryption = new NitroBlowfish();
        encryption.Initialize(gameCode, 3, 8, keyStore.BlowfishDsKey);
        byte[] phase2 = encryption.Encryption(true, data);

        encryption.Initialize(gameCode, 2, 8, keyStore.BlowfishDsKey);
        return encryption.Encryption(true, phase2);
    }

    public bool HasEncryptedArm9(DataStream arm9)
    {
        ArgumentNullException.ThrowIfNull(arm9);
        if (arm9.Length < EncryptedSecureAreaLength)
            throw new ArgumentException("ARM9 must be at least 2 KB", nameof(arm9));

        byte[] secureAreaId = new byte[8];
        arm9.Position = 0;
        arm9.Read(secureAreaId);
        if (secureAreaId.SequenceEqual(DestroyedSecureId)) {
            return false;
        }

        byte[] decryptedSecureId = DecryptSecureAreaId(secureAreaId);
        if (Encoding.ASCII.GetString(decryptedSecureId) == SecureAreaId) {
            return true;
        }

        return false; // like secure area filled with 0x00 to avoid it.
    }

    public DataStream EncryptArm9(DataStream arm9)
    {
        ArgumentNullException.ThrowIfNull(arm9);
        if (arm9.Length < EncryptedSecureAreaLength)
            throw new ArgumentException("ARM9 must be at least 2 KB", nameof(arm9));

        var encryption = new NitroBlowfish();
        var output = new DataStream();
        arm9.WriteTo(output);

        // Only the first 2 KB of the secure area are encrypted.
        using var encryptedArea = new DataStream(output, 0, EncryptedSecureAreaLength);

        // Set the secure area ID (overwritten by BIOS so it's not in the game dumps).
        encryptedArea.Position = 0;
        encryptedArea.Write(Encoding.ASCII.GetBytes(SecureAreaId), 0, 8);

        // First pass encrypt the whole area
        encryption.Initialize(gameCode, 3, 8, keyStore.BlowfishDsKey);
        encryption.Encryption(true, encryptedArea);

        // Second pass only re-encrypts the secure area ID
        encryption.Initialize(gameCode, 2, 8, keyStore.BlowfishDsKey);
        using var secureAreaIdStream = new DataStream(encryptedArea, 0, 8);
        encryption.Encryption(true, secureAreaIdStream);

        return output;
    }
}
