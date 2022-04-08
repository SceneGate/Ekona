using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SceneGate.Ekona
{
    /// <summary>
    /// Information of a data hash.
    /// </summary>
    public class HashInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HashInfo"/>  class.
        /// </summary>
        /// <param name="algoName">The name of the hashing algorithm.</param>
        /// <param name="hash">The hash data.</param>
        public HashInfo(string algoName, byte[] hash)
        {
            AlgorithmName = algoName;
            Hash = hash;
            Status = HashStatus.NotValidated;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HashInfo"/>  class.
        /// </summary>
        /// <param name="algoName">The name of the hashing algorithm.</param>
        /// <param name="hash">The hash data.</param>
        /// <param name="isValid">Value indicating whether this signature is valid.</param>
        public HashInfo(string algoName, byte[] hash, bool isValid)
            : this(algoName, hash)
        {
            Status = isValid ? HashStatus.Valid : HashStatus.Invalid;
        }

        /// <summary>
        /// Gets the hashing algorithm name.
        /// </summary>
        public string AlgorithmName { get; }

        /// <summary>
        /// Gets the hash.
        /// </summary>
        public byte[] Hash { get; }

        /// <summary>
        /// Gets a value indicating whether the hash is null.
        /// </summary>
        public bool IsNull => Hash.All(x => x == 0);

        /// <summary>
        /// Gets or sets the status of the hash.
        /// </summary>
        public HashStatus Status { get; set; }
    }
}
