using System;

namespace SceneGate.Ekona
{
    /// <summary>
    /// Information for a checksum verification.
    /// </summary>
    /// <typeparam name="T">The checksum type, i.e. ushort for CRC-16.</typeparam>
    public class ChecksumInfo<T>
        where T : IEquatable<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChecksumInfo{T}"/> class.
        /// </summary>
        public ChecksumInfo()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChecksumInfo{T}"/> class.
        /// </summary>
        /// <param name="actual">The actual checksum of the stream.</param>
        /// <param name="expected">The expected checksum of the stream.</param>
        public ChecksumInfo(T actual, T expected)
        {
            Actual = actual;
            Expected = expected;
        }

        /// <summary>
        /// Gets the actual checksum of the stream.
        /// </summary>
        public T Actual { get; init; }

        /// <summary>
        /// Gets the expected checksum of the stream.
        /// </summary>
        public T Expected { get; init; }

        /// <summary>
        /// Gets a value indicating whether the stream match the expected checksum.
        /// </summary>
        public bool IsValid => Actual.Equals(Expected);
    }
}
