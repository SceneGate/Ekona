using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SceneGate.Ekona.Containers.Rom;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
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
                .Select(line => line.Split(','))
                .Select(data => new TestCaseData(
                    Path.Combine(basePath, data[0]),
                    Path.Combine(basePath, data[1]))
                    .SetName($"({data[1]})"));
        }

        [TestCaseSource(nameof(GetFiles))]
        public void DeserializeMatchInfo(string infoPath, string bannerPath)
        {
            TestDataBase.IgnoreIfFileDoesNotExist(infoPath);
            TestDataBase.IgnoreIfFileDoesNotExist(bannerPath);

            string yaml = File.ReadAllText(infoPath);
            Banner expected = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build()
                .Deserialize<Banner>(yaml);

            using Node node = NodeFactory.FromFile(bannerPath, FileOpenMode.Read);
            node.Invoking(n => n.TransformWith<Binary2Banner>()).Should().NotThrow();

            var actual = node.GetFormatAs<Banner>();
            actual.Version.Should().Be(expected.Version);
            actual.ChecksumBase.IsValid.Should().BeTrue();

            if (actual.Version.Minor > 1) {
                actual.ChecksumChinese.IsValid.Should().BeTrue();
            }

            if (actual.Version.Minor > 2) {
                actual.ChecksumKorean.IsValid.Should().BeTrue();
            }

            if (actual.Version is { Major: 1, Minor: 3 }) {
                actual.ChecksumAnimatedIcon.IsValid.Should().BeTrue();
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
    }
}
