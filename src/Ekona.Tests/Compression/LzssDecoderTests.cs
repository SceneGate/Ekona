namespace SceneGate.Ekona.Tests.Compression;

using System;
using System.IO;
using NUnit.Framework;
using SceneGate.Ekona.Compression;
using Yarhl.IO;

[TestFixture]
public class LzssDecoderTests
{
    [Test]
    public void DecodeRawToken()
    {
        byte[] input = [0x00, 0xBE, 0xB0, 0xCA, 0xFE, 0xC0, 0xC0, 0xBA, 0xBE];
        byte[] expected = [0xBE, 0xB0, 0xCA, 0xFE, 0xC0, 0xC0, 0xBA, 0xBE];

        AssertConversion(input, expected);
    }

    [Test]
    public void DecodeCopyToken()
    {
        byte[] input = [
            0b0001_0011,
            0xBE, 0xEE, 0xEF,
            0x0_0, 0x02, // full past
            0xCA, 0xFE,
            0x1_0, 0x01, // with future
            0x0_0, 0x00, // full future
        ];
        byte[] expected = [
            0xBE, 0xEE, 0xEF, 0xBE, 0xEE, 0xEF,
            0xCA, 0xFE, 0xCA, 0xFE, 0xCA, 0xFE,
            0xFE, 0xFE, 0xFE,
        ];

        AssertConversion(input, expected);
    }

    [Test]
    public void DecodeThrowsEOSWithMissingRawAfterReadingFlag()
    {
        using var input = new DataStream();
        input.Write([ 0b0000_0000 ]);

        using DataStream actual = new();
        var decoder = new LzssDecoder(actual, false);
        Assert.That(() => decoder.Convert(input), Throws.InstanceOf<EndOfStreamException>());
    }

    [Test]
    public void DecodeStopWithPaddingFlagBits()
    {
        byte[] input = [ 0b0000_0000, 0xAA ];
        byte[] expected = [ 0xAA ];

        AssertConversion(input, expected);
    }

    [Test]
    public void DecodeThrowsEOSWithMissingCopyInfo()
    {
        using DataStream input = new();
        input.Write([ 0b0100_0000, 0xAA, 0x00 ]);

        var decoder = new LzssDecoder(new DataStream(), false);
        Assert.That(() => decoder.Convert(input), Throws.InstanceOf<EndOfStreamException>());
    }

    [Test]
    public void DecodeNewFlagAfter8Tokens()
    {
        byte[] input = [0x00, 0xBE, 0xB0, 0xCA, 0xFE, 0xC0, 0xC0, 0xBA, 0xBE, 0x00, 0xAA];
        byte[] expected = [0xBE, 0xB0, 0xCA, 0xFE, 0xC0, 0xC0, 0xBA, 0xBE, 0xAA];

        AssertConversion(input, expected);
    }

    [Test]
    public void ThrowWhenOutputBufferIsNotLargeEnough()
    {
        byte[] inputFailRaw = [ 0b0000_0000, 0xAA ];
        byte[] inputFailCopy = [ 0b0100_0000, 0x00, 0x00, 0x00 ];

        using var streamParent = new DataStream();
        using var fixedOutputStream = new DataStream(streamParent, 0, 0);
        Assert.That(
            () => new LzssDecoder(fixedOutputStream, false).Convert(new MemoryStream(inputFailRaw)),
            Throws.InstanceOf<InvalidOperationException>());
        Assert.That(
            () => new LzssDecoder(fixedOutputStream, false).Convert(new MemoryStream(inputFailCopy)),
            Throws.InstanceOf<InvalidOperationException>());
    }

    [Test]
    public void DecodeEmpty()
    {
        byte[] input = [];
        byte[] expected = [];

        AssertConversion(input, expected);
    }

    private void AssertConversion(byte[] input, byte[] expected)
    {
        using DataStream expectedStream = DataStreamFactory.FromArray(expected);
        using DataStream inputStream = DataStreamFactory.FromArray(input);

        using DataStream actual = new();
        var decoder = new LzssDecoder(actual, false);
        _ = decoder.Convert(inputStream);

        Assert.Multiple(() => {
            Assert.That(inputStream.Position, Is.EqualTo(input.Length));
            Assert.That(actual.Length, Is.EqualTo(expected.Length));
            Assert.That(expectedStream.Compare(actual), Is.True);
        });
    }
}
