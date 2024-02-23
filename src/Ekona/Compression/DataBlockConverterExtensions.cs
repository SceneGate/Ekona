namespace SceneGate.Ekona.Compression;

using System;
using System.Buffers;
using System.IO;

/// <summary>
/// Extension methods for easily convert with <see cref="IDataBlockConverter{TSrc,TDst}"/>.
/// </summary>
public static class DataBlockConverterExtensions
{
    private const int ReadBufferLength = 9 * 1024; // so a 88% of compression rate (LZSS) is below 80 kB (<LOH)

    /// <summary>
    /// Converts the input stream.
    /// </summary>
    /// <param name="converter">The converter to use.</param>
    /// <param name="input">Data to process.</param>
    /// <param name="output">Stream to write the output.</param>
    public static void Convert(this IDataBlockConverter<byte, byte> converter, Stream input, Stream output)
    {
        byte[] inputBuffer = ArrayPool<byte>.Shared.Rent(ReadBufferLength);

        int outputBufferLength = converter.GetOutputMaxCount(inputBuffer.Length);
        byte[] outputBuffer = ArrayPool<byte>.Shared.Rent(outputBufferLength);

        try {
            while (input.Position < input.Length) {
                // Read from input stream
                long inputPos = input.Position;
                int read = input.Read(inputBuffer);
                ReadOnlySpan<byte> inputData = inputBuffer.AsSpan(0, read);

                // Convert
                int produced = converter.Convert(inputData, outputBuffer, out int consumed);
                Span<byte> outputData = outputBuffer.AsSpan(0, produced);

                // Write to output stream
                output.Write(outputData);

                // Advance as many bytes as we consumed
                input.Position = inputPos + consumed;
            }
        } finally {
            ArrayPool<byte>.Shared.Return(inputBuffer);
            ArrayPool<byte>.Shared.Return(outputBuffer);
        }
    }
}
