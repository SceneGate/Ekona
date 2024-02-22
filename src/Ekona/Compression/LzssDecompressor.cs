namespace SceneGate.Ekona.Compression;

using System;
using System.IO;
using Yarhl.FileFormat;
using Yarhl.IO;

public class LzssDecompressor :
    IConverter<IBinary, BinaryFormat>,
    IConverter<Stream, DataStream>
{
    public BinaryFormat Convert(IBinary source)
    {
        ArgumentNullException.ThrowIfNull(source);

        DataStream decompressed = Convert(source.Stream);
        return new BinaryFormat(decompressed);
    }

    public DataStream Convert(Stream source)
    {
        ArgumentNullException.ThrowIfNull(source);

        uint header = new DataReader(source).ReadUInt32();
        uint id = header & 0xFF;
        uint uncompressedLength = header >> 8;

        if (id != 0x10) {
            throw new FormatException("Invalid header");
        }

        var decompressed = new DataStream();

        var decoder = new LzssDecoder();
        decoder.Convert(source, decompressed);

        return decompressed;
    }
}
