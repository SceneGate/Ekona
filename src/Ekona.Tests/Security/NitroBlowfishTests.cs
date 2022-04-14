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
using FluentAssertions;
using NUnit.Framework;
using SceneGate.Ekona.Security;
using Yarhl.IO;

namespace SceneGate.Ekona.Tests.Security;

[TestFixture]
public class NitroBlowfishTest
{
    [Test]
    [TestCase(new uint[] { 0x01234567, 0x89ABCDEF }, "AAAA", 2, 8, new uint[] { 0xECD83DAE, 0xAB3EF361 })]
    public void Decrypt(uint[] data, string idCode, int level, int modulo, uint[] expected)
    {
        DsiKeyStore keys = TestDataBase.GetDsiKeyStore();
        var blowfish = new NitroBlowfish();
        blowfish.Initialize(idCode, level, modulo, keys.BlowfishDsKey);

        blowfish.Decrypt(ref data[0], ref data[1]);
        data.Should().BeEquivalentTo(expected);
    }

    [Test]
    [TestCase(new uint[] { 0xECD83DAE, 0xAB3EF361 }, "AAAA", 2, 8, new uint[] { 0x01234567, 0x89ABCDEF })]
    public void Encrypt(uint[] data, string idCode, int level, int modulo, uint[] expected)
    {
        DsiKeyStore keys = TestDataBase.GetDsiKeyStore();
        var blowfish = new NitroBlowfish();
        blowfish.Initialize(idCode, level, modulo, keys.BlowfishDsKey);

        blowfish.Encrypt(ref data[0], ref data[1]);
        data.Should().BeEquivalentTo(expected);
    }

    [Test]
    public void StreamEncDecryption()
    {
        using var stream = new DataStream();
        var inputWriter = new DataWriter(stream);
        inputWriter.Write(0xECD83DAE);
        inputWriter.Write(0xAB3EF361);

        DsiKeyStore keys = TestDataBase.GetDsiKeyStore();
        var blowfish = new NitroBlowfish();
        blowfish.Initialize("AAAA", 2, 8, keys.BlowfishDsKey);

        blowfish.Encryption(true, stream);

        stream.Position = 0;
        var outputReader = new DataReader(stream);
        outputReader.ReadUInt32().Should().Be(0x01234567);
        outputReader.ReadUInt32().Should().Be(0x89ABCDEF);

        blowfish.Encryption(false, stream);

        stream.Position = 0;
        outputReader.ReadUInt32().Should().Be(0xECD83DAE);
        outputReader.ReadUInt32().Should().Be(0xAB3EF361);
    }

    [Test]
    public void ArrayEncDecryption()
    {
        using var inputStream = new DataStream();
        var inputWriter = new DataWriter(inputStream);
        inputWriter.Write(0xECD83DAE);
        inputWriter.Write(0xAB3EF361);
        byte[] input = new byte[8];
        inputStream.Position = 0;
        inputStream.Read(input);

        DsiKeyStore keys = TestDataBase.GetDsiKeyStore();
        var blowfish = new NitroBlowfish();
        blowfish.Initialize("AAAA", 2, 8, keys.BlowfishDsKey);

        byte[] output = blowfish.Encryption(true, input);

        using var outputStream = DataStreamFactory.FromArray(output);
        var outputReader = new DataReader(outputStream);
        outputReader.ReadUInt32().Should().Be(0x01234567);
        outputReader.ReadUInt32().Should().Be(0x89ABCDEF);

        byte[] finalEncryption = blowfish.Encryption(false, output);

        finalEncryption.Should().BeEquivalentTo(input);
    }
}
