# AbstractValueObject Benchmarks

`AbstractValueObject`의 `ValueObjectEqualityComparer`에서 배열 비교/해시 패턴을 비교합니다.

## 변경 내용 (Tier 1+2+3)

| Tier | 메서드 | Before | After |
|------|--------|--------|-------|
| 1 | `Equals` (byte[]) | `Array.GetValue(int)` + boxing + 재귀 Equals | `Span<byte>.SequenceEqual` (SIMD) |
| 2 | `Equals` (sbyte/short/ushort/int/uint/long/ulong/float/double/char/bool/Guid/decimal/string) | 동 (boxing) | type-specific `Span<T>.SequenceEqual` |
| 3 | `GetHashCode` (모든 primitive array) | `foreach` + `IEnumerator` + boxing + `GetHashCode()` | `HashCode.AddBytes` (byte[]) / type-specific `HashCode.Add<T>` (no boxing) |
| - | 폴백 | 다차원/jagged/custom 객체 array | element-wise(기존 패턴 유지) |

## 실행

```bash
cd Tests.Benchmarks/AbstractValueObject.Benchmarks
dotnet run -c Release
```

## 결과 (ShortRunJob, 3 iterations × 1 launch × 3 warmup)

환경: Windows 11, Intel Core i7-1065G7, .NET 10.0.2 x64 (RyuJIT, x86-64-v4 / AVX2)

### Equals 비교 (`ArrayEqualityBenchmarks`)

| 배열 타입 | 크기 | Before | After | 속도 향상 | 메모리 |
|----------|-----:|-------:|------:|---------:|-------:|
| `byte[]`   |   16 |   466 ns |   1.3 ns |   **351×** | 768B → 0 |
| `byte[]`   |  256 | 6,904 ns |   5.9 ns | **1,170×** | 12KB → 0 |
| `byte[]`   | 4096 | 116,000 ns |  82 ns | **1,414×** | 192KB → 0 |
| `int[]`    |   16 |   424 ns |   2.4 ns |     175× | 768B → 0 |
| `int[]`    |  256 | 6,292 ns |    24 ns |     262× | 12KB → 0 |
| `int[]`    | 4096 | 100,200 ns |   288 ns |     348× | 192KB → 0 |
| `long[]`   |   16 |   421 ns |   2.6 ns |     162× | 768B → 0 |
| `long[]`   |  256 | 6,532 ns |    41 ns |     159× | 12KB → 0 |
| `long[]`   | 4096 | 100,500 ns | 1,453 ns |      69× | 192KB → 0 |
| `Guid[]`   |   16 |   564 ns |    17 ns |      33× | 1KB → 0 |
| `Guid[]`   |  256 | 8,934 ns |   250 ns |      36× | 16KB → 0 |
| `Guid[]`   | 4096 | 146,300 ns | 4,409 ns |      33× | 256KB → 0 |
| `string[]` |   16 |    96 ns |    24 ns |     4.0× | 0 → 0 |
| `string[]` |  256 | 1,240 ns |   322 ns |     3.9× | 0 → 0 |
| `string[]` | 4096 | 20,870 ns | 6,514 ns |     3.2× | 0 → 0 |

### GetHashCode 비교 (`ArrayHashCodeBenchmarks`)

| 배열 타입 | 크기 | Before | After | 속도 향상 | 메모리 |
|----------|-----:|-------:|------:|---------:|-------:|
| `byte[]`  |   16 |   246 ns |   9 ns |     **27×** | 416B → 0 |
| `byte[]`  |  256 | 3,057 ns |  69 ns |     **44×** | 6KB → 0 |
| `byte[]`  | 4096 | 47,500 ns | 901 ns |     **53×** | 96KB → 0 |
| `int[]`   |   16 |   201 ns |  30 ns |       7× | 416B → 0 |
| `int[]`   |  256 | 2,767 ns | 437 ns |       6× | 6KB → 0 |
| `int[]`   | 4096 | 51,975 ns | 6,614 ns |     8× | 96KB → 0 |
| `Guid[]`  |   16 |   242 ns |  54 ns |     4.5× | 544B → 0 |
| `Guid[]`  |  256 | 4,928 ns | 722 ns |       7× | 8KB → 0 |
| `Guid[]`  | 4096 | 57,400 ns | 10,470 ns | 5.5× | 128KB → 0 |

## 핵심 효과

- **byte[]**: SIMD(AVX2) 가속으로 4KB 비교가 **116μs → 82ns** — **1400배** 향상
- **모든 primitive array**: boxing 100% 제거 → 매 비교당 GC pressure **0**
- **string[]**: reference equality 기반이라 boxing은 없으나 element-by-element 호출 vs `EqualityComparer<string>.Default` 직접 호출로 3-4배 향상
- **다차원/jagged/custom 객체 array**: 기존 폴백 경로 유지(behavior 변화 없음)

## 의의

`byte[]` 4KB 비교를 매 요청 1회 한다고 가정하면:
- 이전: **116μs + 192KB GC pressure** × 요청 수
- 신규: **82ns + 0 byte allocation** × 요청 수

24시간 365일 가동 시스템에서 GC pressure 0은 GC pause 빈도와 tail-latency 안정성에 직접 기여합니다.
