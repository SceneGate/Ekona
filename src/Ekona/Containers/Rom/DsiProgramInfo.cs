// Copyright(c) 2022 SceneGate
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
using SceneGate.Ekona.Security;

namespace SceneGate.Ekona.Containers.Rom;

/// <summary>
/// Information of DSi programs.
/// </summary>
public class DsiProgramInfo
{
    /// <summary>
    /// Gets or sets the global memory banks settings (MBK 1 - 5) for the DSi extended memory.
    /// </summary>
    public GlobalMemoryBankSettings[] GlobalMemoryBanks { get; set; }

    /// <summary>
    /// Gets or sets the local memory banks settings for ARM9 (MBK 6 - 8) for the DSi extended memory.
    /// </summary>
    public LocalMemoryBankSettings[] LocalMemoryBanksArm9 { get; set; }

    /// <summary>
    /// Gets or sets the local memory banks settings for ARM7 (MBK 6 - 8) for the DSi extended memory.
    /// </summary>
    public LocalMemoryBankSettings[] LocalMemoryBanksArm7 { get; set; }

    /// <summary>
    /// Gets or sets the global memory bank 9 settings: WRAM slot write protect.
    /// </summary>
    public int GlobalMbk9Settings { get; set; }

    /// <summary>
    /// Gets or sets the WRAMCNT register settings.
    /// </summary>
    public byte GlobalWramCntSettings { get; set; }

    /// <summary>
    /// Gets or sets the program start jump kind.
    /// </summary>
    public ProgramStartJumpKind StartJumpKind { get; set; }

    public uint AccessControl { get; set; }

    public uint Arm7ScfgExt7Setting { get; set; }

    public uint Arm9iRamAddress { get; set; }

    public uint SdDeviceListArm7RamAddress { get; set; }

    public uint Arm7iRamAddress { get; set; }

    public int SdShared200Length { get; set; }

    public int SdShared201Length { get; set; }

    public byte EulaVersion { get; set; }

    public byte UseRatings { get; set; }

    public int SdShared202Length { get; set; }

    public int SdShared203Length { get; set; }

    public int SdShared204Length { get; set; }

    public int SdShared205Length { get; set; }

    public uint Arm9iParametersTableOffset { get; set; }

    public uint Arm7iParametersTableOffset { get; set; }

    public int ModcryptArea1Target { get; set; }

    public int ModcryptArea2Target { get; set; }

    public ulong TitleId { get; set; }

    public uint SdPublicSaveLength { get; set; }

    public uint SdPrivateSaveLength { get; set; }

    public byte AgeRatingCero { get; set; }

    public byte AgeRatingEsrb { get; set; }

    public byte AgeRatingUsk { get; set; }

    public byte AgeRatingPegiEurope { get; set; }

    public byte AgeRatingPegiPortugal { get; set; }

    public byte AgeRatingPegiUk { get; set; }

    public byte AgeRatingAgcb { get; set; }

    public byte AgeRatingGrb { get; set; }

    public HashInfo Arm9SecureMac { get; set; }

    public HashInfo Arm7Mac { get; set; }

    public HashInfo DigestMain { get; set; }

    public HashInfo Arm9iMac { get; set; }

    public HashInfo Arm7iMac { get; set; }

    public HashInfo Arm9Mac { get; set; }
}
