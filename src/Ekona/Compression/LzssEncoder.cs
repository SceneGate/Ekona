namespace SceneGate.Ekona.Compression;

using System;
using System.IO;
using Yarhl.FileFormat;
using Yarhl.IO;

/// <summary>
/// Encode / Compress blocks of data with the LZSS DS/GBA algorithm.
/// </summary>
public class LzssEncoder :
    IConverter<Stream, Stream>,
    IConverter<IBinary, BinaryFormat>
{
    private const int MinSequenceLength = 3;
    private const int MaxSequenceLength = (1 << 4) - 1 + MinSequenceLength;
    private const int MaxDistance = 1 << 12;

    private readonly byte[] windowBuffer = new byte[MaxDistance + MaxSequenceLength];
    private Stream output = null!;

    private byte currentFlag;
    private long flagPosition;

    /// <summary>
    /// Initializes a new instance of the <see cref="LzssEncoder"/> class.
    /// </summary>
    /// <remarks>The output of the converter will be in a new memory stream each time.</remarks>
    public LzssEncoder()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LzssEncoder"/> class.
    /// </summary>
    /// <param name="output">Stream to write the output.</param>
    public LzssEncoder(Stream output)
    {
        ArgumentNullException.ThrowIfNull(output);
        this.output = output;
    }

    /// <inheritdoc />
    public BinaryFormat Convert(IBinary source)
    {
        ArgumentNullException.ThrowIfNull(source);
        Stream result = Convert(source.Stream);

        return new BinaryFormat(result);
    }

    /// <inheritdoc />
    public Stream Convert(Stream source)
    {
        output ??= new MemoryStream();

        long decompressedLength = source.Length;
        output.WriteByte(0x10); // compression ID
        output.WriteByte((byte)(decompressedLength & 0xFF));
        output.WriteByte((byte)(decompressedLength >> 8));
        output.WriteByte((byte)(decompressedLength >> 16));

        // Prepare the initial token flag
        currentFlag = 0;
        flagPosition = output.Position;
        output.WriteByte(0);

        int actionsEncoded = 0;
        source.Position = 0;
        while (source.Position < source.Length) {
            if (actionsEncoded == 8) {
                FlushFlag(true);
                actionsEncoded = 0;
            }

            (int sequencePos, int sequenceLen) = FindSequence(source);

            if (sequenceLen >= MinSequenceLength) {
                currentFlag |= (byte)(1 << (7 - actionsEncoded));

                int encodedLength = sequenceLen - MinSequenceLength;
                int encodedPos = sequencePos;
                output.WriteByte((byte)((encodedLength << 4) | (encodedPos >> 8)));
                output.WriteByte((byte)encodedPos);

                source.Position += sequenceLen;
            } else {
                // flag bit is 0, so no need to update it
                output.WriteByte((byte)source.ReadByte());
            }

            actionsEncoded++;
        }

        FlushFlag(false);
        return output;
    }

    private void FlushFlag(bool hasMoreData)
    {
        long currentPos = output.Position;
        output.Position = flagPosition;
        output.WriteByte(currentFlag);
        output.Position = currentPos;

        if (hasMoreData) {
            currentFlag = 0;
            flagPosition = currentPos;
            output.WriteByte(0x00);
        }
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
            return (0, 0);
        }

        Span<byte> window = windowBuffer.AsSpan(0, windowLen + maxPattern);

        long inputPos = input.Position;
        input.Position = windowPos;
        _ = input.Read(window);
        input.Position = inputPos;

        Span<byte> fullPattern = window[^maxPattern..];

        int bestLength = -1;
        int bestPos = -1;
        for (int pos = 0; pos < windowLen - 1; pos++) {
            int length = 0;
            for (; length < maxPattern; length++) {
                if (fullPattern[length] != window[pos + length]) {
                    break;
                }
            }

            if (length > bestLength) {
                bestLength = length;
                bestPos = pos;
                if (length == MaxSequenceLength) {
                    return (windowLen - bestPos - 1, bestLength);
                }
            }
        }

        return (windowLen - bestPos - 1, bestLength);
    }
}
