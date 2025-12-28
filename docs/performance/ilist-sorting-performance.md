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

_Last updated 2025-12-28 04:03 UTC on Windows 11 (10.0.26200) 64bit_

Times are single-pass measurements in milliseconds (lower is better). `n/a` indicates the algorithm was skipped for the dataset size.

### Sorted

| List Size | Ghost    | Meteor   | Pattern-Defeating QuickSort | Grail    | Power    | Insertion | Tim      | Jesse    | Green    | Ska      | Ipn      | Smooth   | Block    | IPS4o    | Power+   | Glide    | Flux     |
| --------- | -------- | -------- | --------------------------- | -------- | -------- | --------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- |
| 100       | 0.030 ms | 0.002 ms | 0.001 ms                    | 0.001 ms | 0.001 ms | 0.001 ms  | 0.001 ms | 0.059 ms | 0.001 ms | 0.006 ms | 0.001 ms | 0.002 ms | 0.001 ms | 0.001 ms | 0.001 ms | 0.001 ms | 0.002 ms |
| 1,000     | 0.021 ms | 0.026 ms | 0.007 ms                    | 0.006 ms | 0.005 ms | 0.006 ms  | 0.004 ms | 0.842 ms | 0.006 ms | 0.098 ms | 0.007 ms | 0.017 ms | 0.005 ms | 0.032 ms | 0.005 ms | 0.005 ms | 0.027 ms |
| 10,000    | 0.269 ms | 0.364 ms | 0.074 ms                    | 0.063 ms | 0.043 ms | 0.056 ms  | 0.042 ms | 11.5 ms  | 0.060 ms | 1.40 ms  | 0.069 ms | 0.168 ms | 0.048 ms | 0.505 ms | 0.043 ms | 0.042 ms | 0.355 ms |
| 100,000   | 3.27 ms  | 4.85 ms  | 0.691 ms                    | 0.599 ms | 0.441 ms | n/a       | 0.421 ms | 197 ms   | 0.600 ms | 18.4 ms  | 0.690 ms | 1.65 ms  | 0.479 ms | 6.23 ms  | 0.428 ms | 0.420 ms | 4.59 ms  |
| 1,000,000 | 38.5 ms  | 60.5 ms  | 6.96 ms                     | 6.46 ms  | 4.27 ms  | n/a       | 4.20 ms  | 2.13 s   | 6.00 ms  | 225 ms   | 7.01 ms  | 17.5 ms  | 5.43 ms  | 88.4 ms  | 4.27 ms  | 4.45 ms  | 54.8 ms  |

### Nearly Sorted (2% swaps)

| List Size | Ghost    | Meteor   | Pattern-Defeating QuickSort | Grail    | Power    | Insertion | Tim      | Jesse    | Green    | Ska      | Ipn      | Smooth   | Block    | IPS4o    | Power+   | Glide    | Flux     |
| --------- | -------- | -------- | --------------------------- | -------- | -------- | --------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- |
| 100       | 0.001 ms | 0.002 ms | 0.002 ms                    | 0.001 ms | 0.001 ms | 0.001 ms  | 0.001 ms | 0.058 ms | 0.001 ms | 0.006 ms | 0.001 ms | 0.002 ms | 0.001 ms | 0.002 ms | 0.002 ms | 0.001 ms | 0.002 ms |
| 1,000     | 0.022 ms | 0.026 ms | 0.029 ms                    | 0.126 ms | 0.030 ms | 0.007 ms  | 0.014 ms | 0.832 ms | 0.006 ms | 0.099 ms | 0.026 ms | 0.017 ms | 0.006 ms | 0.041 ms | 0.022 ms | 0.014 ms | 0.028 ms |
| 10,000    | 0.318 ms | 0.371 ms | 0.402 ms                    | 0.069 ms | 0.246 ms | 0.058 ms  | 0.179 ms | 11.7 ms  | 0.070 ms | 1.65 ms  | 0.365 ms | 0.173 ms | 0.065 ms | 0.616 ms | 0.387 ms | 0.158 ms | 0.364 ms |
| 100,000   | 3.28 ms  | 4.86 ms  | 5.04 ms                     | 0.711 ms | 3.75 ms  | n/a       | 2.61 ms  | 191 ms   | 0.679 ms | 18.4 ms  | 4.42 ms  | 1.73 ms  | 0.586 ms | 7.53 ms  | 5.61 ms  | 2.24 ms  | 4.61 ms  |
| 1,000,000 | 40.0 ms  | 60.7 ms  | 55.0 ms                     | 7.57 ms  | 47.8 ms  | n/a       | 25.1 ms  | 2.10 s   | 7.32 ms  | 226 ms   | 52.3 ms  | 17.6 ms  | 7.20 ms  | 93.4 ms  | 75.9 ms  | 18.7 ms  | 56.2 ms  |

### Shuffled (deterministic)

| List Size | Ghost    | Meteor   | Pattern-Defeating QuickSort | Grail    | Power    | Insertion | Tim      | Jesse    | Green    | Ska      | Ipn      | Smooth   | Block    | IPS4o    | Power+   | Glide    | Flux     |
| --------- | -------- | -------- | --------------------------- | -------- | -------- | --------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- |
| 100       | 0.008 ms | 0.006 ms | 0.005 ms                    | 0.007 ms | 0.007 ms | 0.015 ms  | 0.009 ms | 0.030 ms | 0.007 ms | 0.008 ms | 0.007 ms | 0.010 ms | 0.005 ms | 0.005 ms | 0.029 ms | 0.010 ms | 0.006 ms |
| 1,000     | 0.144 ms | 0.122 ms | 0.087 ms                    | 0.102 ms | 0.103 ms | 1.37 ms   | 0.136 ms | 0.403 ms | 0.105 ms | 0.141 ms | 0.093 ms | 0.182 ms | 0.091 ms | 0.101 ms | 0.422 ms | 0.150 ms | 0.095 ms |
| 10,000    | 2.00 ms  | 1.78 ms  | 1.18 ms                     | 1.42 ms  | 1.46 ms  | 137 ms    | 1.54 ms  | 5.47 ms  | 1.35 ms  | 1.83 ms  | 1.26 ms  | 2.78 ms  | 1.14 ms  | 1.46 ms  | 6.16 ms  | 1.71 ms  | 1.43 ms  |
| 100,000   | 29.2 ms  | 24.6 ms  | 14.9 ms                     | 17.7 ms  | 18.0 ms  | n/a       | 19.8 ms  | 70.0 ms  | 17.6 ms  | 24.4 ms  | 15.6 ms  | 34.3 ms  | 14.5 ms  | 18.3 ms  | 77.9 ms  | 22.5 ms  | 17.4 ms  |
| 1,000,000 | 411 ms   | 305 ms   | 180 ms                      | 212 ms   | 213 ms   | n/a       | 246 ms   | 896 ms   | 215 ms   | 289 ms   | 186 ms   | 448 ms   | 180 ms   | 227 ms   | 1.12 s   | 289 ms   | 214 ms   |

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
