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
                    .Excluding((FluentAssertions.Equivalency.IMemberInfo info) => info.Type == typeof(HashStatus))
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

            programInfo.ChecksumSecureArea.Status.Should().Be(HashStatus.Valid);

            if (isDsi || programInfo.ProgramFeatures.HasFlag(DsiRomFeatures.NitroBannerSigned)) {
                programInfo.BannerMac.Status.Should().Be(HashStatus.Valid);
            }

            if (programInfo.ProgramFeatures.HasFlag(DsiRomFeatures.NitroProgramSigned)) {
                programInfo.NitroProgramMac.Status.Should().Be(HashStatus.Valid);
                programInfo.NitroOverlaysMac.Status.Should().Be(HashStatus.Valid);
                programInfo.Signature.Status.Should().Be(HashStatus.Valid);
            }

            if (isDsi) {
                programInfo.NitroProgramMac.IsNull.Should().BeTrue();
                programInfo.NitroOverlaysMac.IsNull.Should().BeTrue();
                programInfo.Signature.Status.Should().Be(HashStatus.Valid);

                programInfo.DsiInfo.Arm9SecureMac.Status.Should().Be(HashStatus.Valid);
                programInfo.DsiInfo.Arm7Mac.Status.Should().Be(HashStatus.Valid);
                programInfo.DsiInfo.DigestMain.Status.Should().Be(HashStatus.Valid);

                // TODO: After modcrypt implementation.
                // programInfo.DsiInfo.Arm9iMac.Status.Should().Be(HashStatus.Valid)
                // programInfo.DsiInfo.Arm7iMac.Status.Should().Be(HashStatus.Valid)
                programInfo.DsiInfo.Arm9Mac.Status.Should().Be(HashStatus.Valid);

                programInfo.DsiInfo.DigestHashesStatus.Should().Be(HashStatus.Valid);
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

            // TODO: After generating DSi disgest (requires modcrypt #11)
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

            bool isDsi = originalInfo.UnitCode != DeviceUnitKind.DS;

            newInfo.ChecksumSecureArea.Hash.Should().BeEquivalentTo(originalInfo.ChecksumSecureArea.Hash);
            newInfo.ChecksumSecureArea.Status.Should().Be(HashStatus.NotValidated);

            if (isDsi || originalInfo.ProgramFeatures.HasFlag(DsiRomFeatures.NitroBannerSigned)) {
                newInfo.BannerMac.Hash.Should().BeEquivalentTo(originalInfo.BannerMac.Hash);
                newInfo.BannerMac.Status.Should().Be(HashStatus.NotValidated);
            }

            if (originalInfo.ProgramFeatures.HasFlag(DsiRomFeatures.NitroProgramSigned)) {
                newInfo.NitroProgramMac.Hash.Should().BeEquivalentTo(originalInfo.NitroProgramMac.Hash);
                newInfo.NitroProgramMac.Status.Should().Be(HashStatus.NotValidated);

                newInfo.NitroOverlaysMac.Hash.Should().BeEquivalentTo(originalInfo.NitroOverlaysMac.Hash);
                newInfo.NitroOverlaysMac.Status.Should().Be(HashStatus.NotValidated);

                // Not regenerated but should keep it
                newInfo.Signature.Hash.Should().BeEquivalentTo(originalInfo.Signature.Hash);
                newInfo.Signature.Status.Should().Be(HashStatus.NotValidated);
            }

            if (isDsi) {
                // Not regenerated but should keep it
                newInfo.Signature.Hash.Should().BeEquivalentTo(originalInfo.Signature.Hash);
                newInfo.Signature.Status.Should().Be(HashStatus.NotValidated);

                newInfo.DsiInfo.Arm9SecureMac.Hash.Should().BeEquivalentTo(originalInfo.DsiInfo.Arm9SecureMac.Hash);
                newInfo.DsiInfo.Arm9SecureMac.Status.Should().Be(HashStatus.NotValidated);

                newInfo.DsiInfo.Arm7Mac.Hash.Should().BeEquivalentTo(originalInfo.DsiInfo.Arm7Mac.Hash);
                newInfo.DsiInfo.Arm7Mac.Status.Should().Be(HashStatus.NotValidated);

                newInfo.DsiInfo.DigestMain.Hash.Should().BeEquivalentTo(originalInfo.DsiInfo.DigestMain.Hash);
                newInfo.DsiInfo.DigestMain.Status.Should().Be(HashStatus.NotValidated);

                newInfo.DsiInfo.Arm9iMac.Hash.Should().BeEquivalentTo(originalInfo.DsiInfo.Arm9iMac.Hash);
                newInfo.DsiInfo.Arm9iMac.Status.Should().Be(HashStatus.NotValidated);

                newInfo.DsiInfo.Arm7iMac.Hash.Should().BeEquivalentTo(originalInfo.DsiInfo.Arm7iMac.Hash);
                newInfo.DsiInfo.Arm7iMac.Status.Should().Be(HashStatus.NotValidated);

                newInfo.DsiInfo.Arm9Mac.Hash.Should().BeEquivalentTo(originalInfo.DsiInfo.Arm9Mac.Hash);
                newInfo.DsiInfo.Arm9Mac.Status.Should().Be(HashStatus.NotValidated);

                newInfo.DsiInfo.DigestHashesStatus.Should().Be(HashStatus.NotValidated);
            }
        }

        [TestCaseSource(nameof(GetFiles))]
        public void WriteSeveralTimesGeneratesSameResult(string infoPath, string romPath)
        {
            TestDataBase.IgnoreIfFileDoesNotExist(romPath);

            using Node node = NodeFactory.FromFile(romPath, FileOpenMode.Read);
            NitroRom rom = node.TransformWith<Binary2NitroRom>().GetFormatAs<NitroRom>();

            using var output1 = (BinaryFormat)ConvertFormat.With<NitroRom2Binary>(rom);
            using var output2 = (BinaryFormat)ConvertFormat.With<NitroRom2Binary>(rom);
            output1.Stream.Compare(output2.Stream).Should().BeTrue();
        }

        [TestCaseSource(nameof(GetFiles))]
        public void WriteSpecificStreamGeneratesSameResult(string infoPath, string romPath)
        {
            TestDataBase.IgnoreIfFileDoesNotExist(romPath);

            using Node node = NodeFactory.FromFile(romPath, FileOpenMode.Read);
            NitroRom rom = node.TransformWith<Binary2NitroRom>().GetFormatAs<NitroRom>();

            using var createdStream = (BinaryFormat)ConvertFormat.With<NitroRom2Binary>(rom);

            using var ownStream = new DataStream();
            var converterParams = new NitroRom2BinaryParams { OutputStream = ownStream };
            var returnStream = (BinaryFormat)ConvertFormat.With<NitroRom2Binary, NitroRom2BinaryParams>(converterParams, rom);

            returnStream.Stream.Should().BeSameAs(ownStream);
            ownStream.Length.Should().Be(createdStream.Stream.Length);
            ownStream.Compare(createdStream.Stream).Should().BeTrue();

            // Second pass
            ConvertFormat.With<NitroRom2Binary, NitroRom2BinaryParams>(converterParams, rom);
            ownStream.Disposed.Should().BeFalse();
            ownStream.Length.Should().Be(createdStream.Stream.Length);
            ownStream.Compare(createdStream.Stream).Should().BeTrue();
        }

        [TestCaseSource(nameof(GetFiles))]
        public void ReadWriteThreeWaysRomWithKeyGeneratesSameHashes(string infoPath, string romPath)
        {
            TestDataBase.IgnoreIfFileDoesNotExist(romPath);
            DsiKeyStore keys = TestDataBase.GetDsiKeyStore();

            using Node node = NodeFactory.FromFile(romPath, FileOpenMode.Read);

            node.Invoking(n => n.TransformWith<Binary2NitroRom>()).Should().NotThrow();
            ProgramInfo originalInfo = node.GetFormatAs<NitroRom>()!.Information;

            var nitroParameters = new NitroRom2BinaryParams { KeyStore = keys };
            node.Invoking(n => n.TransformWith<NitroRom2Binary, NitroRom2BinaryParams>(nitroParameters)).Should().NotThrow();

            node.Invoking(n => n.TransformWith<Binary2NitroRom>()).Should().NotThrow();
            ProgramInfo newInfo = node.GetFormatAs<NitroRom>()!.Information;
            bool isDsi = originalInfo.UnitCode != DeviceUnitKind.DS;

            newInfo.ChecksumSecureArea.Hash.Should().BeEquivalentTo(originalInfo.ChecksumSecureArea.Hash);
            originalInfo.ChecksumSecureArea.Status.Should().Be(HashStatus.Generated);

            if (isDsi || originalInfo.ProgramFeatures.HasFlag(DsiRomFeatures.NitroBannerSigned)) {
                newInfo.BannerMac.Hash.Should().BeEquivalentTo(originalInfo.BannerMac.Hash);
                originalInfo.BannerMac.Status.Should().Be(HashStatus.Generated);
            }

            if (originalInfo.ProgramFeatures.HasFlag(DsiRomFeatures.NitroProgramSigned)) {
                newInfo.NitroProgramMac.Hash.Should().BeEquivalentTo(originalInfo.NitroProgramMac.Hash);
                originalInfo.NitroProgramMac.Status.Should().Be(HashStatus.Generated);

                newInfo.NitroOverlaysMac.Hash.Should().BeEquivalentTo(originalInfo.NitroOverlaysMac.Hash);
                originalInfo.NitroOverlaysMac.Status.Should().Be(HashStatus.Generated);

                // Not regenerated but should keep it
                newInfo.Signature.Hash.Should().BeEquivalentTo(originalInfo.Signature.Hash);
                originalInfo.Signature.Status.Should().Be(HashStatus.NotValidated);
            }

            if (isDsi) {
                // Not regenerated but should keep it
                newInfo.Signature.Hash.Should().BeEquivalentTo(originalInfo.Signature.Hash);
                originalInfo.Signature.Status.Should().Be(HashStatus.NotValidated);

                newInfo.DsiInfo.Arm9SecureMac.Hash.Should().BeEquivalentTo(originalInfo.DsiInfo.Arm9SecureMac.Hash);
                originalInfo.DsiInfo.Arm9SecureMac.Status.Should().Be(HashStatus.Generated);

                newInfo.DsiInfo.Arm7Mac.Hash.Should().BeEquivalentTo(originalInfo.DsiInfo.Arm7Mac.Hash);
                originalInfo.DsiInfo.Arm7Mac.Status.Should().Be(HashStatus.Generated);

                newInfo.DsiInfo.DigestMain.Hash.Should().BeEquivalentTo(originalInfo.DsiInfo.DigestMain.Hash);
                originalInfo.DsiInfo.DigestMain.Status.Should().Be(HashStatus.Generated);

                // TODO: After modcrypt implementation.
                // newInfo.DsiInfo.Arm9iMac.Hash.Should().BeEquivalentTo(originalInfo.DsiInfo.Arm9iMac.Hash)
                // originalInfo.DsiInfo.Arm9iMac.Status.Should().Be(HashStatus.Generated)
                // newInfo.DsiInfo.Arm7iMac.Hash.Should().BeEquivalentTo(originalInfo.DsiInfo.Arm7iMac.Hash)
                // originalInfo.DsiInfo.Arm7iMac.Status.Should().Be(HashStatus.Generated)
                // originalInfo.DsiInfo.DigestHashesStatus.Should().Be(HashStatus.Generated);
                newInfo.DsiInfo.Arm9Mac.Hash.Should().BeEquivalentTo(originalInfo.DsiInfo.Arm9Mac.Hash);
                originalInfo.DsiInfo.Arm9Mac.Status.Should().Be(HashStatus.Generated);
            }
        }
    }
}
