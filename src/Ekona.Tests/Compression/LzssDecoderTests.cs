namespace SceneGate.Ekona.Tests.Compression;

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SceneGate.Ekona.Compression;

[TestFixture]
public class LzssDecoderTests
{
    [Test]
    public void MaxCountSizeForSmallBufferGivesNonZero()
    {
        const int inputLength = 1;
        var decoder = new LzssDecoder();

        int maxCount = decoder.GetOutputMaxCount(inputLength);

        Assert.That(maxCount, Is.GreaterThan(0));
    }

    [Test]
    public void MaxCountForReadBufferIsLessThanLoh()
    {
        const int inputLength = 9 * 1024;
        var decoder = new LzssDecoder();

        int maxCount = decoder.GetOutputMaxCount(inputLength);

        Assert.That(maxCount, Is.LessThan(80 * 1024));
    }

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
    public void DecodeStopWithMissingRawAfterReadingFlag()
    {
        byte[] input = [ 0b0000_0000 ];
        byte[] expected = [ ];

        AssertConversion(input, expected);
    }

    [Test]
    public void DecodeStopWithMissingRawAfterIteration()
    {
        byte[] input = [ 0b0000_0000, 0xAA ];
        byte[] expected = [ 0xAA ];

        AssertConversion(input, expected);
    }

    [Test]
    public void DecodeStopWithMissingCopyInfo()
    {
        byte[] input = [ 0b0100_0000, 0xAA, 0x00 ];
        byte[] expected = [ 0xAA ];

        var decoder = new LzssDecoder();
        byte[] actual = new byte[32];

        int produced = decoder.Convert(input, actual, out int consumed);
        Assert.Multiple(() => {
            Assert.That(consumed, Is.EqualTo(2));
            Assert.That(produced, Is.EqualTo(expected.Length));
            Assert.That(actual.Take(produced), Is.EquivalentTo(expected));
        });
    }

    [Test]
    public void DecodeContinueKeepingInfo()
    {
        // Test keep flag and past buffer
        byte[] input1 = [ 0b0010_0000, 0xCA, 0xFE ];
        byte[] input2 = [ 0x00, 0x00 ];
        byte[] expected = [ 0xCA, 0xFE, 0xFE, 0xFE, 0xFE ];

        var decoder = new LzssDecoder();
        byte[] actual = new byte[32];

        int produced1 = decoder.Convert(input1, actual, out int consumed1);
        Assert.Multiple(() => {
            Assert.That(consumed1, Is.EqualTo(input1.Length));
            Assert.That(produced1, Is.EqualTo(2));
        });

        int produced2 = decoder.Convert(input2, actual.AsSpan(produced1), out int consumed2);
        Assert.Multiple(() => {
            Assert.That(consumed2, Is.EqualTo(input2.Length));
            Assert.That(produced2, Is.EqualTo(3));
            Assert.That(actual.Take(produced1 + produced2), Is.EquivalentTo(expected));
        });
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
        byte[] inputFailCopy = [ 0b1000_0000, 0x00, 0x00 ];

        Assert.That(
            () => new LzssDecoder().Convert(inputFailRaw, [], out _),
            Throws.InstanceOf<EndOfStreamException>());
        Assert.That(
            () => new LzssDecoder().Convert(inputFailCopy, [], out _),
            Throws.InstanceOf<EndOfStreamException>());
    }

    [Test]
    public void DecodeEmpty()
    {
        byte[] input = [];
        byte[] expected = [];

        AssertConversion(input, expected);
    }

    private static void AssertConversion(ReadOnlySpan<byte> input, ReadOnlySpan<byte> expected)
    {
        var decoder = new LzssDecoder();
        byte[] actual = new byte[32];

        int produced = decoder.Convert(input, actual, out int consumed);

        Assert.That(consumed, Is.EqualTo(input.Length));
        Assert.That(produced, Is.EqualTo(expected.Length));
        Assert.That(actual.Take(produced), Is.EquivalentTo(expected.ToArray()));
    }
}
