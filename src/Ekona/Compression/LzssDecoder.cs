namespace SceneGate.Ekona.Compression;

using System;
using System.IO;

/// <summary>
/// Decode / Decompress blocks of data with the LZSS DS/GBA algorithm.
/// </summary>
public class LzssDecoder : IDataBlockConverter<byte, byte>
{
    private const int MinSequenceLength = 2;

    private readonly CircularBuffer<byte> pastBuffer = new((1 << 12) + 19);

    private byte flag;
    private int remainingFlagBits;

    /// <inheritdoc />
    public int GetOutputMaxCount(int inputLength)
    {
        // best compression rate achieved by copying a sequence of bytes (already in circular buffer)
        // for each bit of the token (already read) with its maximum sequence length.
        return inputLength * 8 * 18 / 17;
    }

    /// <inheritdoc />
    public int Convert(ReadOnlySpan<byte> input, Span<byte> output, out int consumed)
    {
        int produced = 0;
        consumed = 0;

        bool continueProcessing = consumed < input.Length;
        while (continueProcessing) {
            bool enoughDataAvailable = IsFlagRawCopy(input, ref consumed)
                ? DecodeRawMode(input, ref consumed, output, ref produced)
                : DecodePastCopyMode(input, ref consumed, output, ref produced);

            continueProcessing = enoughDataAvailable && (consumed < input.Length);
        }

        return produced;
    }

    private bool IsFlagRawCopy(ReadOnlySpan<byte> input, ref int consumed)
    {
        if (remainingFlagBits <= 0) {
            remainingFlagBits = 8;
            flag = input[consumed++];
        }

        remainingFlagBits--;
        return ((flag >> remainingFlagBits) & 1) == 0;
    }

    private bool DecodeRawMode(ReadOnlySpan<byte> input, ref int consumed, Span<byte> output, ref int produced)
    {
        if (consumed >= input.Length) {
            return false;
        }

        if (produced >= output.Length) {
            throw new EndOfStreamException("Output is not large enough to decompress data");
        }

        WriteOutput(input[consumed++], output, ref produced);
        return true;
    }

    private bool DecodePastCopyMode(ReadOnlySpan<byte> input, ref int consumed, Span<byte> output, ref int produced)
    {
        if (consumed + 1 >= input.Length) {
            return false;
        }

        byte info = input[consumed++];
        int bufferPos = ((info & 0x0F) << 8) | input[consumed++];
        int length = (info >> 4) + MinSequenceLength + 1;

        if (produced + length > output.Length) {
            throw new EndOfStreamException("Output is not large enough to decompress data");
        }

        while (length > 0) {
            byte value = pastBuffer[bufferPos];
            WriteOutput(value, output, ref produced);
            length--;
        }

        return true;
    }

    private void WriteOutput(byte value, Span<byte> output, ref int produced)
    {
        pastBuffer.PushFront(value);
        output[produced++] = value;
    }
}
