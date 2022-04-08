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
    public class Binary2NitroRomTests
    {
        public static IEnumerable<TestCaseData> GetFiles()
        {
            string basePath = Path.Combine(TestDataBase.RootFromOutputPath, "Containers");
            string listPath = Path.Combine(basePath, "rom.txt");
            return TestDataBase.ReadTestListFile(listPath)
                .Select(line => line.Split(','))
                .Select(data => new TestCaseData(
                    Path.Combine(basePath, data[0]),
                    Path.Combine(basePath, data[1]))
                    .SetName($"{{m}}({data[1]})"));
        }

        [TestCaseSource(nameof(GetFiles))]
        public void DeserializeRomMatchInfo(string infoPath, string romPath)
        {
            TestDataBase.IgnoreIfFileDoesNotExist(infoPath);
            TestDataBase.IgnoreIfFileDoesNotExist(romPath);

            string yaml = File.ReadAllText(infoPath);
            NodeContainerInfo expected = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build()
                .Deserialize<NodeContainerInfo>(yaml);

            using Node node = NodeFactory.FromFile(romPath, FileOpenMode.Read);
            node.Invoking(n => n.TransformWith<Binary2NitroRom>()).Should().NotThrow();
            node.Should().MatchInfo(expected);
        }

        [TestCaseSource(nameof(GetFiles))]
        [Ignore("Missing key loading")]
        public void DeserializeRomWithKeysHasValidSignatures(string infoPath, string romPath)
        {
            TestDataBase.IgnoreIfFileDoesNotExist(romPath);

            var keys = new DsiKeyStore(); // TODO: Set keys

            using Node node = NodeFactory.FromFile(romPath, FileOpenMode.Read);
            node.Invoking(n => n.TransformWith<Binary2NitroRom, DsiKeyStore>(keys)).Should().NotThrow();

            NitroRom rom = node.GetFormatAs<NitroRom>();
            RomInfo programInfo = rom.Information;
            bool isDsi = programInfo.UnitCode != DeviceUnitKind.DS;

            if (isDsi || programInfo.DsiRomFeatures.HasFlag(DsiRomFeatures.BannerHmac)) {
                programInfo.BannerMac.Status.Should().Be(HashStatus.Valid);
            }

            if (programInfo.DsiRomFeatures.HasFlag(DsiRomFeatures.SignedHeader)) {
                // TODO: Verify FAT and Header HMACs.
                // programInfo.FatMac.Status.Should().Be(HashStatus.Valid)
                // programInfo.HeaderMac.Status.Should().Be(HashStatus.Valid)
                programInfo.Signature.Status.Should().Be(HashStatus.Valid);
            }

            if (isDsi) {
                programInfo.FatMac.IsNull.Should().BeTrue();
                programInfo.HeaderMac.IsNull.Should().BeTrue();
                programInfo.Signature.Status.Should().Be(HashStatus.Valid);
            }
        }

        [TestCaseSource(nameof(GetFiles))]
        public void TwoWaysIdenticalRomStream(string infoPath, string romPath)
        {
            TestDataBase.IgnoreIfFileDoesNotExist(romPath);

            using Node node = NodeFactory.FromFile(romPath, FileOpenMode.Read);

            var rom = (NodeContainerFormat)ConvertFormat.With<Binary2NitroRom>(node.Format!);
            var generatedStream = (BinaryFormat)ConvertFormat.With<NitroRom2Binary>(rom);

            generatedStream.Stream.Length.Should().Be(node.Stream!.Length);

            // TODO: After implementing ARM9 tail and DSi fields
            // generatedStream.Stream!.Compare(node.Stream).Should().BeTrue()
        }

        [TestCaseSource(nameof(GetFiles))]
        public void ReadWriteThreeWaysRomMatchInfo(string infoPath, string romPath)
        {
            TestDataBase.IgnoreIfFileDoesNotExist(infoPath);
            TestDataBase.IgnoreIfFileDoesNotExist(romPath);

            string yaml = File.ReadAllText(infoPath);
            NodeContainerInfo expected = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build()
                .Deserialize<NodeContainerInfo>(yaml);

            using Node node = NodeFactory.FromFile(romPath, FileOpenMode.Read);
            node.Invoking(n => n.TransformWith<Binary2NitroRom>()).Should().NotThrow();
            node.Invoking(n => n.TransformWith<NitroRom2Binary>()).Should().NotThrow();
            node.Invoking(n => n.TransformWith<Binary2NitroRom>()).Should().NotThrow();

            node.Should().MatchInfo(expected);
        }
    }
}
