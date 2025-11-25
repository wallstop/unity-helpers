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

_Last updated 2025-11-25 22:56 UTC on Windows 11 (10.0.26200) 64bit_

Times are single-pass measurements in milliseconds (lower is better). `n/a` indicates the algorithm was skipped for the dataset size.

### Sorted

| List Size | Ghost    | Meteor   | Pattern-Defeating QuickSort | Grail    | Power    | Insertion |
| --------- | -------- | -------- | --------------------------- | -------- | -------- | --------- |
| 100       | 0.239 ms | 0.078 ms | 0.326 ms                    | 0.138 ms | 0.427 ms | 0.095 ms  |
| 1,000     | 0.024 ms | 0.028 ms | 0.008 ms                    | 0.020 ms | 0.006 ms | 0.006 ms  |
| 10,000    | 0.309 ms | 0.670 ms | 0.075 ms                    | 0.123 ms | 0.048 ms | 0.061 ms  |
| 100,000   | 3.95 ms  | 5.38 ms  | 0.767 ms                    | 1.25 ms  | 0.483 ms | n/a       |
| 1,000,000 | 43.8 ms  | 68.8 ms  | 7.72 ms                     | 12.1 ms  | 5.15 ms  | n/a       |

### Nearly Sorted (2% swaps)

| List Size | Ghost    | Meteor   | Pattern-Defeating QuickSort | Grail    | Power    | Insertion |
| --------- | -------- | -------- | --------------------------- | -------- | -------- | --------- |
| 100       | 0.004 ms | 0.002 ms | 0.010 ms                    | 0.186 ms | 0.027 ms | 0.001 ms  |
| 1,000     | 0.026 ms | 0.029 ms | 0.038 ms                    | 0.010 ms | 0.017 ms | 0.007 ms  |
| 10,000    | 0.304 ms | 0.405 ms | 0.422 ms                    | 0.079 ms | 0.298 ms | 0.066 ms  |
| 100,000   | 3.73 ms  | 5.46 ms  | 5.16 ms                     | 0.847 ms | 3.74 ms  | n/a       |
| 1,000,000 | 44.8 ms  | 69.3 ms  | 61.8 ms                     | 8.50 ms  | 51.1 ms  | n/a       |

### Shuffled (deterministic)

| List Size | Ghost    | Meteor   | Pattern-Defeating QuickSort | Grail    | Power    | Insertion |
| --------- | -------- | -------- | --------------------------- | -------- | -------- | --------- |
| 100       | 0.011 ms | 0.008 ms | 0.008 ms                    | 0.029 ms | 0.013 ms | 0.017 ms  |
| 1,000     | 0.160 ms | 0.147 ms | 0.099 ms                    | 0.115 ms | 0.116 ms | 1.58 ms   |
| 10,000    | 2.24 ms  | 1.95 ms  | 1.32 ms                     | 1.48 ms  | 1.83 ms  | 156 ms    |
| 100,000   | 32.7 ms  | 26.1 ms  | 16.3 ms                     | 18.8 ms  | 18.9 ms  | n/a       |
| 1,000,000 | 463 ms   | 329 ms   | 193 ms                      | 221 ms   | 225 ms   | n/a       |

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
