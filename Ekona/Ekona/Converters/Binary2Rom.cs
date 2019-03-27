namespace Ekona.Converters
{
    using Yarhl.FileFormat;
    using Ekona.Formats;
    using System;
    using Yarhl.IO;
    using Yarhl.FileSystem;

    public class Binary2Rom : 
        IConverter<BinaryFormat, Rom>,
        IConverter<Rom, BinaryFormat>
    {
        /// <summary>
        /// Read the internal info of a ROM file.
        /// </summary>
        /// <param name="str">Stream to read from.</param>
        public Rom Convert(BinaryFormat source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            DataStream str = source.Stream;

            Rom rom = new Rom();

            // Read header
            str.Seek(0, SeekMode.Start);
            rom.Header = new RomHeader();
            rom.Header.Read(str);

            // Read banner
            var bannerStream = new DataStream(str, rom.Header.BannerOffset, Banner.Size);
            var bannerFile = new Node("Banner", new BinaryFormat(bannerStream));
            rom.Banner = new Banner();
            rom.Banner.Initialize(bannerFile);
            rom.Banner.Read();

            // Read file system: FAT and FNT
            rom.FileSystem = new FileSystem();
            rom.FileSystem.Initialize(null, rom.Header);
            rom.FileSystem.Read(str);

            // Assign common tags (they will be assigned recursively)
            rom.File.Tags["_Device_"] = "NDS";
            rom.File.Tags["_MakerCode_"] = rom.Header.MakerCode;
            rom.File.Tags["_GameCode_"] = rom.Header.GameCode;

            // Get the ROM folders with files and system files.
            rom.FileSystem.SystemFolder.AddFile(bannerFile);

            rom.File.AddFolder(rom.FileSystem.Root);
            rom.File.AddFolder(rom.FileSystem.SystemFolder);

            return rom;
        }

        /// <summary>
        /// Write a new ROM data.
        /// </summary>
        /// <param name="str">Stream to write to.</param>
        public BinaryFormat Convert(Rom source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            BinaryFormat binary = new BinaryFormat();

            DataStream headerStr = new DataStream(new System.IO.MemoryStream(), 0, 0);
            DataStream fileSysStr = new DataStream(new System.IO.MemoryStream(), 0, 0);
            DataStream bannerStr = new DataStream(new System.IO.MemoryStream(), 0, 0);

            source.FileSystem.Write(fileSysStr);

            //source.Banner.UpdateCrc();
            source.Banner.Write(bannerStr);

            source.Header.BannerOffset = (uint)(source.Header.HeaderSize + fileSysStr.Length);
            //source.Header.UpdateCrc();
            source.Header.Write(headerStr);

            headerStr.WriteTo(binary.Stream);
            fileSysStr.WriteTo(binary.Stream);
            bannerStr.WriteTo(binary.Stream);
            source.FileSystem.WriteFiles(binary.Stream);
            binary.Stream.WriteUntilLength(FileSystem.PaddingByte, (int)source.Header.CartridgeSize);

            headerStr.Dispose();
            fileSysStr.Dispose();
            bannerStr.Dispose();

            return binary;
        }

    }
}
