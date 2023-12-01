// Copyright(c) 2021 SceneGate
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SceneGate.Ekona.Containers.Rom;
using SceneGate.Ekona.Security;
using Texim.Animations;
using Texim.Formats;
using Texim.Palettes;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Yarhl.FileFormat;
using Yarhl.FileSystem;
using Yarhl.IO;

namespace SceneGate.Ekona.Tests.Containers.Rom
{
    [TestFixture]
    public class Binary2BannerTests
    {
        public static IEnumerable<TestCaseData> GetFiles()
        {
            string basePath = Path.Combine(TestDataBase.RootFromOutputPath, "Containers");
            string listPath = Path.Combine(basePath, "banner.txt");
            return TestDataBase.ReadTestListFile(listPath)
                .Select(data => new TestCaseData(Path.Combine(basePath, data))
                    .SetName($"{{m}}({data})"));
        }

        public static string GetInfoFile(string bannerPath) => bannerPath + ".yml";

        public static string GetIconFile(string bannerPath) => bannerPath + ".png";

        public static string GetGifFile(string bannerPath) => bannerPath + ".gif";

        public static string GetAniInfoFile(string bannerPath) => bannerPath + ".ani.yml";

        [TestCaseSource(nameof(GetFiles))]
        public void DeserializeBannerMatchInfo(string bannerPath)
        {
            TestDataBase.IgnoreIfFileDoesNotExist(bannerPath);

            string infoPath = GetInfoFile(bannerPath);
            TestDataBase.IgnoreIfFileDoesNotExist(infoPath);

            string yaml = File.ReadAllText(infoPath);
            Banner expected = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build()
                .Deserialize<Banner>(yaml);

            using Node node = NodeFactory.FromFile(bannerPath, FileOpenMode.Read);
            node.Invoking(n => n.TransformWith<Binary2Banner>()).Should().NotThrow();
            node.Children["info"].Should().NotBeNull();

            var actual = node.Children["info"].GetFormatAs<Banner>();
            actual.Version.Should().Be(expected.Version);
            actual.ChecksumBase.Status.Should().Be(HashStatus.Valid);

            if (actual.Version.Minor > 1) {
                actual.ChecksumChinese.Status.Should().Be(HashStatus.Valid);
            }

            if (actual.Version.Minor > 2) {
                actual.ChecksumKorean.Status.Should().Be(HashStatus.Valid);
            }

            if (actual.Version is { Major: > 1 } or { Major: 1, Minor: >= 3 }) {
                actual.SupportAnimatedIcon.Should().BeTrue();
                actual.ChecksumAnimatedIcon.Status.Should().Be(HashStatus.Valid);
            } else {
                actual.SupportAnimatedIcon.Should().BeFalse();
            }

            actual.JapaneseTitle.Should().Be(expected.JapaneseTitle);
            actual.EnglishTitle.Should().Be(expected.EnglishTitle);
            actual.FrenchTitle.Should().Be(expected.FrenchTitle);
            actual.GermanTitle.Should().Be(expected.GermanTitle);
            actual.ItalianTitle.Should().Be(expected.ItalianTitle);
            actual.SpanishTitle.Should().Be(expected.SpanishTitle);

            if (actual.Version.Minor > 1) {
                actual.ChineseTitle.Should().Be(expected.ChineseTitle);
            }

            if (actual.Version.Minor > 2) {
                actual.KoreanTitle.Should().Be(expected.KoreanTitle);
            }
        }

        [TestCaseSource(nameof(GetFiles))]
        public void DeserializeBannerIcon(string bannerPath)
        {
            TestDataBase.IgnoreIfFileDoesNotExist(bannerPath);

            string expectedIconPath = GetIconFile(bannerPath);
            TestDataBase.IgnoreIfFileDoesNotExist(expectedIconPath);

            using Node node = NodeFactory.FromFile(bannerPath, FileOpenMode.Read);
            node.Invoking(n => n.TransformWith<Binary2Banner>()).Should().NotThrow();
            Node actualIcon = node.Children["icon"];
            actualIcon.Should().NotBeNull();

            var paletteParam = new IndexedImageBitmapParams {
                Palettes = actualIcon.GetFormatAs<IPaletteCollection>(),
            };
            var image2Bitmap = new IndexedImage2Bitmap(paletteParam);

            actualIcon.Invoking(n => n.TransformWith(image2Bitmap))
                .Should().NotThrow();

            using var expectedIcon = NodeFactory.FromFile(expectedIconPath, FileOpenMode.Read);
            actualIcon.Stream.Compare(expectedIcon.Stream).Should().BeTrue();
        }

        [TestCaseSource(nameof(GetFiles))]
        public void DeserializeBannerAnimatedIcon(string bannerPath)
        {
            TestDataBase.IgnoreIfFileDoesNotExist(bannerPath);

            using Node node = NodeFactory.FromFile(bannerPath, FileOpenMode.Read);
            node.Invoking(n => n.TransformWith<Binary2Banner>()).Should().NotThrow();
            if (!node.Children["info"].GetFormatAs<Banner>().SupportAnimatedIcon) {
                Assert.Pass();
            }

            Node animated = node.Children["animated"];
            animated.Should().NotBeNull();

            int numImages = Binary2Banner.NumAnimatedImages;
            animated.Children.Where(n => n.Name.StartsWith("bitmap")).Should().HaveCount(numImages);
            animated.Children["palettes"]?.GetFormatAs<PaletteCollection>()?.Palettes.Should().HaveCount(numImages);
            animated.Children["animation"].Should().NotBeNull();

            string gifPath = GetGifFile(bannerPath);
            TestDataBase.IgnoreIfFileDoesNotExist(gifPath);
            using var expectedGif = DataStreamFactory.FromFile(gifPath, FileOpenMode.Read);

            AnimatedFullImage animatedImage = animated.GetFormatAs<NodeContainerFormat>()
                .ConvertWith(new IconAnimation2AnimatedImage());
            using var actualGif = new AnimatedFullImage2Gif().Convert(animatedImage);
            actualGif.Stream.Compare(expectedGif).Should().BeTrue("GIF streams must be identical");

            string infoPath = GetAniInfoFile(bannerPath);
            TestDataBase.IgnoreIfFileDoesNotExist(infoPath);
            string yaml = File.ReadAllText(infoPath);
            IconAnimationSequence expectedInfo = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build()
                .Deserialize<IconAnimationSequence>(yaml);

            animated.Children["animation"].GetFormatAs<IconAnimationSequence>()
                .Should().BeEquivalentTo(expectedInfo);
        }

        [Test]
        public void FrameCannotHaveMoreThan8ImagesPalettes()
        {
            var frame = new IconAnimationFrame();
            frame.Invoking(f => f.BitmapIndex = 8)
                .Should().Throw<ArgumentOutOfRangeException>();
            frame.Invoking(f => f.BitmapIndex = -1)
                .Should().Throw<ArgumentOutOfRangeException>();
            frame.Invoking(f => f.PaletteIndex = 8)
                .Should().Throw<ArgumentOutOfRangeException>();
            frame.Invoking(f => f.PaletteIndex = -1)
                .Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestCaseSource(nameof(GetFiles))]
        public void TwoWaysIdenticalBannerStream(string bannerPath)
        {
            TestDataBase.IgnoreIfFileDoesNotExist(bannerPath);

            using Node node = NodeFactory.FromFile(bannerPath, FileOpenMode.Read);

            var banner = node.GetFormatAs<IBinary>().ConvertWith(new Binary2Banner());
            var generatedStream = banner.ConvertWith(new Banner2Binary());

            generatedStream.Stream.Length.Should().Be(node.Stream!.Length);
            generatedStream.Stream.Compare(node.Stream).Should().BeTrue();
        }

        [TestCaseSource(nameof(GetFiles))]
        public void ThreeWaysIdenticalBannerObjects(string bannerPath)
        {
            TestDataBase.IgnoreIfFileDoesNotExist(bannerPath);

            using var originalStream = new BinaryFormat(DataStreamFactory.FromFile(bannerPath, FileOpenMode.Read));
            using NodeContainerFormat originalNode = originalStream.ConvertWith(new Binary2Banner());
            using BinaryFormat generatedStream = originalNode.ConvertWith(new Banner2Binary());
            using NodeContainerFormat generatedNode = generatedStream.ConvertWith(new Binary2Banner());

            generatedNode.Should().BeEquivalentTo(originalNode, opts => opts.IgnoringCyclicReferences());
        }
    }
}
