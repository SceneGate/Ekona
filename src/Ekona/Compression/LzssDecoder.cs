namespace SceneGate.Ekona.Compression;

using System;
using System.IO;
using System.Runtime.CompilerServices;
using Yarhl.FileFormat;
using Yarhl.IO;

/// <summary>
/// Decode / Decompress blocks of data with the LZSS DS/GBA algorithm.
/// </summary>
public class LzssDecoder :
    IConverter<Stream, Stream>,
    IConverter<IBinary, BinaryFormat>
{
    private const int MinSequenceLength = 3;
    private const int MaxDistance = (1 << 12) - 1;

    private readonly CircularBuffer<byte> pastBuffer = new(MaxDistance);

    private readonly Stream output;
    private readonly bool hasHeader;

    private byte flag;
    private int remainingFlagBits;

    /// <summary>
    /// Initializes a new instance of the <see cref="LzssDecoder"/> class.
    /// </summary>
    public LzssDecoder()
    {
        output = new MemoryStream();
        hasHeader = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LzssDecoder"/> class.
    /// </summary>
    /// <param name="decompressedLength">The maximum decompressed length of the output.</param>
    public LzssDecoder(int decompressedLength)
    {
        output = new MemoryStream(decompressedLength);
        hasHeader = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LzssDecoder"/> class.
    /// </summary>
    /// <param name="output">The output stream to write the decompressed data.</param>
    /// <param name="hasHeader">Value indicating whether the input stream has a 4-bytes header.</param>
    public LzssDecoder(Stream output, bool hasHeader)
    {
        ArgumentNullException.ThrowIfNull(output);
        this.output = output;
        this.hasHeader = hasHeader;
    }

    /// <inheritdoc />
    public BinaryFormat Convert(IBinary source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return new BinaryFormat(Convert(source.Stream));
    }

    /// <inheritdoc />
    public Stream Convert(Stream source)
    {
        ArgumentNullException.ThrowIfNull(source);

        source.Position = 0;
        if (hasHeader) {
            if (source.Length < 4) {
                throw new EndOfStreamException();
            }

            // bit 0-3: ID (0x10), 4-31: uncompressed length
            Span<byte> header = stackalloc byte[4];
            source.Read(header);

            uint id = header[0];
            if (id != 0x10) {
                throw new FormatException("Invalid header");
            }
        }

        while (source.Position < source.Length) {
            if (IsFlagRawCopy(source)) {
                DecodeRawMode(source);
            } else {
                DecodePastCopyMode(source);
            }
        }

        return output;
    }

    private bool IsFlagRawCopy(Stream input)
    {
        if (remainingFlagBits <= 0) {
            remainingFlagBits = 8;
            flag = (byte)input.ReadByte();
        }

        remainingFlagBits--;
        return ((flag >> remainingFlagBits) & 1) == 0;
    }

    private void DecodeRawMode(Stream input)
    {
        if (input.Position >= input.Length) {
            throw new EndOfStreamException();
        }

        WriteOutput((byte)input.ReadByte());
    }

    private void DecodePastCopyMode(Stream input)
    {
        if (input.Position + 1 >= input.Length) {
            throw new EndOfStreamException();
        }

        byte info = (byte)input.ReadByte();
        int bufferPos = ((info & 0x0F) << 8) | (byte)input.ReadByte();
        int length = (info >> 4) + MinSequenceLength;

        while (length > 0) {
            byte value = pastBuffer[bufferPos];
            WriteOutput(value);
            length--;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteOutput(byte value)
    {
        pastBuffer.PushFront(value);
        output.WriteByte(value);
    }
}
