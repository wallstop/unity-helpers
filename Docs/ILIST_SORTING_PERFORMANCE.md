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

_Last updated 2025-11-22 03:06 UTC on Windows 11 (10.0.26200) 64bit_

Times are single-pass measurements in milliseconds (lower is better). `n/a` indicates the algorithm was skipped for the dataset size.

### Sorted

| List Size | Ghost    | Meteor   | Pattern-Defeating QuickSort | Grail    | Power    | Insertion |
| --------- | -------- | -------- | --------------------------- | -------- | -------- | --------- |
| 100       | 0.379 ms | 0.070 ms | 0.404 ms                    | 0.753 ms | 0.870 ms | 0.066 ms  |
| 1,000     | 0.023 ms | 0.027 ms | 0.008 ms                    | 0.019 ms | 0.005 ms | 0.006 ms  |
| 10,000    | 0.286 ms | 0.388 ms | 0.073 ms                    | 0.126 ms | 0.045 ms | 0.062 ms  |
| 100,000   | 3.54 ms  | 5.18 ms  | 0.736 ms                    | 1.29 ms  | 0.457 ms | n/a       |
| 1,000,000 | 41.5 ms  | 64.7 ms  | 7.44 ms                     | 39.2 ms  | 4.55 ms  | n/a       |

### Nearly Sorted (2% swaps)

| List Size | Ghost    | Meteor   | Pattern-Defeating QuickSort | Grail    | Power    | Insertion |
| --------- | -------- | -------- | --------------------------- | -------- | -------- | --------- |
| 100       | 0.003 ms | 0.002 ms | 0.009 ms                    | 0.182 ms | 0.061 ms | 0.001 ms  |
| 1,000     | 0.023 ms | 0.028 ms | 0.035 ms                    | 0.010 ms | 0.020 ms | 0.006 ms  |
| 10,000    | 0.292 ms | 0.395 ms | 0.419 ms                    | 0.081 ms | 0.249 ms | 0.064 ms  |
| 100,000   | 3.56 ms  | 5.21 ms  | 5.05 ms                     | 0.801 ms | 3.82 ms  | n/a       |
| 1,000,000 | 42.7 ms  | 65.3 ms  | 59.0 ms                     | 7.92 ms  | 48.4 ms  | n/a       |

### Shuffled (deterministic)

| List Size | Ghost    | Meteor   | Pattern-Defeating QuickSort | Grail    | Power    | Insertion |
| --------- | -------- | -------- | --------------------------- | -------- | -------- | --------- |
| 100       | 0.011 ms | 0.008 ms | 0.007 ms                    | 0.012 ms | 0.011 ms | 0.016 ms  |
| 1,000     | 0.154 ms | 0.130 ms | 0.093 ms                    | 0.109 ms | 0.120 ms | 1.49 ms   |
| 10,000    | 2.15 ms  | 1.95 ms  | 1.23 ms                     | 1.45 ms  | 1.53 ms  | 147 ms    |
| 100,000   | 31.4 ms  | 25.4 ms  | 15.3 ms                     | 18.5 ms  | 18.5 ms  | n/a       |
| 1,000,000 | 439 ms   | 322 ms   | 181 ms                      | 218 ms   | 222 ms   | n/a       |

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
