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
using SceneGate.Ekona.Security;
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
        public void DeserializeRomMatchHeaderInfo(string infoPath, string romPath)
        {
            TestDataBase.IgnoreIfFileDoesNotExist(romPath);
            string resourceDir = Path.GetDirectoryName(romPath);
            string headerInfoPath = Path.GetFileNameWithoutExtension(romPath) + ".header.yml";
            headerInfoPath = Path.Combine(resourceDir, headerInfoPath);
            TestDataBase.IgnoreIfFileDoesNotExist(headerInfoPath);

            string yaml = File.ReadAllText(headerInfoPath);
            ProgramInfo expectedInfo = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build()
                .Deserialize<RomHeader>(yaml)
                .ProgramInfo;

            using Node node = NodeFactory.FromFile(romPath, FileOpenMode.Read);
            node.Invoking(n => n.TransformWith<Binary2NitroRom>()).Should().NotThrow();
            node.GetFormatAs<NitroRom>().Information.Should().BeEquivalentTo(
                expectedInfo,
                opts => opts
                    .Excluding((FluentAssertions.Equivalency.IMemberInfo info) => info.Type == typeof(HashInfo))
                    .Excluding(p => p.Overlays9Info)
                    .Excluding(p => p.Overlays7Info));
        }

        [TestCaseSource(nameof(GetFiles))]
        public void DeserializeRomWithKeysHasValidSignatures(string infoPath, string romPath)
        {
            TestDataBase.IgnoreIfFileDoesNotExist(romPath);
            DsiKeyStore keys = TestDataBase.GetDsiKeyStore();

            using Node node = NodeFactory.FromFile(romPath, FileOpenMode.Read);
            node.Invoking(n => n.TransformWith<Binary2NitroRom, DsiKeyStore>(keys)).Should().NotThrow();

            NitroRom rom = node.GetFormatAs<NitroRom>();
            ProgramInfo programInfo = rom.Information;
            bool isDsi = programInfo.UnitCode != DeviceUnitKind.DS;

            if (isDsi || programInfo.ProgramFeatures.HasFlag(DsiRomFeatures.BannerSigned)) {
                programInfo.BannerMac.Status.Should().Be(HashStatus.Valid);
            }

            if (programInfo.ProgramFeatures.HasFlag(DsiRomFeatures.ProgramSigned)) {
                // TODO: Verify header (0x160 bytes) + armX (secure area encrypted) HMAC
                // programInfo.ProgramMac.Status.Should().Be(HashStatus.Valid)
                programInfo.OverlaysMac.Status.Should().Be(HashStatus.Valid);
                programInfo.Signature.Status.Should().Be(HashStatus.Valid);
            }

            if (isDsi) {
                programInfo.OverlaysMac.IsNull.Should().BeTrue();
                programInfo.ProgramMac.IsNull.Should().BeTrue();
                programInfo.Signature.Status.Should().Be(HashStatus.Valid);
            }
        }

        [TestCaseSource(nameof(GetFiles))]
        public void TwoWaysIdenticalRomStream(string infoPath, string romPath)
        {
            TestDataBase.IgnoreIfFileDoesNotExist(romPath);

            using Node node = NodeFactory.FromFile(romPath, FileOpenMode.Read);

            var rom = (NitroRom)ConvertFormat.With<Binary2NitroRom>(node.Format!);
            var generatedStream = (BinaryFormat)ConvertFormat.With<NitroRom2Binary>(rom);

            generatedStream.Stream.Length.Should().Be(node.Stream!.Length);

            // TODO: After implementing DSi fields
            if (rom.Information.UnitCode == DeviceUnitKind.DS) {
                generatedStream.Stream!.Compare(node.Stream).Should().BeTrue();
            }
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
            ProgramInfo originalInfo = node.GetFormatAs<NitroRom>().Information;

            node.Invoking(n => n.TransformWith<NitroRom2Binary>()).Should().NotThrow();
            node.Invoking(n => n.TransformWith<Binary2NitroRom>()).Should().NotThrow();
            ProgramInfo newInfo = node.GetFormatAs<NitroRom>().Information;

            node.Should().MatchInfo(expected);

            // Keep old hashes
            newInfo.OverlaysMac.Hash.Should().BeEquivalentTo(originalInfo.OverlaysMac.Hash);
            newInfo.BannerMac.Hash.Should().BeEquivalentTo(originalInfo.BannerMac.Hash);
        }

        [TestCaseSource(nameof(GetFiles))]
        public void ReadWriteThreeWaysRomWithKeyGeneratesSameHashes(string infoPath, string romPath)
        {
            TestDataBase.IgnoreIfFileDoesNotExist(romPath);
            DsiKeyStore keys = TestDataBase.GetDsiKeyStore();

            using Node node = NodeFactory.FromFile(romPath, FileOpenMode.Read);

            node.Invoking(n => n.TransformWith<Binary2NitroRom>()).Should().NotThrow();
            ProgramInfo originalInfo = node.GetFormatAs<NitroRom>().Information;

            var nitroParameters = new NitroRom2BinaryParams { KeyStore = keys };
            node.Invoking(n => n.TransformWith<NitroRom2Binary, NitroRom2BinaryParams>(nitroParameters)).Should().NotThrow();

            node.Invoking(n => n.TransformWith<Binary2NitroRom>()).Should().NotThrow();
            ProgramInfo newInfo = node.GetFormatAs<NitroRom>().Information;

            newInfo.OverlaysMac.Hash.Should().BeEquivalentTo(originalInfo.OverlaysMac.Hash);
            newInfo.BannerMac.Hash.Should().BeEquivalentTo(originalInfo.BannerMac.Hash);
        }
    }
}
