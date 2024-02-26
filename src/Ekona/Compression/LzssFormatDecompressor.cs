namespace SceneGate.Ekona.Compression;

using System;
using System.IO;
using Yarhl.FileFormat;
using Yarhl.IO;

/// <summary>
/// Converter that decompress a binary format with LZSS format.
/// </summary>
public class LzssFormatDecompressor :
    IConverter<IBinary, BinaryFormat>,
    IConverter<Stream, DataStream>
{
    /// <inheritdoc />
    public BinaryFormat Convert(IBinary source)
    {
        ArgumentNullException.ThrowIfNull(source);

        DataStream decompressed = Convert(source.Stream);
        return new BinaryFormat(decompressed);
    }

    /// <inheritdoc />
    public DataStream Convert(Stream source)
    {
        ArgumentNullException.ThrowIfNull(source);

        // bit 0-3: ID (0x10), 4-31: uncompressed length
        uint header = new DataReader(source).ReadUInt32();
        uint id = header & 0xFF;
        if (id != 0x10) {
            throw new FormatException("Invalid header");
        }

        var decompressed = new DataStream();
        var decoder = new LzssDecoder();
        decoder.Convert(source, decompressed);

        return decompressed;
    }
}
