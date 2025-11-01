# IList Sorting Performance Benchmarks

Unity Helpers ships several custom sorting algorithms for `IList<T>` that cover different trade-offs between adaptability, allocation patterns, and stability. This page gathers context and benchmark snapshots so you can choose the right algorithm for your workload and compare results across operating systems.

## Algorithm Cheatsheet

| Algorithm                   | Stable? | Best For                                                                   | Reference                                                                       |
| --------------------------- | ------- | -------------------------------------------------------------------------- | ------------------------------------------------------------------------------- |
| Ghost Sort                  | No      | Mixed workloads that benefit from adaptive gap sorting and few allocations | Upstream project by Will Stafford Parsons (public repository currently offline) |
| Meteor Sort                 | No      | Almost-sorted data where gap shrinking beats plain insertion sort          | [meteorsort by Wiley Looper](https://github.com/wileylooper/meteorsort)         |
| Pattern-Defeating QuickSort | No      | General-purpose quicksort with protections against worst-case inputs       | [pdqsort by Orson Peters](https://github.com/orlp/pdqsort)                      |
| Grail Sort                  | Yes     | Large datasets where stability + low allocations matter                    | [GrailSort](https://github.com/Mrrl/GrailSort)                                  |
| Power Sort                  | Yes     | Partially ordered data that benefits from adaptive run detection           | [PowerSort (Munro & Wild)](https://arxiv.org/abs/1805.04154)                    |
| Insertion Sort              | Yes     | Tiny or nearly sorted collections where O(n²) is acceptable                | [Wikipedia - Insertion sort](https://en.wikipedia.org/wiki/Insertion_sort)      |

> **What does “stable” mean?** Stable sorting algorithms preserve the relative order of elements that compare as equal. This matters when items carry secondary keys (e.g., sorting people by last name but keeping first-name order deterministic). Unstable algorithms can reshuffle equal entries, which is usually fine for numeric keys but can break deterministic pipelines.
>
> **Heads up:** The original Ghost Sort repository was formerly hosted on GitHub under `wstaffordp/ghostsort`, but it currently returns 404. The Unity Helpers implementation remains based on that source; we will relink if/when an official mirror returns.

## Dataset Scenarios

- **Sorted** – ascending integers, verifying best-case behavior.
- **Nearly Sorted (2% swaps)** – deterministic neighbor swaps introduce light disorder to expose adaptive optimizations.
- **Shuffled (deterministic)** – Fisher–Yates shuffle using a fixed seed for reproducibility across runs and machines.

Each benchmark sorts a fresh copy of the dataset once and reports wall-clock duration. Insertion sort is skipped for lists larger than 10,000 elements because O(n²) quickly becomes impractical; the table shows `n/a` for those entries.

Run the `IListSortingPerformanceTests.Benchmark` test inside Unity’s Test Runner to refresh the tables below. Results automatically land in the section that matches the current operating system.

## Windows (Editor/Player)

<!-- ILIST_SORT_WINDOWS_START -->

_Last updated 2025-11-01 02:07 UTC on Windows 11 (10.0.26200) 64bit_

Times are single-pass measurements in milliseconds (lower is better). `n/a` indicates the algorithm was skipped for the dataset size.

### Sorted

| List Size | Ghost    | Meteor   | Pattern-Defeating QuickSort | Grail    | Power    | Insertion |
| --------- | -------- | -------- | --------------------------- | -------- | -------- | --------- |
| 100       | 0.180 ms | 0.072 ms | 0.285 ms                    | 0.113 ms | 0.393 ms | 0.067 ms  |
| 1,000     | 0.025 ms | 0.029 ms | 0.008 ms                    | 0.027 ms | 0.005 ms | 0.006 ms  |
| 10,000    | 0.307 ms | 0.414 ms | 0.071 ms                    | 0.122 ms | 0.043 ms | 0.063 ms  |
| 100,000   | 3.70 ms  | 5.42 ms  | 0.711 ms                    | 1.20 ms  | 0.433 ms | n/a       |
| 1,000,000 | 43.4 ms  | 67.0 ms  | 7.14 ms                     | 14.5 ms  | 4.36 ms  | n/a       |

### Nearly Sorted (2% swaps)

| List Size | Ghost    | Meteor   | Pattern-Defeating QuickSort | Grail    | Power    | Insertion |
| --------- | -------- | -------- | --------------------------- | -------- | -------- | --------- |
| 100       | 0.003 ms | 0.002 ms | 0.009 ms                    | 0.154 ms | 0.021 ms | 0.001 ms  |
| 1,000     | 0.025 ms | 0.029 ms | 0.034 ms                    | 0.010 ms | 0.017 ms | 0.007 ms  |
| 10,000    | 0.309 ms | 0.426 ms | 0.411 ms                    | 0.075 ms | 0.272 ms | 0.068 ms  |
| 100,000   | 3.74 ms  | 5.47 ms  | 5.00 ms                     | 0.768 ms | 3.97 ms  | n/a       |
| 1,000,000 | 43.7 ms  | 67.3 ms  | 58.2 ms                     | 7.83 ms  | 50.8 ms  | n/a       |

### Shuffled (deterministic)

| List Size | Ghost    | Meteor   | Pattern-Defeating QuickSort | Grail    | Power    | Insertion |
| --------- | -------- | -------- | --------------------------- | -------- | -------- | --------- |
| 100       | 0.010 ms | 0.008 ms | 0.007 ms                    | 0.013 ms | 0.011 ms | 0.017 ms  |
| 1,000     | 0.157 ms | 0.135 ms | 0.094 ms                    | 0.114 ms | 0.118 ms | 1.52 ms   |
| 10,000    | 2.22 ms  | 1.94 ms  | 1.21 ms                     | 1.46 ms  | 1.51 ms  | 150 ms    |
| 100,000   | 31.6 ms  | 25.7 ms  | 15.1 ms                     | 18.3 ms  | 18.5 ms  | n/a       |
| 1,000,000 | 443 ms   | 323 ms   | 179 ms                      | 219 ms   | 222 ms   | n/a       |

<!-- ILIST_SORT_WINDOWS_END -->

## macOS

<!-- ILIST_SORT_MACOS_START -->

Pending — run the IList sorting benchmark suite on macOS to capture results.

<!-- ILIST_SORT_MACOS_END -->

## Linux

<!-- ILIST_SORT_LINUX_START -->

Pending — run the IList sorting benchmark suite on Linux to capture results.

<!-- ILIST_SORT_LINUX_END -->

## Other Platforms

<!-- ILIST_SORT_OTHER_START -->

Pending — run the IList sorting benchmark suite on the target platform to capture results.

<!-- ILIST_SORT_OTHER_END -->
