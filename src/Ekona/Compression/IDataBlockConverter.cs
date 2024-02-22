namespace SceneGate.Ekona.Compression;

using System;

public interface IDataBlockConverter<TSrc, TDst>
{
    int GetOutputMaxCount(int inputLength);

    int Convert(ReadOnlySpan<TSrc> input, Span<TDst> output, out int consumed);
}
