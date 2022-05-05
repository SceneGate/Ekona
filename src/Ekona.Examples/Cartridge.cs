using SceneGate.Ekona.Containers.Rom;
using SceneGate.Ekona.Security;
using Texim.Animations;
using Texim.Formats;
using Yarhl.FileFormat;
using Yarhl.FileSystem;
using Yarhl.IO;

namespace SceneGate.Ekona.Examples;

public class Cartridge
{
    public void DecryptEncryptArm9(Node rom, DsiKeyStore keyStore)
    {
        #region DecryptEncryptArm9
        var programInfo = Navigator.SearchNode(rom, "system/info").GetFormatAs<ProgramInfo>();
        var key1Encryption = new NitroKey1Encryption(programInfo.GameCode, keyStore);

        DataStream arm9 = Navigator.SearchNode(rom, "system/arm9").Stream!;
        bool isEncrypted = key1Encryption.HasEncryptedArm9(arm9);
        if (isEncrypted) {
            DataStream decryptedArm9 = key1Encryption.DecryptArm9(arm9);
        } else {
            DataStream encryptedArm9 = key1Encryption.EncryptArm9(arm9);
        }
        #endregion
    }

    public void ExportGif(Node rom)
    {
        #region ExportIconGif
        Node animated = Navigator.SearchNode(rom, "system/banner/animated");

        AnimatedFullImage animatedImage = (AnimatedFullImage)ConvertFormat.With<IconAnimation2AnimatedImage>(animated.Format);
        using var gifData = (BinaryFormat)ConvertFormat.With<AnimatedFullImage2Gif>(animatedImage);

        gifData.Stream.WriteTo("icon.gif");
        #endregion
    }
}
