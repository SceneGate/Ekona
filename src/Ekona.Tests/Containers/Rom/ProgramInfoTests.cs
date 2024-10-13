namespace SceneGate.Ekona.Tests.Containers.Rom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SceneGate.Ekona.Containers.Rom;
using SceneGate.Ekona.Security;

[TestFixture]
internal class ProgramInfoTests
{
    [Test]
    public void HasValidHashesTrueIfNull()
    {
        var info = new ProgramInfo();

        Assert.That(info.HasValidHashes(), Is.True);
    }

    [Test]
    public void HasValidHashesTrueIfGenerated()
    {
        var info = new ProgramInfo();
        _ = CreateDsHashes(info, HashStatus.Generated);

        Assert.That(info.HasValidHashes(), Is.True);
    }

    [Test]
    public void HasValidHashesThrowsIfNotValidated()
    {
        var info = new ProgramInfo();
        _ = CreateDsHashes(info, HashStatus.NotValidated);

        Assert.That(() => info.HasValidHashes(), Throws.InvalidOperationException);
    }

    [Test]
    public void HasValidHashesFalseIfAnyInvalid()
    {
        var info = new ProgramInfo();
        HashInfo[] hashInfos = CreateDsHashes(info, HashStatus.Invalid);

        foreach (HashInfo hashInfo in hashInfos) {
            Assert.That(info.HasValidHashes(), Is.False);
            hashInfo.Status = HashStatus.Valid;
        }

        Assert.That(info.HasValidHashes(), Is.True);
    }

    [Test]
    public void HasValidHashesTrueIfAllValid()
    {
        var info = new ProgramInfo();
        _ = CreateDsHashes(info, HashStatus.Valid);

        Assert.That(info.HasValidHashes(), Is.True);
    }

    [Test]
    public void HasValidHashesDsi()
    {
        var info = new ProgramInfo();
        HashInfo[] hashInfos = CreateDsiHashes(info, HashStatus.Invalid);

        foreach (HashInfo hashInfo in hashInfos) {
            Assert.That(info.HasValidHashes(), Is.False);
            hashInfo.Status = HashStatus.Valid;
        }

        Assert.That(info.HasValidHashes(), Is.False);

        info.DsiInfo.DigestHashesStatus = HashStatus.Valid;
        Assert.That(info.HasValidHashes(), Is.True);
    }

    [Test]
    public void HasValidHashesDsExtended()
    {
        var info = new ProgramInfo();
        HashInfo[] hashInfos = CreateDsExtendedHashes(info, HashStatus.Invalid);

        foreach (HashInfo hashInfo in hashInfos) {
            Assert.That(info.HasValidHashes(), Is.False);
            hashInfo.Status = HashStatus.Valid;
        }

        Assert.That(info.HasValidHashes(), Is.True);
    }

    [Test]
    public void HasSignatureForDsIsTrue()
    {
        var info = new ProgramInfo();
        info.UnitCode = DeviceUnitKind.DS;

        Assert.That(info.HasValidSignature(), Is.True);

        info.Signature = CreateHashInfo(HashStatus.Invalid);
        Assert.That(info.HasValidSignature(), Is.True);
    }

    [Test]
    public void HasSignatureForDsi()
    {
        var info = new ProgramInfo();
        info.UnitCode = DeviceUnitKind.DSiExclusive;

        // null signature
        Assert.That(info.HasValidSignature(), Is.True);

        info.Signature = CreateHashInfo(HashStatus.Generated);
        Assert.That(info.HasValidSignature(), Is.True);

        info.Signature.Status = HashStatus.Invalid;
        Assert.That(info.HasValidSignature(), Is.False);

        info.Signature.Status = HashStatus.Valid;
        Assert.That(info.HasValidSignature(), Is.True);

        info.Signature.Status = HashStatus.NotValidated;
        Assert.That(() => info.HasValidSignature(), Throws.InvalidOperationException);
    }

    private static HashInfo[] CreateDsHashes(ProgramInfo info, HashStatus status)
    {
        info.UnitCode = DeviceUnitKind.DS;

#pragma warning disable S1121 // cool syntax for tests
        var hashInfos = new List<HashInfo> {
            (info.ChecksumHeader = CreateHashInfo(status)),
            (info.ChecksumLogo = CreateHashInfo(status)),
            (info.ChecksumSecureArea = CreateHashInfo(status)),
        };
#pragma warning restore S1121

        return hashInfos.ToArray();
    }

    private static HashInfo[] CreateDsExtendedHashes(ProgramInfo info, HashStatus status)
    {
        info.UnitCode = DeviceUnitKind.DS;

        info.ProgramFeatures = DsiRomFeatures.NitroProgramSigned | DsiRomFeatures.NitroBannerSigned;

#pragma warning disable S1121 // cool syntax for tests
        var hashInfos = new List<HashInfo> {
            (info.ChecksumHeader = CreateHashInfo(status)),
            (info.ChecksumLogo = CreateHashInfo(status)),
            (info.ChecksumSecureArea = CreateHashInfo(status)),

            (info.BannerMac = CreateHashInfo(status)),

            (info.NitroProgramMac = CreateHashInfo(status)),
            (info.NitroOverlaysMac = CreateHashInfo(status)),
        };
#pragma warning restore S1121

        return hashInfos.ToArray();
    }

    private static HashInfo[] CreateDsiHashes(ProgramInfo info, HashStatus status)
    {
        info.UnitCode = DeviceUnitKind.DSiExclusive;
        info.DsiInfo ??= new DsiProgramInfo();

        info.DsiInfo.DigestHashesStatus = status;

#pragma warning disable S1121 // cool syntax for tests
        var hashInfos = new List<HashInfo> {
            (info.ChecksumHeader = CreateHashInfo(status)),
            (info.ChecksumLogo = CreateHashInfo(status)),
            (info.ChecksumSecureArea = CreateHashInfo(status)),

            (info.BannerMac = CreateHashInfo(status)),

            (info.DsiInfo.Arm9SecureMac = CreateHashInfo(status)),
            (info.DsiInfo.Arm7Mac = CreateHashInfo(status)),
            (info.DsiInfo.DigestMain = CreateHashInfo(status)),
            (info.DsiInfo.Arm9iMac = CreateHashInfo(status)),
            (info.DsiInfo.Arm7iMac = CreateHashInfo(status)),
            (info.DsiInfo.Arm9Mac = CreateHashInfo(status)),
        };
#pragma warning restore S1121

        return hashInfos.ToArray();
    }

    private static HashInfo CreateHashInfo(HashStatus status)
    {
        return new HashInfo("TestAlgo", new byte[] { 0x42 }) {
            Status = status,
        };
    }
}
