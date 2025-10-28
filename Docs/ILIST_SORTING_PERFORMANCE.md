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

_Last updated 2025-10-28 22:30 UTC on Windows 11 (10.0.26200) 64bit_

Times are single-pass measurements in milliseconds (lower is better). `n/a` indicates the algorithm was skipped for the dataset size.

### Sorted

| List Size | Ghost    | Meteor   | Pattern-Defeating QuickSort | Grail    | Power    | Insertion |
| --------- | -------- | -------- | --------------------------- | -------- | -------- | --------- |
| 100       | 0.399 ms | 0.074 ms | 0.392 ms                    | 0.792 ms | 1.13 ms  | 0.066 ms  |
| 1,000     | 0.025 ms | 0.027 ms | 0.008 ms                    | 0.025 ms | 0.008 ms | 0.006 ms  |
| 10,000    | 0.259 ms | 0.365 ms | 0.067 ms                    | 0.117 ms | 0.042 ms | 0.054 ms  |
| 100,000   | 3.16 ms  | 4.70 ms  | 0.674 ms                    | 1.21 ms  | 0.441 ms | n/a       |
| 1,000,000 | 37.9 ms  | 58.2 ms  | 6.84 ms                     | 11.6 ms  | 4.12 ms  | n/a       |

### Nearly Sorted (2% swaps)

| List Size | Ghost    | Meteor   | Pattern-Defeating QuickSort | Grail    | Power    | Insertion |
| --------- | -------- | -------- | --------------------------- | -------- | -------- | --------- |
| 100       | 0.002 ms | 0.002 ms | 0.006 ms                    | 0.165 ms | 0.056 ms | 0.001 ms  |
| 1,000     | 0.021 ms | 0.025 ms | 0.032 ms                    | 0.009 ms | 0.021 ms | 0.006 ms  |
| 10,000    | 0.274 ms | 0.356 ms | 0.384 ms                    | 0.073 ms | 0.233 ms | 0.058 ms  |
| 100,000   | 3.24 ms  | 4.75 ms  | 4.74 ms                     | 0.717 ms | 3.85 ms  | n/a       |
| 1,000,000 | 38.1 ms  | 58.7 ms  | 55.0 ms                     | 7.61 ms  | 49.5 ms  | n/a       |

### Shuffled (deterministic)

| List Size | Ghost    | Meteor   | Pattern-Defeating QuickSort | Grail    | Power    | Insertion |
| --------- | -------- | -------- | --------------------------- | -------- | -------- | --------- |
| 100       | 0.009 ms | 0.014 ms | 0.007 ms                    | 0.012 ms | 0.011 ms | 0.015 ms  |
| 1,000     | 0.144 ms | 0.124 ms | 0.089 ms                    | 0.113 ms | 0.112 ms | 1.34 ms   |
| 10,000    | 2.01 ms  | 1.81 ms  | 1.18 ms                     | 1.42 ms  | 1.51 ms  | 133 ms    |
| 100,000   | 29.0 ms  | 24.1 ms  | 14.5 ms                     | 17.9 ms  | 18.3 ms  | n/a       |
| 1,000,000 | 405 ms   | 302 ms   | 173 ms                      | 215 ms   | 221 ms   | n/a       |

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
