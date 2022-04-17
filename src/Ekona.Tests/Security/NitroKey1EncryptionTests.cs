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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SceneGate.Ekona.Containers.Rom;
using SceneGate.Ekona.Security;
using Yarhl.FileSystem;
using Yarhl.IO;

namespace SceneGate.Ekona.Tests.Security;

[TestFixture]
public class NitroKey1EncryptionTests
{
    public static IEnumerable<TestCaseData> GetRoms()
    {
        string basePath = Path.Combine(TestDataBase.RootFromOutputPath, "Containers");
        string listPath = Path.Combine(basePath, "rom.txt");
        return TestDataBase.ReadTestListFile(listPath)
            .Select(line => line.Split(','))
            .Select(data => new TestCaseData(Path.Combine(basePath, data[1]))
                .SetName($"{{m}}({data[1]})"));
    }

    [Test]
    public void GenerateDisabledSecureAreaMatch()
    {
        DsiKeyStore keys = TestDataBase.GetDsiKeyStore();
        var encryption = new NitroKey1Encryption("AAAA", keys);

        byte[] token = encryption.GenerateEncryptedDisabledSecureAreaToken();

        encryption.HasDisabledSecureArea(token).Should().BeTrue();
    }

    [Test]
    [TestCase(new byte[] { 0x1D, 0x9E, 0xBA, 0xC7, 0xBB, 0x0E, 0x9E, 0x6A }, "B2KJ")]
    public void DecryptSecureAreaId(byte[] encrypted, string idCode)
    {
        DsiKeyStore keys = TestDataBase.GetDsiKeyStore();

        var encryption = new NitroKey1Encryption(idCode, keys);
        encryption.HasValidSecureAreaId(encrypted).Should().BeTrue();
    }

    [Test]
    [TestCase(new byte[] { 0x1D, 0x9E, 0xBA, 0xC7, 0xBB, 0x0E, 0x9E, 0x6A }, "B2KJ")]
    public void EncryptSecureAreaId(byte[] encrypted, string idCode)
    {
        DsiKeyStore keys = TestDataBase.GetDsiKeyStore();
        var encryption = new NitroKey1Encryption(idCode, keys);

        byte[] actualEncrypted = encryption.GenerateEncryptedSecureAreaId();
        actualEncrypted.Should().BeEquivalentTo(encrypted);
    }

    [TestCaseSource(nameof(GetRoms))]
    public void EncryptedDecryptArm9IsIdentical(string romPath)
    {
        TestDataBase.IgnoreIfFileDoesNotExist(romPath);
        DsiKeyStore keys = TestDataBase.GetDsiKeyStore();

        using Node node = NodeFactory.FromFile(romPath, FileOpenMode.Read);
        node.TransformWith<Binary2NitroRom>();
        var nitroRom = node.GetFormatAs<NitroRom>();
        var originalArm9 = nitroRom.System.Children["arm9"].Stream!;

        var encryption = new NitroKey1Encryption(nitroRom.Information.GameCode, keys);
        using var encryptedArm9 = encryption.EncryptArm9(originalArm9);
        using var decryptedArm9 = encryption.DecryptArm9(encryptedArm9);

        decryptedArm9.Compare(originalArm9).Should().BeTrue();
    }

    [TestCaseSource(nameof(GetRoms))]
    public void GameSecureAreaAreDecrypted(string romPath)
    {
        TestDataBase.IgnoreIfFileDoesNotExist(romPath);
        DsiKeyStore keys = TestDataBase.GetDsiKeyStore();

        using Node node = NodeFactory.FromFile(romPath, FileOpenMode.Read);
        node.TransformWith<Binary2NitroRom>();
        var nitroRom = node.GetFormatAs<NitroRom>();
        var originalArm9 = nitroRom.System.Children["arm9"].Stream!;

        var encryption = new NitroKey1Encryption(nitroRom.Information.GameCode, keys);
        encryption.HasEncryptedArm9(originalArm9).Should().BeFalse();
    }
}
