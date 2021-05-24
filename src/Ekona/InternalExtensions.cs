using System.Data.HashFunction;
using System.Data.HashFunction.CRC;
using Yarhl.IO;

namespace SceneGate.Ekona
{
    /// <summary>
    /// Internal method extensions.
    /// </summary>
    internal static class InternalExtensions
    {
        /// <summary>
        /// Read a CRC-16 value and verify it matches the region of data.
        /// </summary>
        /// <param name="reader">The data reader with the stream to verify.</param>
        /// <param name="offset">The offset of the region of data where the CRC-16 applies.</param>
        /// <param name="length">The length of the region of data.</param>
        /// <returns>The information about the checksum verification.</returns>
        public static ChecksumInfo<ushort> ValidateCrc16(this DataReader reader, long offset, long length)
        {
            ushort expected = reader.ReadUInt16();
            using DataStream segment = DataStreamFactory.FromStream(reader.Stream, offset, length);

            ICRC crc = CRCFactory.Instance.Create(CRCConfig.MODBUS);
            IHashValue hash = crc.ComputeHash(segment);
            ushort actual = (ushort)(hash.Hash[0] | (hash.Hash[1] << 8));

            return new ChecksumInfo<ushort> {
                Expected = expected,
                Actual = actual,
            };
        }
    }
}
