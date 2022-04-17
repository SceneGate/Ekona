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
using BenchmarkDotNet.Attributes;
using SceneGate.Ekona.Security;
using Yarhl.IO;

namespace SceneGate.Ekona.PerformanceTests;

[MemoryDiagnoser]
public class NitroBlowfishTest
{
    private readonly NitroBlowfish blowfish = new NitroBlowfish();
    private readonly byte[] buffer = new byte[16 * 1024];
    private byte[] key = null!;

    public int Level => 3;

    public int Modulo => 8;

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random();
        key = new byte[NitroBlowfish.KeyLength];
        random.NextBytes(key);
    }

    [Benchmark]
    [Arguments(8)]
    [Arguments(2 * 1024)]
    public void EncryptArray(int dataLength)
    {
        var data = buffer[..dataLength];

        blowfish.Initialize("YYYY", Level, Modulo, key);
        blowfish.Encrypt(data);
    }

    [Benchmark]
    [Arguments(8)]
    [Arguments(2 * 1024)]
    public void EncryptStream(int dataLength)
    {
        using var stream = DataStreamFactory.FromArray(buffer, 0, dataLength);

        blowfish.Initialize("YYYY", Level, Modulo, key);
        blowfish.Encrypt(stream);
    }

    [Benchmark]
    public void Encrypt64Bits()
    {
        uint data0 = 0x89ABCDEF;
        uint data1 = 0x01234567;

        blowfish.Initialize("YYYY", Level, Modulo, key);
        blowfish.Encrypt(ref data0, ref data1);
    }

    [Benchmark]
    public void Initialization()
    {
        blowfish.Initialize("YYYY", Level, Modulo, key);
    }
}
