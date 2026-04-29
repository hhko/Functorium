using BenchmarkDotNet.Attributes;

namespace AbstractValueObject.Benchmarks;

/// <summary>
/// AbstractValueObject 내부 ValueObjectEqualityComparer의 배열 비교 패턴을 비교합니다.
///
/// Old: Array.GetValue(int) + boxing + 재귀 Equals (현재 코드 패턴)
/// New: type-specific Span&lt;T&gt;.SequenceEqual (SIMD 가속)
///
/// byte[]·int[]·long[]·Guid[]·string[] 다양한 크기에서 측정합니다.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class ArrayEqualityBenchmarks
{
    [Params(16, 256, 4096)]
    public int ArraySize { get; set; }

    private byte[] _byteA = null!;
    private byte[] _byteB = null!;
    private int[] _intA = null!;
    private int[] _intB = null!;
    private long[] _longA = null!;
    private long[] _longB = null!;
    private Guid[] _guidA = null!;
    private Guid[] _guidB = null!;
    private string[] _stringA = null!;
    private string[] _stringB = null!;

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);

        _byteA = new byte[ArraySize];
        rng.NextBytes(_byteA);
        _byteB = (byte[])_byteA.Clone();

        _intA = new int[ArraySize];
        for (int i = 0; i < ArraySize; i++) _intA[i] = rng.Next();
        _intB = (int[])_intA.Clone();

        _longA = new long[ArraySize];
        for (int i = 0; i < ArraySize; i++) _longA[i] = rng.NextInt64();
        _longB = (long[])_longA.Clone();

        _guidA = new Guid[ArraySize];
        for (int i = 0; i < ArraySize; i++) _guidA[i] = Guid.NewGuid();
        _guidB = (Guid[])_guidA.Clone();

        _stringA = new string[ArraySize];
        for (int i = 0; i < ArraySize; i++) _stringA[i] = $"item_{i}";
        _stringB = (string[])_stringA.Clone();
    }

    // ─── byte[] ────────────────────────────────────────

    [Benchmark(Baseline = true), BenchmarkCategory("byte[]")]
    public bool Old_ByteArray() => OldArrayEquals(_byteA, _byteB);

    [Benchmark, BenchmarkCategory("byte[]")]
    public bool New_ByteArray() => _byteA.AsSpan().SequenceEqual(_byteB);

    // ─── int[] ────────────────────────────────────────

    [Benchmark, BenchmarkCategory("int[]")]
    public bool Old_IntArray() => OldArrayEquals(_intA, _intB);

    [Benchmark, BenchmarkCategory("int[]")]
    public bool New_IntArray() => _intA.AsSpan().SequenceEqual(_intB);

    // ─── long[] ───────────────────────────────────────

    [Benchmark, BenchmarkCategory("long[]")]
    public bool Old_LongArray() => OldArrayEquals(_longA, _longB);

    [Benchmark, BenchmarkCategory("long[]")]
    public bool New_LongArray() => _longA.AsSpan().SequenceEqual(_longB);

    // ─── Guid[] ───────────────────────────────────────

    [Benchmark, BenchmarkCategory("Guid[]")]
    public bool Old_GuidArray() => OldArrayEquals(_guidA, _guidB);

    [Benchmark, BenchmarkCategory("Guid[]")]
    public bool New_GuidArray() => _guidA.AsSpan().SequenceEqual(_guidB);

    // ─── string[] ─────────────────────────────────────

    [Benchmark, BenchmarkCategory("string[]")]
    public bool Old_StringArray() => OldArrayEquals(_stringA, _stringB);

    [Benchmark, BenchmarkCategory("string[]")]
    public bool New_StringArray() => _stringA.AsSpan().SequenceEqual(_stringB);

    // ─── 기존 패턴 (GetValue + boxing + 재귀 Equals) ───────

    private static bool OldArrayEquals(Array xArray, Array yArray)
    {
        if (xArray.Length != yArray.Length)
            return false;

        for (int i = 0; i < xArray.Length; i++)
        {
            object? xv = xArray.GetValue(i);
            object? yv = yArray.GetValue(i);

            if (xv is null && yv is null) continue;
            if (xv is null || yv is null) return false;
            if (!xv.Equals(yv)) return false;
        }
        return true;
    }
}
