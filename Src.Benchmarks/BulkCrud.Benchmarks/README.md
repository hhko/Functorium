# Bulk CRUD Performance Improvement Benchmark Results

## Environment

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26100.7840/24H2/2024Update/HudsonValley)
Intel Core i7-1065G7 CPU 1.30GHz (Max: 1.50GHz), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.102
  [Host]   : .NET 10.0.2 (10.0.2, 10.0.225.61305), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.2 (10.0.2, 10.0.225.61305), X64 RyuJIT x86-64-v4
```

## Results Summary

### DomainEventCollector: List+Any O(n^2) vs HashSet O(n)

| Method | Count | Mean | Ratio | Allocated | Alloc Ratio |
|--------|------:|-----:|------:|----------:|------------:|
| **HashSet Track (O(n))** | **1,000** | **43.52 us** | **0.08** | **71.44 KB** | **0.70** |
| HashSet TrackRange (O(n)) | 1,000 | 42.33 us | 0.07 | 71.48 KB | 0.70 |
| List+Any Track (O(n^2)) | 1,000 | 570.40 us | 1.00 | 102.15 KB | 1.00 |
| | | | | | |
| **HashSet Track (O(n))** | **10,000** | **975.58 us** | **0.004** | **657.31 KB** | **0.59** |
| HashSet TrackRange (O(n)) | 10,000 | 1,089.77 us | 0.005 | 657.35 KB | 0.59 |
| List+Any Track (O(n^2)) | 10,000 | 220,668.62 us | 1.00 | 1,115.68 KB | 1.00 |
| | | | | | |
| **HashSet Track (O(n))** | **100,000** | **11,179.67 us** | **0.002** | **5,897.48 KB** | **0.55** |
| HashSet TrackRange (O(n)) | 100,000 | 12,176.20 us | 0.002 | 5,896.37 KB | 0.55 |
| List+Any Track (O(n^2)) | 100,000 | 5,847,864.90 us | 1.00 | 10,642.13 KB | 1.00 |

### Performance Improvement Summary

| Item | Before (100K) | After (100K) | Speedup | Memory Reduction |
|------|--------------|-------------|---------|-----------------|
| DomainEventCollector.Track | 5,847 ms (O(n^2)) | 11.2 ms (O(n)) | **~523x** | **45%** |
| DomainEventCollector.TrackRange | N/A | 12.2 ms (O(n)) | - | **45%** |

### InMemory & EF Core Bulk CRUD (Performance Tests)

All 11 performance tests pass, validating that bulk operations are faster than single-item loops:

```
Tests passed: 11/11

DomainEventCollectorPerfTests:
  - TrackRange_100K_Items_Completes_Under_100ms          PASS
  - Track_Deduplication_Works_With_HashSet                PASS
  - TrackRange_Followed_By_Collect_Returns_Events         PASS

InMemoryBulkCrudPerfTests:
  - CreateRange_100K_Products_Under_500ms                 PASS
  - GetByIds_100K_Products_Under_500ms                    PASS
  - UpdateRange_100K_Products_Under_500ms                 PASS
  - DeleteRange_100K_Products_Under_500ms                 PASS
  - SingleCreate_Loop_Is_Slower_Than_CreateRange           PASS

EfCoreBulkCrudPerfTests:
  - EfCore_AddRange_10K_Is_Faster_Than_SingleAdd_Loop     PASS
  - EfCore_ExecuteDeleteAsync_Faster_Than_LoadAndRemove   PASS
  - EfCore_CreateRange_Repository_Correctness             PASS
```

## Analysis

### DomainEventCollector (Critical Hotspot)

The most impactful optimization. The original `List<T> + Any(ReferenceEquals)` approach has O(n^2) complexity for tracking aggregates — each `Track()` call scans the entire list for duplicates.

- At **1K items**: 13x faster (570 us -> 43 us)
- At **10K items**: 226x faster (220 ms -> 976 us)
- At **100K items**: 523x faster (5.8 seconds -> 11 ms)

The replacement `HashSet<ReferenceEqualityComparer>` achieves O(1) per-item tracking with O(n) total.

### InMemory Repository Bulk Operations

Bulk methods (`CreateRange`, `GetByIds`, `UpdateRange`, `DeleteRange`) eliminate per-item IO monad overhead by wrapping the entire collection operation in a single `IO.lift()` call. Performance tests confirm all 100K-item operations complete under 500ms.

### EF Core Repository Bulk Operations

- **CreateRange**: Uses `AddRange()` (single change tracker registration) instead of individual `Add()` calls
- **DeleteRange**: Uses `ExecuteUpdateAsync` (single SQL UPDATE) instead of load-modify-save per entity
- **GetByIds**: Uses `WHERE IN` (single query) instead of N individual `GetById` queries

### PageRequest MaxPageSize

Changed from 100 to 10,000, reducing required pages for 100K items from 1,000 to 10.

### Streaming Support

Added `IAsyncEnumerable<TDto> Stream()` to `IQueryPort` with Dapper `QueryUnbufferedAsync` implementation, enabling memory-efficient processing of large result sets without loading all items into memory.

## How to Run

### Performance Tests

```bash
dotnet test --project Src.Benchmarks/BulkCrud.Benchmarks/BulkCrud.Benchmarks.csproj
```

### BenchmarkDotNet Benchmarks

```bash
cd Src.Benchmarks/BulkCrud.Benchmarks.Runner

# All benchmarks
dotnet run -c Release -- --filter '*'

# Specific benchmark
dotnet run -c Release -- --filter '*DomainEventCollectorBenchmarks*'
dotnet run -c Release -- --filter '*InMemoryCreateBenchmarks*'
dotnet run -c Release -- --filter '*EfCoreBulkBenchmarks*'
```
