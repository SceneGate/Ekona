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
using Texim.Formats;
using Texim.Images;
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
            actual.ChecksumBase.IsValid.Should().BeTrue();

            if (actual.Version.Minor > 1) {
                actual.ChecksumChinese.IsValid.Should().BeTrue();
            }

            if (actual.Version.Minor > 2) {
                actual.ChecksumKorean.IsValid.Should().BeTrue();
            }

            if (actual.Version is { Major: > 1 } or { Major: 1, Minor: >= 3 }) {
                actual.SupportAnimatedIcon.Should().BeTrue();
                actual.ChecksumAnimatedIcon.IsValid.Should().BeTrue();
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
            actualIcon.Invoking(n => n.TransformWith<IndexedImage2Bitmap, IndexedImageBitmapParams>(paletteParam))
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

            // TODO: GIF support in Texim
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

            var banner = (NodeContainerFormat)ConvertFormat.With<Binary2Banner>(node.Format!);
            var generatedStream = (BinaryFormat)ConvertFormat.With<Banner2Binary>(banner);

            generatedStream.Stream.Length.Should().Be(node.Stream!.Length);

            // generatedStream.Stream.Compare(node.Stream).Should().BeTrue()
        }

        [TestCaseSource(nameof(GetFiles))]
        public void ThreeWaysIdenticalBannerObjects(string bannerPath)
        {
            TestDataBase.IgnoreIfFileDoesNotExist(bannerPath);

            using Node node = NodeFactory.FromFile(bannerPath, FileOpenMode.Read);

            var originalNode = (NodeContainerFormat)ConvertFormat.With<Binary2Banner>(node.Format!);
            var originalBanner = originalNode.Root.Children["info"].GetFormatAs<Banner>();
            var originalIcon = originalNode.Root.Children["icon"].GetFormatAs<IndexedPaletteImage>();

            var generatedStream = (BinaryFormat)ConvertFormat.With<Banner2Binary>(originalNode);

            var generatedNode = (NodeContainerFormat)ConvertFormat.With<Binary2Banner>(generatedStream);
            var generatedBanner = generatedNode.Root.Children["info"].GetFormatAs<Banner>();
            var generatedIcon = generatedNode.Root.Children["icon"].GetFormatAs<IndexedPaletteImage>();

            generatedBanner.Should().BeEquivalentTo(originalBanner, opts => opts.Excluding(f => f.ChecksumAnimatedIcon));
            generatedIcon.Should().BeEquivalentTo(originalIcon);

            // TODO: compare animated
        }
    }
}
