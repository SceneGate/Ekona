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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SceneGate.Ekona.Containers.Rom;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Yarhl.FileFormat;
using Yarhl.FileSystem;
using Yarhl.IO;

namespace SceneGate.Ekona.Tests.Containers.Rom
{
    [TestFixture]
    public class Binary2RomHeaderTests
    {
        public static IEnumerable<TestCaseData> GetFiles()
        {
            string basePath = Path.Combine(TestDataBase.RootFromOutputPath, "Containers");
            string listPath = Path.Combine(basePath, "header.txt");
            return TestDataBase.ReadTestListFile(listPath)
                .Select(line => line.Split(','))
                .Select(data => new TestCaseData(
                    Path.Combine(basePath, data[0]),
                    Path.Combine(basePath, data[1]))
                    .SetName($"({data[1]})"));
        }

        [TestCaseSource(nameof(GetFiles))]
        public void DeserializeMatchInfo(string infoPath, string headerPath)
        {
            TestDataBase.IgnoreIfFileDoesNotExist(infoPath);
            TestDataBase.IgnoreIfFileDoesNotExist(headerPath);

            string yaml = File.ReadAllText(infoPath);
            RomHeader expected = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build()
                .Deserialize<RomHeader>(yaml);

            using Node node = NodeFactory.FromFile(headerPath, FileOpenMode.Read);
            node.Invoking(n => n.TransformWith<Binary2RomHeader>()).Should().NotThrow();

            node.GetFormatAs<RomHeader>().Should().BeEquivalentTo(
                expected,
                opts => opts.Excluding(p => p.CopyrightLogo));
        }

        [TestCaseSource(nameof(GetFiles))]
        [Ignore("It requires to implement DSi fields #9")]
        public void TwoWaysIdenticalStream(string infoPath, string headerPath)
        {
            TestDataBase.IgnoreIfFileDoesNotExist(headerPath);

            using Node node = NodeFactory.FromFile(headerPath, FileOpenMode.Read);

            var header = (RomHeader)ConvertFormat.With<Binary2RomHeader>(node.Format!);
            var generatedStream = (BinaryFormat)ConvertFormat.With<RomHeader2Binary>(header);

            var originalStream = new DataStream(node.Stream!, 0, header.SectionInfo.HeaderSize);
            originalStream.Length.Should().Be(generatedStream.Stream.Length);
            originalStream.Compare(generatedStream.Stream).Should().BeTrue();
        }

        [TestCaseSource(nameof(GetFiles))]
        public void ThreeWaysIdenticalObjects(string infoPath, string headerPath)
        {
            TestDataBase.IgnoreIfFileDoesNotExist(headerPath);

            using Node node = NodeFactory.FromFile(headerPath, FileOpenMode.Read);

            var originalHeader = (RomHeader)ConvertFormat.With<Binary2RomHeader>(node.Format!);
            var generatedStream = (BinaryFormat)ConvertFormat.With<RomHeader2Binary>(originalHeader);
            var generatedHeader = (RomHeader)ConvertFormat.With<Binary2RomHeader>(generatedStream);

            // Ignore ChecksumHeader as we are not generating identical headers due to DSi flags yet (#9).
            generatedHeader.Should().BeEquivalentTo(
                originalHeader,
                opts => opts.Excluding(p => p.ProgramInfo.ChecksumHeader));
        }
    }
}
