using System.Data.HashFunction;
using System.Data.HashFunction.CRC;
using System.Linq;
using System.Security.Cryptography;
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

        /// <summary>
        /// Read a SHA-1 HMAC.
        /// </summary>
        /// <param name="reader">The data reader with the stream to read the HMAC.</param>
        /// <returns>The information about the HMAC verification.</returns>
        public static HMACInfo ReadHMACSHA1(this DataReader reader)
        {
            byte[] hmac = reader.ReadBytes(0x14);
            return new HMACInfo("HMACSHA1", hmac);
        }

        /// <summary>
        /// Read a RSA-SHA1 signature with the provided padding scheme.
        /// </summary>
        /// <param name="reader">The data reader with the stream to read the signature.</param>
        /// <returns>The information about the signature.</returns>
        public static SignatureInfo ReadSignatureSHA1RSA(this DataReader reader)
        {
            byte[] signature = reader.ReadBytes(128);
            return new SignatureInfo(HashAlgorithmName.SHA1, null, signature);
        }

        /// <summary>
        /// Compute a CRC16 over the specific substream and write the result.
        /// </summary>
        /// <param name="writer">Write to write the result and get the stream.</param>
        /// <param name="offset">Offset of the segment to calculate the CRC.</param>
        /// <param name="length">The length to calculate the CRC.</param>
        public static void WriteComputedCrc16(this DataWriter writer, long offset, long length)
        {
            using DataStream segment = new DataStream(writer.Stream, offset, length);

            ICRC crc = CRCFactory.Instance.Create(CRCConfig.MODBUS);
            IHashValue hash = crc.ComputeHash(segment);
            ushort actual = (ushort)(hash.Hash[0] | (hash.Hash[1] << 8));

            writer.Write(actual);
        }
    }
}
