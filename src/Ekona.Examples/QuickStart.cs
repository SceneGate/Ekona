// Copyright (c) 2022 SceneGate

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
using SceneGate.Ekona.Containers.Rom;
using Texim.Animations;
using Texim.Formats;
using Texim.Images;
using Yarhl.FileFormat;
using Yarhl.FileSystem;
using Yarhl.IO;
using Yarhl.Media.Text;

namespace SceneGate.Ekona.Examples;

public class QuickStart
{
    public void OpenWriteGame(string gameFilePath)
    {
        #region OpenGame
        // Create node from file with binary format.
        Node game = NodeFactory.FromFile(gameFilePath, FileOpenMode.Read);

        // Use the `Binary2NitroRom` converter to convert the binary format
        // into node containers (virtual file system tree).
        game.TransformWith<Binary2NitroRom>();

        // And it's done!
        // Now we can access to every game file. For instance, we can export one file
        Node gameFile = game.Children["data"].Children["Items.dat"];
        gameFile.Stream.WriteTo("dump/items.dat");
        #endregion

        #region ModifyFile
        // Read the file by converting from binary format into PO (translation format)
        Node itemsFile = Navigator.SearchNode(game, "data/Items.dat");
        Po items = itemsFile.TransformWith<BinaryItems2Po>().GetFormatAs<Po>();

        // Let's modify the first entry
        items.Entries[0].Translated = "Hello world!";

        // Convert back from PO format into binary (write into a new memory stream)
        itemsFile.TransformWith<Po2BinaryItems>();
        #endregion

        #region WriteGame
        game.TransformWith<NitroRom2Binary>();
        game.Stream.WriteTo("output/new_game.nds");
        #endregion
    }

    public void AccessHeaderInfo(string gameFilePath)
    {
        #region HeaderInfo
        Node game = NodeFactory.FromFile(gameFilePath, FileOpenMode.Read)
            .TransformWith<Binary2NitroRom>();

        ProgramInfo info = game.Children["system"].Children["info"]
            .GetFormatAs<ProgramInfo>();

        Console.WriteLine($"Game title: {info.GameTitle} [{info.GameCode}]");
        #endregion

        #region BannerTitle
        Banner bannerInfo = Navigator.SearchNode(game, "system/banner/info").GetFormatAs<Banner>();
        Console.WriteLine($"Japanese title: {bannerInfo.JapaneseTitle}");
        Console.WriteLine($"English title: {bannerInfo.EnglishTitle}");
        #endregion

        #region ExportIcon
        IndexedPaletteImage icon = Navigator.SearchNode(game, "system/banner/icon")
            .GetFormatAs<IndexedPaletteImage>();

        // Using Texim converters, create a PNG image
        IndexedImage2Bitmap bitmapConverter = new IndexedImage2Bitmap();
        bitmapConverter.Initialize(new IndexedImageBitmapParams { Palettes = icon });

        using BinaryFormat binaryPng = bitmapConverter.Convert(icon);
        binaryPng.Stream.WriteTo("dump/icon.png");

        // For DSi-enhanced games we can export its animated icon as GIF
        if (bannerInfo.SupportAnimatedIcon) {
            Node animatedNode = Navigator.SearchNode(game, "system/banner/animated");
            var animatedImage = ConvertFormat.With<IconAnimation2AnimatedImage>(animatedNode.Format);
            using var binaryGif = (BinaryFormat)ConvertFormat.With<AnimatedFullImage2Gif>(animatedImage);
            binaryGif.Stream.WriteTo("dump/icon.gif");
        }
        #endregion
    }

    private sealed class BinaryItems2Po : IConverter<IBinary, Po>
    {
        public Po Convert(IBinary source) => new Po();
    }

    private sealed class Po2BinaryItems : IConverter<Po, BinaryFormat>
    {
        public BinaryFormat Convert(Po source) => new BinaryFormat();
    }
}
