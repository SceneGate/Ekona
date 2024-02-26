namespace Ekona.Compressions;

using System;
using System.IO;
using Yarhl.FileFormat;
using Yarhl.IO;

public class LzssEncoder :
    IConverter<Stream, Stream>,
    IConverter<IBinary, BinaryFormat>
{
    private const int MinSequenceLength = 3;
    private const int MaxSequenceLength = (1 << 4) + MinSequenceLength - 1;
    private const int MaxDistance = (1 << 12) - 1;

    private readonly byte[] windowBuffer = new byte[MaxDistance + MaxSequenceLength];
    private readonly byte[] patternBuffer = new byte[MaxSequenceLength];
    private readonly Stream output;

    private int actionsEncoded;
    private byte currentFlag;
    private long flagPosition;

    public LzssEncoder()
    {
        output = new MemoryStream();
    }

    public LzssEncoder(Stream output)
    {
        ArgumentNullException.ThrowIfNull(output);
        this.output = output;
    }

    public BinaryFormat Convert(IBinary source)
    {
        ArgumentNullException.ThrowIfNull(source);
        Stream result = Convert(source.Stream);

        return new BinaryFormat(result);
    }

    public Stream Convert(Stream source)
    {
        while (source.Position < source.Length) {
            if (actionsEncoded == 8) {
                FlushFlag();
            }

            (int sequencePos, int sequenceLen) = FindSequence(source);

            if (sequenceLen >= MinSequenceLength) {
                currentFlag |= (byte)(1 << (7 - actionsEncoded));

                int encodedLength = sequenceLen - MinSequenceLength;
                output.WriteByte((byte)((encodedLength << 4) | (sequencePos >> 8)));
                output.WriteByte((byte)sequencePos);

                source.Position += sequenceLen;
            } else {
                // flag bit is 0, so no need to update it
                output.WriteByte((byte)source.ReadByte());
            }

            actionsEncoded++;
        }

        FlushFlag();
        return output;
    }

    private void FlushFlag()
    {
        long currentPos = output.Position;
        output.Position = flagPosition;
        output.WriteByte(currentFlag);

        currentFlag = 0;
        output.Position = currentPos;
        flagPosition = currentPos;
    }

    private (int pos, int length) FindSequence(Stream input)
    {
        long inputLen = input.Length;

        int maxPattern = (int)(input.Position + MaxSequenceLength > inputLen
            ? inputLen - input.Position
            : MaxSequenceLength);
        if (maxPattern < MinSequenceLength) {
            return (0, 0);
        }

        long windowPos = input.Position > MaxDistance ? input.Position - MaxDistance : 0;
        int windowLen = (int)(windowPos + MaxDistance > input.Position
            ? input.Position - windowPos
            : MaxDistance);
        if (windowLen == 0) {
            return (-1, -1);
        }

        long inputPos = input.Position;

        Span<byte> window = windowBuffer.AsSpan(0, windowLen + (maxPattern - 1));
        input.Position = windowPos;
        _ = input.Read(window);

        Span<byte> pattern = patternBuffer.AsSpan(0, maxPattern);
        input.Position = inputPos;
        _ = input.Read(pattern);

        input.Position = inputPos;

        int bestLength = -1;
        int bestPos = -1;
        for (int pos = windowLen - 1; pos >= 0; pos--) {
            int length = 0;
            for (; length < maxPattern; length++) {
                if (pattern[length] != window[pos + length]) {
                    break;
                }
            }

            if (length > bestLength) {
                bestLength = length;
                bestPos = pos;
                if (length == MaxSequenceLength) {
                    return (windowLen - bestPos, bestLength);
                }
            }
        }

        return (windowLen - bestPos, bestLength);
    }
}
