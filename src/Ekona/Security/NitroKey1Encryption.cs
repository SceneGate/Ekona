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
using System.IO;
using System.Linq;
using System.Text;
using Yarhl.IO;

namespace SceneGate.Ekona.Security;

/// <summary>
/// Encrypt and decrypt specific DS content with the KEY1 algorithm (blowfish).
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
    /// <param name="gameCode">The DS game code.</param>
    /// <param name="keyStore">The store with the DS keys.</param>
    /// <exception cref="ArgumentNullException">The arguments are null or .</exception>
    /// <exception cref="ArgumentException">The game code does not have length 4.</exception>
    public NitroKey1Encryption(string gameCode, DsiKeyStore keyStore)
    {
        this.gameCode = gameCode ?? throw new ArgumentNullException(nameof(gameCode));
        this.keyStore = keyStore ?? throw new ArgumentNullException(nameof(keyStore));

        if (gameCode.Length != 4)
            throw new ArgumentException("It must have length 4", nameof(gameCode));
        if (keyStore.BlowfishDsKey is null)
            throw new ArgumentNullException(nameof(keyStore), "The DS blowfish key is null");
    }

    /// <summary>
    /// Decrypt the DS header token and verify if its content that indicates the secure are is disabled.
    /// </summary>
    /// <param name="token">The encrypted token from the DS header.</param>
    /// <returns>A value indicating whether the secure area is disabled.</returns>
    /// <exception cref="ArgumentException">The token does not have the expected length 8.</exception>
    public bool HasDisabledSecureArea(byte[] token)
    {
        ArgumentNullException.ThrowIfNull(token);
        if (token.Length != 8)
            throw new ArgumentException("Length must be 8", nameof(token));

        var encryption = new NitroBlowfish();
        encryption.Initialize(gameCode, 1, 8, keyStore.BlowfishDsKey);

        byte[] decrypted = encryption.Decrypt(token);
        return Encoding.ASCII.GetString(decrypted) == DisableSecureAreaToken;
    }

    /// <summary>
    /// Generate an encrypted token for the DS header to indicate that the secure are should be disabled.
    /// </summary>
    /// <returns>The encrypted token.</returns>
    public byte[] GenerateEncryptedDisabledSecureAreaToken()
    {
        var encryption = new NitroBlowfish();
        encryption.Initialize(gameCode, 1, 8, keyStore.BlowfishDsKey);

        byte[] decrypted = Encoding.ASCII.GetBytes(DisableSecureAreaToken);
        return encryption.Encrypt(decrypted);
    }

    /// <summary>
    /// Decrypt the secure area ID and verify if its content match the expected result.
    /// </summary>
    /// <param name="data">The encrypted secure area ID.</param>
    /// <returns>A value indicating whether the encrypted secure area ID is valid.</returns>
    /// <exception cref="ArgumentException">The argument does not have the expected length 8.</exception>
    public bool HasValidSecureAreaId(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length != 8)
            throw new ArgumentException("Length must be 8", nameof(data));

        var encryption = new NitroBlowfish();
        encryption.Initialize(gameCode, 2, 8, keyStore.BlowfishDsKey);
        byte[] phase1 = encryption.Decrypt(data);

        encryption.Initialize(gameCode, 3, 8, keyStore.BlowfishDsKey);
        byte[] decrypted = encryption.Decrypt(phase1);

        return Encoding.ASCII.GetString(decrypted) == SecureAreaId;
    }

    /// <summary>
    /// Generate an encrypted secure area ID.
    /// </summary>
    /// <returns>The encrypted secure area ID.</returns>
    public byte[] GenerateEncryptedSecureAreaId()
    {
        byte[] data = Encoding.ASCII.GetBytes(SecureAreaId);

        var encryption = new NitroBlowfish();
        encryption.Initialize(gameCode, 3, 8, keyStore.BlowfishDsKey);
        byte[] phase2 = encryption.Encrypt(data);

        encryption.Initialize(gameCode, 2, 8, keyStore.BlowfishDsKey);
        return encryption.Encrypt(phase2);
    }

    /// <summary>
    /// Check if the ARM9 stream has an encrypted secure area.
    /// </summary>
    /// <param name="arm9">The ARM9 program to verify.</param>
    /// <returns>A value indicating whether the stream has an encrypted secure area.</returns>
    /// <exception cref="ArgumentException">The ARM9 is smaller than expected (2 KB).</exception>
    public bool HasEncryptedArm9(Stream arm9)
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

        if (HasValidSecureAreaId(secureAreaId)) {
            return true;
        }

        return false; // like secure area filled with 0x00 to avoid it.
    }

    /// <summary>
    /// Encrypt the secure area of the ARM9 program.
    /// </summary>
    /// <param name="arm9">The ARM9 program to encrypt its secure area.</param>
    /// <returns>A copy of the ARM9 program with the secure area encrypted.</returns>
    /// <exception cref="ArgumentException">The program is smaller than expected (2 KB).</exception>
    public DataStream EncryptArm9(Stream arm9)
    {
        ArgumentNullException.ThrowIfNull(arm9);
        if (arm9.Length < EncryptedSecureAreaLength)
            throw new ArgumentException("ARM9 must be at least 2 KB", nameof(arm9));

        var encryption = new NitroBlowfish();
        var output = new DataStream();

        using var inputWrapper = new DataStream(arm9);
        inputWrapper.WriteTo(output);

        // Only the first 2 KB of the secure area are encrypted.
        using var encryptedArea = new DataStream(output, 0, EncryptedSecureAreaLength);

        // Re-generate CRC
        var crc = new NitroCrcGenerator();
        using var crcDataSegment = new DataStream(encryptedArea, 0x10, EncryptedSecureAreaLength - 0x10);
        byte[] actualCrc = crc.GenerateCrc16(crcDataSegment);
        encryptedArea.Position = 0x0E;
        encryptedArea.Write(actualCrc);

        // Set the secure area ID (overwritten by BIOS so it's not in the game dumps).
        encryptedArea.Position = 0;
        encryptedArea.Write(Encoding.ASCII.GetBytes(SecureAreaId), 0, 8);

        // First pass encrypt the whole secure area
        encryption.Initialize(gameCode, 3, 8, keyStore.BlowfishDsKey);
        encryption.Encrypt(encryptedArea);

        // Second pass only re-encrypts the secure area ID
        encryption.Initialize(gameCode, 2, 8, keyStore.BlowfishDsKey);
        using var secureAreaIdStream = new DataStream(encryptedArea, 0, 8);
        encryption.Encrypt(secureAreaIdStream);

        return output;
    }

    /// <summary>
    /// Decrypt the secure area of the ARM9 program.
    /// </summary>
    /// <param name="arm9">The ARM9 program to decrypt its secure area.</param>
    /// <returns>A copy of the ARM9 program with the secure area decrypted.</returns>
    /// <exception cref="ArgumentException">The program is smaller than expected (2 KB).</exception>
    public DataStream DecryptArm9(Stream arm9)
    {
        ArgumentNullException.ThrowIfNull(arm9);
        if (arm9.Length < EncryptedSecureAreaLength)
            throw new ArgumentException("ARM9 must be at least 2 KB", nameof(arm9));

        var encryption = new NitroBlowfish();
        var output = new DataStream();

        using var inputWrapper = new DataStream(arm9);
        inputWrapper.WriteTo(output);

        // Only the first 2 KB of the secure area are encrypted.
        using var encryptedArea = new DataStream(output, 0, EncryptedSecureAreaLength);

        // First pass decrypt only secure area ID.
        encryption.Initialize(gameCode, 2, 8, keyStore.BlowfishDsKey);
        using var secureAreaIdStream = new DataStream(encryptedArea, 0, 8);
        encryption.Decrypt(secureAreaIdStream);

        // Second pass decrypt the whole secure area
        encryption.Initialize(gameCode, 3, 8, keyStore.BlowfishDsKey);
        encryption.Decrypt(encryptedArea);

        // Verify secure area ID
        byte[] secureAreaId = new byte[8];
        encryptedArea.Position = 0;
        encryptedArea.Read(secureAreaId);
        if (Encoding.ASCII.GetString(secureAreaId) != SecureAreaId) {
            throw new FormatException("ARM9 has an invalid secure area ID. Is it encrypted?");
        }

        // Destory secure area ID as it's expected (and dumper does).
        encryptedArea.Position = 0;
        encryptedArea.Write(DestroyedSecureId);

        // Verify checksum
        var crc = new NitroCrcGenerator();
        using var crcDataSegment = new DataStream(encryptedArea, 0x10, EncryptedSecureAreaLength - 0x10);
        byte[] actualCrc = crc.GenerateCrc16(crcDataSegment);

        byte[] expectedCrc = new byte[2];
        encryptedArea.Position = 0x0E;
        encryptedArea.Read(expectedCrc);
        if (!expectedCrc.SequenceEqual(actualCrc)) {
            throw new FormatException("Invalid CRC. Failed to decrypt ARM9");
        }

        return output;
    }
}
