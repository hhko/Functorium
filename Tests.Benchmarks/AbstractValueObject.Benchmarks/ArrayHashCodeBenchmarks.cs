using BenchmarkDotNet.Attributes;

namespace AbstractValueObject.Benchmarks;

/// <summary>
/// AbstractValueObject 내부 ValueObjectEqualityComparer의 배열 해시 패턴을 비교합니다.
///
/// Old: foreach (var item in array) → IEnumerator → boxing → item.GetHashCode()
/// New: type-specific HashCode.AddBytes / generic Add&lt;T&gt; (boxing 회피)
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class ArrayHashCodeBenchmarks
{
    [Params(16, 256, 4096)]
    public int ArraySize { get; set; }

    private byte[] _bytes = null!;
    private int[] _ints = null!;
    private Guid[] _guids = null!;

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);

        _bytes = new byte[ArraySize];
        rng.NextBytes(_bytes);

        _ints = new int[ArraySize];
        for (int i = 0; i < ArraySize; i++) _ints[i] = rng.Next();

        _guids = new Guid[ArraySize];
        for (int i = 0; i < ArraySize; i++) _guids[i] = Guid.NewGuid();
    }

    // ─── byte[] ────────────────────────────────────────

    [Benchmark(Baseline = true), BenchmarkCategory("byte[]")]
    public int Old_ByteArrayHash() => OldArrayHash(_bytes);

    [Benchmark, BenchmarkCategory("byte[]")]
    public int New_ByteArrayHash()
    {
        var hc = new HashCode();
        hc.AddBytes(_bytes);
        return hc.ToHashCode();
    }

    // ─── int[] ────────────────────────────────────────

    [Benchmark, BenchmarkCategory("int[]")]
    public int Old_IntArrayHash() => OldArrayHash(_ints);

    [Benchmark, BenchmarkCategory("int[]")]
    public int New_IntArrayHash()
    {
        var hc = new HashCode();
        foreach (int i in _ints) hc.Add(i);
        return hc.ToHashCode();
    }

    // ─── Guid[] ───────────────────────────────────────

    [Benchmark, BenchmarkCategory("Guid[]")]
    public int Old_GuidArrayHash() => OldArrayHash(_guids);

    [Benchmark, BenchmarkCategory("Guid[]")]
    public int New_GuidArrayHash()
    {
        var hc = new HashCode();
        foreach (Guid g in _guids) hc.Add(g);
        return hc.ToHashCode();
    }

    // ─── 기존 패턴 (foreach + boxing + GetHashCode) ───────

    private static int OldArrayHash(Array array)
    {
        unchecked
        {
            int hash = 17;
            foreach (var item in array)
            {
                hash = hash * 23 + (item?.GetHashCode() ?? 0);
            }
            return hash;
        }
    }
}
