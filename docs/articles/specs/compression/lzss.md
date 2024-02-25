# LZSS

[Lempel–Ziv–Storer–Szymanski (LZSS)](https://en.wikipedia.org/wiki/Lempel%E2%80%93Ziv%E2%80%93Storer%E2%80%93Szymanski)
is a lossless compression algorithm implemented in the BIOS of the GBA and DS.
Software can trigger the decompression functions via
[SWI calls](https://problemkaputt.de/gbatek.htm#biosdecompressionfunctions).

## Format

The GBA/DS BIOS expects a 32-bits header before the compression data.

| Offset | Type   | Description     |
| ------ | ------ | --------------- |
| 0x00   | uint   | Header          |
| 0x04   | byte[] | Compressed data |

The header bit fields are:

- Bits 0-3: reserved (0)
- Bits 4-7: compression type `1`
- Bits 8-31: decompressed length

### Compression format

The compression supports two operation modes:

- Copy the next byte from the input stream into the output stream.
- Repeat a sequence from the decompressed data in the output

The compressed data starts with a flag byte that indicates the mode for the next
8 operations. The bits are processed in big-endian order that is, from bit 7 to
bit 0.

If the next flag bit is 0, then the next byte from the input stream is written
into the output stream.

If the next flag bit is 1, then there is a 16-bits value in the input stream
containing the repeat information:

- Bits 0-11: backwards counting position of the start of the sequence in the
  output stream.
- Bits 12-15: sequence length - 3 (minimum sequence length)

> [!NOTE]  
> The length of the sequence could be larger than the available output at the
> start of the decoding. While repeating the sequence, we may need to copy also
> bytes that we just wrote. For instance, we could repeat the last two bytes of
> the output 5 times by encoding the position 1 and a length of 10.

After processing every flag bit, the next input byte contains the next flags.
The operation repeats until reaching the decompressed size or running out of
input data. Note that there may be some unused bits (set to 0).
