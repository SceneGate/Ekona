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

    /// <summary>
    /// Gets or sets the access control for DSi features.
    /// </summary>
    public TwilightAccessControl AccessControl { get; set; }

    /// <summary>
    /// Gets or sets some extended features of DSi ARM7 control register (SCFG).
    /// </summary>
    public ScfgExtendedFeaturesArm7 ScfgExtendedArm7 { get; set; }

    /// <summary>
    /// Gets or sets the start of the RAM address for the ARM9i program.
    /// </summary>
    public uint Arm9iRamAddress { get; set; }

    /// <summary>
    /// Gets or sets the SD/eMMC device list ARM7 RAM address.
    /// 0x400 bytes initialized by the firmware.
    /// </summary>
    public uint StorageDeviceListArm7RamAddress { get; set; }

    /// <summary>
    /// Gets or sets the start of the RAM address for the ARM7i program.
    /// </summary>
    public uint Arm7iRamAddress { get; set; }

    /// <summary>
    /// Gets or sets the length for the file in SD/eMMC "shared2/0000".
    /// </summary>
    public int StorageShared20Length { get; set; }

    /// <summary>
    /// Gets or sets the length for the file in SD/eMMC "shared2/0001".
    /// </summary>
    public int StorageShared21Length { get; set; }

    /// <summary>
    /// Gets or sets the version of the EULA agreement.
    /// </summary>
    public byte EulaVersion { get; set; }

    /// <summary>
    /// Gets or sets the use ratings.
    /// </summary>
    public byte UseRatings { get; set; }

    /// <summary>
    /// Gets or sets the length for the file in SD/eMMC "shared2/0002".
    /// </summary>
    public int StorageShared22Length { get; set; }

    /// <summary>
    /// Gets or sets the length for the file in SD/eMMC "shared2/0003".
    /// </summary>
    public int StorageShared23Length { get; set; }

    /// <summary>
    /// Gets or sets the length for the file in SD/eMMC "shared2/0004".
    /// </summary>
    public int StorageShared24Length { get; set; }

    /// <summary>
    /// Gets or sets the length for the file in SD/eMMC "shared2/0005".
    /// </summary>
    public int StorageShared25Length { get; set; }

    /// <summary>
    /// Gets or sets the offset in the ARM9i program to the code parameters.
    /// </summary>
    public uint Arm9iParametersTableOffset { get; set; }

    /// <summary>
    /// Gets or sets the offset in the ARM7i program to the code parameters.
    /// </summary>
    public uint Arm7iParametersTableOffset { get; set; }

    /// <summary>
    /// Gets or sets the first target area to have encryption modcrypt.
    /// </summary>
    public ModcryptTargetKind ModcryptArea1Target { get; set; }

    /// <summary>
    /// Gets or sets the second target area to have encryption modcrypt.
    /// </summary>
    public ModcryptTargetKind ModcryptArea2Target { get; set; }

    /// <summary>
    /// Gets or sets the title ID of the program (similar to 3DS).
    /// </summary>
    public ulong TitleId { get; set; }

    /// <summary>
    /// Gets or sets the length of the "public.sav" file for DSiWare.
    /// </summary>
    public uint StoragePublicSaveLength { get; set; }

    /// <summary>
    /// Gets or sets the length of the "private.sav" file for DSiWare.
    /// </summary>
    public uint StoragePrivateSaveLength { get; set; }

    /// <summary>
    /// Gets or sets the age rating CERO (Japan).
    /// </summary>
    public AgeRating AgeRatingCero { get; set; }

    /// <summary>
    /// Gets or sets the age rating ESRB (US / Canada).
    /// </summary>
    public AgeRating AgeRatingEsrb { get; set; }

    /// <summary>
    /// Gets or sets the age rating USK (Germany).
    /// </summary>
    public AgeRating AgeRatingUsk { get; set; }

    /// <summary>
    /// Gets or sets the age rating PEGI (Pan-Europe).
    /// </summary>
    public AgeRating AgeRatingPegiEurope { get; set; }

    /// <summary>
    /// Gets or sets the age rating PEGI for Portugal.
    /// </summary>
    public AgeRating AgeRatingPegiPortugal { get; set; }

    /// <summary>
    /// Gets or sets the age rating PEGI and BBFC (UK).
    /// </summary>
    public AgeRating AgeRatingPegiUk { get; set; }

    /// <summary>
    /// Gets or sets the age rating AGCB (Australia).
    /// </summary>
    public AgeRating AgeRatingAgcb { get; set; }

    /// <summary>
    /// Gets or sets the age rating GRB (South Korea).
    /// </summary>
    public AgeRating AgeRatingGrb { get; set; }

    /// <summary>
    /// Gets or sets the SHA1-HMAC of the ARM9 with encrypted secure area.
    /// </summary>
    public HashInfo Arm9SecureMac { get; set; }

    /// <summary>
    /// Gets or sets the SHA1-HMAC of the ARM7.
    /// </summary>
    public HashInfo Arm7Mac { get; set; }

    /// <summary>
    /// Gets or sets the SHA1-HMAC of digest block data.
    /// </summary>
    public HashInfo DigestMain { get; set; }

    /// <summary>
    /// Gets or sets the SHA1-HMAC of the ARM9i program decrypted.
    /// </summary>
    public HashInfo Arm9iMac { get; set; }

    /// <summary>
    /// Gets or sets the SHA1-HMAC of the ARM7i program decrypted.
    /// </summary>
    public HashInfo Arm7iMac { get; set; }

    /// <summary>
    /// Gets or sets the SHA1-HMAC of the ARM9 program without secure area.
    /// </summary>
    public HashInfo Arm9Mac { get; set; }
}
