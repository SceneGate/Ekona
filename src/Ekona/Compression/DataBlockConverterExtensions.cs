namespace SceneGate.Ekona.Compression;

using System;
using System.Buffers;
using System.IO;

public static class DataBlockConverterExtensions
{
    private const int ReadBufferLength = 40 * 1024;

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
