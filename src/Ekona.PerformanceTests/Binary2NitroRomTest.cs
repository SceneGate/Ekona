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
using SceneGate.Ekona.Containers.Rom;
using SceneGate.Ekona.Security;
using Yarhl.FileFormat;
using Yarhl.IO;

namespace SceneGate.Ekona.PerformanceTests;

public class Binary2NitroRomTest
{
    private BinaryFormat binaryRom = null!;
    private DsiKeyStore keyStore = null!;
    private NitroRom root = null!;
    private DataStream outputStream = null!;

    public static IEnumerable<FilePathInfo> RomPaths => ResourceManager.GetRoms();

    [ParamsAllValues]
    public bool UseKeys { get; set; }

    [ParamsSource(nameof(RomPaths))]
    public FilePathInfo RomPath { get; set; } = null!;

    [GlobalSetup]
    public void Setup()
    {
        keyStore = ResourceManager.GetDsiKeyStore();
        binaryRom = new BinaryFormat(DataStreamFactory.FromFile(RomPath.Path, FileOpenMode.Read));
        root = binaryRom.ConvertWith(new Binary2NitroRom());
        outputStream = new DataStream();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        root?.Dispose();
        binaryRom?.Dispose();
        outputStream?.Dispose();
    }

    [Benchmark]
    public NitroRom ReadRom()
    {
        var converter = UseKeys ? new Binary2NitroRom() : new Binary2NitroRom(keyStore);
        return converter.Convert(binaryRom);
    }

    [Benchmark]
    public BinaryFormat WriteRom()
    {
        DsiKeyStore? runKeys = UseKeys ? keyStore : null;
        var parameters = new NitroRom2BinaryParams {
            KeyStore = runKeys,
            OutputStream = outputStream,
        };

        var converter = new NitroRom2Binary(parameters);
        return converter.Convert(root);
    }
}
