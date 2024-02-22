namespace SceneGate.Ekona.Compression;

using System;

public class LzssDecoder : IDataBlockConverter<byte, byte>
{
    private readonly CircularBuffer<byte> pastBuffer = new((1 << 12) + 19);

    private byte flag = 0;
    private int remainingFlagBits = 0;

    public int GetOutputMaxCount(int inputLength) => inputLength * 8 * 19; // last byte was 8-bit enabled flag to copy max

    public int Convert(ReadOnlySpan<byte> input, Span<byte> output, out int consumed)
    {
        int produced = 0;
        consumed = 0;
        while (consumed < input.Length) {
            if (IsFlagRawCopy(input, ref consumed)) {
                if (consumed >= input.Length) {
                    break;
                }

                DecodeRawMode(input, ref consumed, output, ref produced);
            } else {
                if (consumed + 1 >= input.Length) {
                    break;
                }

                DecodePastCopyMode(input, ref consumed, output, ref produced);
            }
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

    private void DecodeRawMode(ReadOnlySpan<byte> input, ref int consumed, Span<byte> output, ref int produced)
    {
        WriteOutput(input[consumed++], output, ref produced);
    }

    private void DecodePastCopyMode(ReadOnlySpan<byte> input, ref int consumed, Span<byte> output, ref int produced)
    {
        byte info = input[consumed++];
        int bufferPos = (((info & 0x0F) << 8) | input[consumed++]) + 1;
        int length = (info >> 4) + 2 + 1;

        while (length > 0) {
            byte value = pastBuffer[bufferPos - 1];
            WriteOutput(value, output, ref produced);
            length--;
        }
    }

    private void WriteOutput(byte value, Span<byte> output, ref int produced)
    {
        pastBuffer.PushFront(value);
        output[produced++] = value;
    }
}
