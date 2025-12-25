# IList Sorting Performance Benchmarks

Unity Helpers ships several custom sorting algorithms for `IList<T>` that cover different trade-offs between adaptability, allocation patterns, and stability. This page gathers context and benchmark snapshots so you can choose the right algorithm for your workload and compare results across operating systems.

## Algorithm Cheatsheet

| Algorithm                   | Stable? | Best For                                                                   | Reference                                                                                                 |
| --------------------------- | ------- | -------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------- |
| Ghost Sort                  | No      | Mixed workloads that benefit from adaptive gap sorting and few allocations | Upstream project by Will Stafford Parsons (public repository currently offline)                           |
| Meteor Sort                 | No      | Almost-sorted data where gap shrinking beats plain insertion sort          | Upstream project by Will Stafford Parsons (public repository currently offline)                           |
| Pattern-Defeating QuickSort | No      | General-purpose quicksort with protections against worst-case inputs       | [pdqsort by Orson Peters](https://github.com/orlp/pdqsort)                                                |
| Grail Sort                  | Yes     | Large datasets where stability + low allocations matter                    | [GrailSort](https://github.com/Mrrl/GrailSort)                                                            |
| Power Sort                  | Yes     | Partially ordered data that benefits from adaptive run detection           | [PowerSort (Munro & Wild)](https://arxiv.org/abs/1805.04154)                                              |
| Tim Sort                    | Yes     | General-purpose stable sorting with abundant natural runs                  | [Wikipedia - Timsort](https://en.wikipedia.org/wiki/Timsort)                                              |
| Jesse Sort                  | No      | Data with long runs or duplicates where dual patience piles shine          | [JesseSort](https://github.com/lewj85/jessesort)                                                          |
| Green Sort                  | Yes     | Sustainable stable merges that trim ordered prefixes                       | [greeNsort](https://www.greensort.org/index.html)                                                         |
| Ska Sort                    | No      | Branch-friendly partitioning on large unstable datasets                    | [Ska Sort](https://probablydance.com/2016/12/27/i-wrote-a-faster-sorting-algorithm/)                      |
| Ipn Sort                    | No      | In-place adaptive quicksort scenarios needing robust pivots                | [ipnsort write-up](https://github.com/Voultapher/sort-research-rs/tree/main/writeup/ipnsort_introduction) |
| Smooth Sort                 | No      | Weak-heap hybrid that approaches O(n) for presorted data                   | [Smoothsort - Wikipedia](https://en.wikipedia.org/wiki/Smoothsort)                                        |
| Block Merge Sort            | Yes     | Stable merges with √n buffer (WikiSort style)                              | [WikiSort](https://github.com/BonzaiThePenguin/WikiSort)                                                  |
| IPS⁴o Sort                  | No      | Cache-aware samplesort with multiway partitioning                          | [IPS⁴o paper](https://arxiv.org/abs/1705.02257)                                                           |
| Power Sort Plus             | Yes     | Enhanced run-priority merges inspired by Wild & Nebel                      | [PowerSort paper](https://arxiv.org/abs/1805.04154)                                                       |
| Glide Sort                  | Yes     | Stable galloping merges from the Rust glidesort research                   | [sort-research-rs](https://github.com/Voultapher/sort-research-rs)                                        |
| Flux Sort                   | No      | Dual-pivot quicksort tuned for modern CPUs                                 | [sort-research-rs](https://github.com/Voultapher/sort-research-rs)                                        |
| Insertion Sort              | Yes     | Tiny or nearly sorted collections where O(n²) is acceptable                | [Wikipedia - Insertion sort](https://en.wikipedia.org/wiki/Insertion_sort)                                |

> **What does “stable” mean?** Stable sorting algorithms preserve the relative order of elements that compare as equal. This matters when items carry secondary keys (e.g., sorting people by last name but keeping first-name order deterministic). Unstable algorithms can reshuffle equal entries, which is usually fine for numeric keys but can break deterministic pipelines.
>
> **Heads up:** The original Ghost Sort repository was formerly hosted on GitHub under `wstaffordp/ghostsort`, but it currently returns 404. The Unity Helpers implementation remains based on that source; we will relink if/when an official mirror returns.

## Dataset Scenarios

- **Sorted** – ascending integers, verifying best-case behavior.
- **Nearly Sorted (2% swaps)** – deterministic neighbor swaps introduce light disorder to expose adaptive optimizations.
- **Shuffled (deterministic)** – Fisher–Yates shuffle using a fixed seed for reproducibility across runs and machines.

Each benchmark sorts a fresh copy of the dataset once and reports wall-clock duration. If a cell still shows `pending`, re-run the benchmark suite to collect fresh data for that algorithm/dataset size.

Run the `IListSortingPerformanceTests.Benchmark` test inside Unity’s Test Runner to refresh the tables below. Results automatically land in the section that matches the current operating system.

## Windows (Editor/Player)

<!-- ILIST_SORT_WINDOWS_START -->

_Last updated 2025-12-25 07:07 UTC on Windows 11 (10.0.26200) 64bit_

Times are single-pass measurements in milliseconds (lower is better). `n/a` indicates the algorithm was skipped for the dataset size.

### Sorted

| List Size | Ghost    | Meteor   | Pattern-Defeating QuickSort | Grail    | Power    | Insertion | Tim      | Jesse    | Green    | Ska      | Ipn      | Smooth   | Block    | IPS4o    | Power+   | Glide    | Flux     |
| --------- | -------- | -------- | --------------------------- | -------- | -------- | --------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- |
| 100       | 0.029 ms | 0.002 ms | 0.001 ms                    | 0.001 ms | 0.001 ms | 0.001 ms  | 0.001 ms | 0.052 ms | 0.001 ms | 0.006 ms | 0.001 ms | 0.002 ms | 0.001 ms | 0.001 ms | 0.001 ms | 0.001 ms | 0.002 ms |
| 1,000     | 0.021 ms | 0.030 ms | 0.007 ms                    | 0.007 ms | 0.004 ms | 0.005 ms  | 0.004 ms | 0.780 ms | 0.006 ms | 0.096 ms | 0.007 ms | 0.016 ms | 0.005 ms | 0.033 ms | 0.004 ms | 0.005 ms | 0.027 ms |
| 10,000    | 0.263 ms | 0.353 ms | 0.067 ms                    | 0.058 ms | 0.041 ms | 0.054 ms  | 0.040 ms | 10.4 ms  | 0.058 ms | 1.37 ms  | 0.067 ms | 0.167 ms | 0.047 ms | 0.528 ms | 0.041 ms | 0.041 ms | 0.365 ms |
| 100,000   | 3.28 ms  | 4.71 ms  | 0.668 ms                    | 0.590 ms | 0.411 ms | n/a       | 0.396 ms | 160 ms   | 0.582 ms | 17.9 ms  | 0.672 ms | 1.61 ms  | 0.465 ms | 6.60 ms  | 0.410 ms | 0.398 ms | 4.54 ms  |
| 1,000,000 | 37.8 ms  | 57.6 ms  | 6.79 ms                     | 6.04 ms  | 4.13 ms  | n/a       | 4.07 ms  | 2.05 s   | 6.00 ms  | 221 ms   | 6.73 ms  | 16.9 ms  | 4.83 ms  | 90.7 ms  | 4.17 ms  | 4.18 ms  | 54.8 ms  |

### Nearly Sorted (2% swaps)

| List Size | Ghost    | Meteor   | Pattern-Defeating QuickSort | Grail    | Power    | Insertion | Tim      | Jesse    | Green    | Ska      | Ipn      | Smooth   | Block    | IPS4o    | Power+   | Glide    | Flux     |
| --------- | -------- | -------- | --------------------------- | -------- | -------- | --------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- |
| 100       | 0.001 ms | 0.002 ms | 0.002 ms                    | 0.001 ms | 0.001 ms | 0.001 ms  | 0.001 ms | 0.051 ms | 0.001 ms | 0.006 ms | 0.001 ms | 0.002 ms | 0.001 ms | 0.002 ms | 0.002 ms | 0.001 ms | 0.002 ms |
| 1,000     | 0.023 ms | 0.025 ms | 0.029 ms                    | 0.007 ms | 0.015 ms | 0.006 ms  | 0.013 ms | 0.759 ms | 0.006 ms | 0.096 ms | 0.028 ms | 0.017 ms | 0.006 ms | 0.040 ms | 0.022 ms | 0.013 ms | 0.028 ms |
| 10,000    | 0.277 ms | 0.367 ms | 0.385 ms                    | 0.069 ms | 0.223 ms | 0.057 ms  | 0.164 ms | 10.9 ms  | 0.066 ms | 1.37 ms  | 0.358 ms | 0.170 ms | 0.064 ms | 0.609 ms | 0.371 ms | 0.163 ms | 0.364 ms |
| 100,000   | 3.25 ms  | 4.76 ms  | 4.63 ms                     | 0.679 ms | 3.42 ms  | n/a       | 2.34 ms  | 172 ms   | 0.681 ms | 17.8 ms  | 4.37 ms  | 1.71 ms  | 0.576 ms | 7.61 ms  | 5.35 ms  | 2.33 ms  | 4.57 ms  |
| 1,000,000 | 38.5 ms  | 58.3 ms  | 54.8 ms                     | 7.46 ms  | 48.6 ms  | n/a       | 26.2 ms  | 1.95 s   | 7.03 ms  | 218 ms   | 51.2 ms  | 17.3 ms  | 7.19 ms  | 97.4 ms  | 75.7 ms  | 18.5 ms  | 55.3 ms  |

### Shuffled (deterministic)

| List Size | Ghost    | Meteor   | Pattern-Defeating QuickSort | Grail    | Power    | Insertion | Tim      | Jesse    | Green    | Ska      | Ipn      | Smooth   | Block    | IPS4o    | Power+   | Glide    | Flux     |
| --------- | -------- | -------- | --------------------------- | -------- | -------- | --------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- |
| 100       | 0.007 ms | 0.006 ms | 0.005 ms                    | 0.006 ms | 0.006 ms | 0.015 ms  | 0.009 ms | 0.028 ms | 0.007 ms | 0.008 ms | 0.007 ms | 0.010 ms | 0.004 ms | 0.005 ms | 0.022 ms | 0.010 ms | 0.006 ms |
| 1,000     | 0.144 ms | 0.122 ms | 0.086 ms                    | 0.100 ms | 0.104 ms | 1.34 ms   | 0.134 ms | 0.391 ms | 0.102 ms | 0.127 ms | 0.091 ms | 0.179 ms | 0.084 ms | 0.101 ms | 0.417 ms | 0.146 ms | 0.105 ms |
| 10,000    | 1.98 ms  | 1.80 ms  | 1.17 ms                     | 1.36 ms  | 1.44 ms  | 133 ms    | 1.57 ms  | 5.18 ms  | 1.40 ms  | 1.80 ms  | 1.24 ms  | 2.58 ms  | 1.17 ms  | 1.47 ms  | 5.90 ms  | 1.76 ms  | 1.37 ms  |
| 100,000   | 28.7 ms  | 24.1 ms  | 14.6 ms                     | 17.7 ms  | 17.9 ms  | n/a       | 19.6 ms  | 67.2 ms  | 17.4 ms  | 21.9 ms  | 15.4 ms  | 32.8 ms  | 14.7 ms  | 19.0 ms  | 76.3 ms  | 22.1 ms  | 17.1 ms  |
| 1,000,000 | 409 ms   | 300 ms   | 175 ms                      | 212 ms   | 214 ms   | n/a       | 246 ms   | 880 ms   | 214 ms   | 278 ms   | 181 ms   | 410 ms   | 176 ms   | 232 ms   | 1.06 s   | 286 ms   | 208 ms   |

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
