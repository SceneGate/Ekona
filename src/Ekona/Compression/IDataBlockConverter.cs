namespace SceneGate.Ekona.Compression;

using System;

/// <summary>
/// Interface to perform conversion of data in iterative blocks.
/// </summary>
/// <typeparam name="TSrc">Type of the input data.</typeparam>
/// <typeparam name="TDst">Type of the destination data.</typeparam>
public interface IDataBlockConverter<TSrc, TDst>
{
    /// <summary>
    /// Gets the maximum size of the output for a given input length.
    /// It can be used to allocate the buffer to use in the conversion.
    /// </summary>
    /// <param name="inputLength">Size of the input buffer to convert.</param>
    /// <returns>Maximum length needed in the output buffer.</returns>
    int GetOutputMaxCount(int inputLength);

    /// <summary>
    /// Converts the next iteration of the input data.
    /// </summary>
    /// <param name="input">Buffer with data to process.</param>
    /// <param name="output">Buffer to write the output.</param>
    /// <param name="consumed">Amount of bytes read from the input.</param>
    /// <returns>Amount of bytes written in the output.</returns>
    int Convert(ReadOnlySpan<TSrc> input, Span<TDst> output, out int consumed);
}
