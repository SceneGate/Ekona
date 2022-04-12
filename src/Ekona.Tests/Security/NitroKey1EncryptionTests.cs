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
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SceneGate.Ekona.Security;

namespace SceneGate.Ekona.Tests.Security;

[TestFixture]
public class NitroKey1EncryptionTests
{
    [Test]
    [TestCase(new byte[] { 0x1D, 0x9E, 0xBA, 0xC7, 0xBB, 0x0E, 0x9E, 0x6A }, "B2KJ")]
    public void DecryptSecureAreaId(byte[] encrypted, string idCode)
    {
        DsiKeyStore keys = TestDataBase.GetDsiKeyStore();

        byte[] decrypted = NitroKey1Encryption.DecryptSecureAreaId(encrypted, idCode, keys);

        string actualToken = Encoding.ASCII.GetString(decrypted);
        actualToken.Should().BeEquivalentTo("encryObj");
    }

    [Test]
    [TestCase(new byte[] { 0x1D, 0x9E, 0xBA, 0xC7, 0xBB, 0x0E, 0x9E, 0x6A }, "B2KJ")]
    public void EncryptSecureAreaId(byte[] encrypted, string idCode)
    {
        DsiKeyStore keys = TestDataBase.GetDsiKeyStore();

        byte[] token = Encoding.ASCII.GetBytes("encryObj");
        byte[] actualEncrypted = NitroKey1Encryption.EncryptSecureAreaId(token, idCode, keys);

        actualEncrypted.Should().BeEquivalentTo(encrypted);
    }
}
