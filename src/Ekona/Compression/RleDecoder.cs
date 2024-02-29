namespace SceneGate.Ekona.Compression;

using System;
using System.IO;
using Yarhl.FileFormat;
using Yarhl.IO;

/// <summary>
/// Decode / Decompress blocks of data with the RLE DS/GBA algorithm.
/// </summary>
public class RleDecoder :
    IConverter<Stream, Stream>,
    IConverter<IBinary, BinaryFormat>
{
    private const int MinSequence = 2;

    private readonly Stream output;
    private readonly bool hasHeader;

    /// <summary>
    /// Initializes a new instance of the <see cref="RleDecoder"/> class.
    /// </summary>
    public RleDecoder()
    {
        output = new MemoryStream();
        hasHeader = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RleDecoder"/> class.
    /// </summary>
    /// <param name="decompressedLength">The maximum decompressed length of the output.</param>
    public RleDecoder(int decompressedLength)
    {
        output = new MemoryStream(decompressedLength);
        hasHeader = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RleDecoder"/> class.
    /// </summary>
    /// <param name="output">The output stream to write the decompressed data.</param>
    /// <param name="hasHeader">Value indicating whether the input stream has a 4-bytes header.</param>
    public RleDecoder(Stream output, bool hasHeader)
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
            if (source.Read(header) != 4) {
                throw new FormatException("Insufficient bytes");
            }

            uint id = header[0];
            if (id != 0x30) {
                throw new FormatException("Invalid header");
            }
        }

        while (source.Position < source.Length) {
            int seqInfo = source.ReadByte();
            bool isCompressed = (seqInfo >> 7) == 1;
            int length = (seqInfo & 0x7F) + 1;

            if (isCompressed) {
                length += MinSequence;
                byte value = (byte)source.ReadByte();
                for (int i = 0; i < length; i++) {
                    output.WriteByte(value);
                }
            } else {
                for (int i = 0; i < length; i++) {
                    output.WriteByte((byte)source.ReadByte());
                }
            }
        }

        return output;
    }
}
