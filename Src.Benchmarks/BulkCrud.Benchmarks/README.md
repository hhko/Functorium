# Bulk CRUD Performance Improvement Benchmark Results

## Environment

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26100.7840/24H2/2024Update/HudsonValley)
Intel Core i7-1065G7 CPU 1.30GHz (Max: 1.50GHz), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.102
  [Host]   : .NET 10.0.2 (10.0.2, 10.0.225.61305), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.2 (10.0.2, 10.0.225.61305), X64 RyuJIT x86-64-v4
```

## Performance Improvement Summary

| Operation | Before (100K) | After (100K) | Speedup | Memory Reduction |
|-----------|--------------|-------------|---------|-----------------|
| DomainEventCollector.Track | 5,847 ms (O(n^2)) | 11.2 ms (O(n)) | **~523x** | **45%** |
| InMemory Create | 403 ms | 123 ms | **~3.3x** | **85%** |
| InMemory Read | 274 ms | 40 ms | **~7x** | **98%** |
| InMemory Update | 121 ms | 38 ms | **~3x** | **94%** |
| InMemory Delete | 92 ms | 11 ms | **~9x** | **99.9%** |
| EF Core Create (10K) | 245,536 ms (4.1 min) | 1,054 ms | **~233x** | **99.4%** |

## Detailed Benchmark Results

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

### InMemory Create: SingleCreate Loop vs CreateRange Bulk

| Method | Count | Mean | Ratio | Allocated | Alloc Ratio |
|--------|------:|-----:|------:|----------:|------------:|
| **SingleCreate Loop** | **1,000** | **11.405 ms** | **1.12** | **1,443.95 KB** | **1.00** |
| CreateRange Bulk | 1,000 | 4.379 ms | 0.43 | 228.38 KB | 0.16 |
| | | | | | |
| **SingleCreate Loop** | **10,000** | **66.753 ms** | **1.01** | **13,566.93 KB** | **1.00** |
| CreateRange Bulk | 10,000 | 10.477 ms | 0.16 | 2,900.41 KB | 0.21 |
| | | | | | |
| **SingleCreate Loop** | **100,000** | **403.114 ms** | **1.00** | **130,156.66 KB** | **1.00** |
| CreateRange Bulk | 100,000 | 122.670 ms | 0.30 | 19,970.69 KB | 0.15 |

### InMemory Read: GetById Loop vs GetByIds Bulk

| Method | Count | Mean | Ratio | Allocated | Alloc Ratio |
|--------|------:|-----:|------:|----------:|------------:|
| **GetById Loop** | **1,000** | **2,491.0 us** | **1.00** | **1,117.3 KB** | **1.00** |
| GetByIds Bulk | 1,000 | 127.7 us | 0.05 | 24.95 KB | 0.02 |
| | | | | | |
| **GetById Loop** | **10,000** | **27,826.8 us** | **1.00** | **11,171.98 KB** | **1.00** |
| GetByIds Bulk | 10,000 | 2,117.1 us | 0.08 | 235.89 KB | 0.02 |
| | | | | | |
| **GetById Loop** | **100,000** | **274,260.8 us** | **1.01** | **111,718.86 KB** | **1.00** |
| GetByIds Bulk | 100,000 | 39,941.9 us | 0.15 | 2,345.27 KB | 0.02 |

### InMemory Update: SingleUpdate Loop vs UpdateRange Bulk

| Method | Count | Mean | Ratio | Allocated | Alloc Ratio |
|--------|------:|-----:|------:|----------:|------------:|
| **SingleUpdate Loop** | **1,000** | **905.5 us** | **1.00** | **1,188.67 KB** | **1.00** |
| UpdateRange Bulk | 1,000 | 154.8 us | 0.17 | 88.4 KB | 0.07 |
| | | | | | |
| **SingleUpdate Loop** | **10,000** | **11,398.6 us** | **1.00** | **11,829.22 KB** | **1.00** |
| UpdateRange Bulk | 10,000 | 2,847.7 us | 0.25 | 815.23 KB | 0.07 |
| | | | | | |
| **SingleUpdate Loop** | **100,000** | **120,814.3 us** | **1.01** | **117,614.91 KB** | **1.00** |
| UpdateRange Bulk | 100,000 | 38,271.5 us | 0.32 | 7,460 KB | 0.06 |

### InMemory Delete: SingleDelete Loop vs DeleteRange Bulk

| Method | Count | Mean | Ratio | Allocated | Alloc Ratio |
|--------|------:|-----:|------:|----------:|------------:|
| **SingleDelete Loop** | **1,000** | **1,343.0 us** | **1.09** | **1,196.48 KB** | **1.000** |
| DeleteRange Bulk | 1,000 | 206.8 us | 0.17 | 1.27 KB | 0.001 |
| | | | | | |
| **SingleDelete Loop** | **10,000** | **16,068.8 us** | **1.00** | **11,907.32 KB** | **1.000** |
| DeleteRange Bulk | 10,000 | 908.8 us | 0.06 | 1.27 KB | 0.000 |
| | | | | | |
| **SingleDelete Loop** | **100,000** | **91,898.9 us** | **1.00** | **118,396.16 KB** | **1.000** |
| DeleteRange Bulk | 100,000 | 10,757.1 us | 0.12 | 1.27 KB | 0.000 |

### EF Core Create: SingleAdd+SaveChanges vs AddRange+SaveChanges (SQLite)

| Method | Count | Mean | Ratio | Allocated | Alloc Ratio |
|--------|------:|-----:|------:|----------:|------------:|
| **SingleAdd + SaveChanges Loop** | **1,000** | **14,259.1 ms** | **1.00** | **245.55 MB** | **1.00** |
| AddRange + Single SaveChanges | 1,000 | 326.2 ms | 0.02 | 13.92 MB | 0.06 |
| AddRange via Repository | 1,000 | 310.0 ms | 0.02 | 13.96 MB | 0.06 |
| | | | | | |
| **SingleAdd + SaveChanges Loop** | **10,000** | **245,535.7 ms** | **1.001** | **22,702.71 MB** | **1.000** |
| AddRange + Single SaveChanges | 10,000 | 1,053.5 ms | 0.004 | 126.99 MB | 0.006 |
| AddRange via Repository | 10,000 | 1,160.4 ms | 0.005 | 127.78 MB | 0.006 |

### Performance Tests (All Pass)

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

### InMemory Repository Bulk CRUD

Bulk methods (`CreateRange`, `GetByIds`, `UpdateRange`, `DeleteRange`) eliminate per-item IO monad overhead by wrapping the entire collection operation in a single `IO.lift()` call.

| Operation | 100K Speedup | 100K Memory Reduction |
|-----------|-------------|----------------------|
| Create | ~3.3x | 85% |
| Read | ~7x | 98% |
| Update | ~3x | 94% |
| Delete | ~9x | 99.9% |

### EF Core Repository Bulk Create (SQLite)

The most dramatic improvement in real-world I/O scenarios:

- **1K items**: SingleAdd+SaveChanges = 14.3s, AddRange+SaveChanges = 326ms (**44x faster**)
- **10K items**: SingleAdd+SaveChanges = 4.1 min, AddRange+SaveChanges = 1.1s (**233x faster**)
- Memory: 99.4% reduction at 10K items (22.7 GB -> 127 MB)

`AddRange via Repository` performs nearly identically to raw `AddRange + SaveChanges`, confirming that the Repository abstraction layer adds negligible overhead.

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
dotnet run -c Release -- --filter '*InMemoryReadBenchmarks*'
dotnet run -c Release -- --filter '*InMemoryUpdateBenchmarks*'
dotnet run -c Release -- --filter '*InMemoryDeleteBenchmarks*'
dotnet run -c Release -- --filter '*EfCoreBulkBenchmarks*'
```
