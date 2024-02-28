namespace SceneGate.Ekona.PerformanceTests;

using BenchmarkDotNet.Attributes;
using SceneGate.Ekona.Compression;

[MemoryDiagnoser]
public class LzssEncoderTests
{
    private Stream inputStream = null!;
    private Stream outputStream = null!;

    [GlobalSetup]
    public void SetUp()
    {
        var input = new byte[Length];
        Random.Shared.NextBytes(input);
        inputStream = new MemoryStream(input);

        var output = new byte[Length * 2];
        outputStream = new MemoryStream(output);
    }

    [Params(512, 10 * 1024, 3 * 1024 * 1024)]
    public int Length { get; set; }

    [Benchmark]
    public Stream Encode()
    {
        outputStream.Position = 0;
        var encoder = new LzssEncoder(outputStream);
        return encoder.Convert(inputStream);
    }
}
