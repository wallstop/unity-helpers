# IList Sorting Performance Benchmarks

Unity Helpers ships several custom sorting algorithms for `IList<T>` that cover different trade-offs between adaptability, allocation patterns, and stability. This page gathers context and benchmark snapshots so you can choose the right algorithm for your workload and compare results across operating systems.

## Algorithm Cheatsheet

| Algorithm                   | Stable? | Best For                                                                   | Reference                                                                                                 |
| --------------------------- | ------- | -------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------- |
| Ghost Sort                  | No      | Mixed workloads that benefit from adaptive gap sorting and few allocations | Upstream project by Will Stafford Parsons (public repository currently offline)                           |
| Meteor Sort                 | No      | Almost-sorted data where gap shrinking beats plain insertion sort          | [meteorsort by Wiley Looper](https://github.com/wileylooper/meteorsort)                                   |
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
| Glide Sort                  | Yes     | Stable galloping merges from the Rust glidesort research                   | [Glidesort write-up](https://github.com/Voultapher/sort-research-rs/tree/main/writeup/glidesort)          |
| Flux Sort                   | No      | Dual-pivot quicksort tuned for modern CPUs                                 | [Fluxsort write-up](https://github.com/Voultapher/sort-research-rs/tree/main/writeup/fluxsort)            |
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

_Last updated 2025-11-29 01:37 UTC on Windows 11 (10.0.26200) 64bit_

Times are single-pass measurements in milliseconds (lower is better). `n/a` indicates the algorithm was skipped for the dataset size.

### Sorted

| List Size | Ghost    | Meteor   | Pattern-Defeating QuickSort | Grail    | Power    | Insertion | Tim      | Jesse    | Green    | Ska      | Ipn      | Smooth   | Block    | IPS4o    | Power+   | Glide    | Flux     |
| --------- | -------- | -------- | --------------------------- | -------- | -------- | --------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- |
| 100       | 0.033 ms | 0.002 ms | 0.001 ms                    | 0.001 ms | 0.001 ms | 0.001 ms  | 0.001 ms | 0.059 ms | 0.001 ms | 0.006 ms | 0.001 ms | 0.002 ms | 0.001 ms | 0.001 ms | 0.001 ms | 0.001 ms | 0.002 ms |
| 1,000     | 0.023 ms | 0.028 ms | 0.008 ms                    | 0.007 ms | 0.005 ms | 0.006 ms  | 0.005 ms | 0.797 ms | 0.007 ms | 0.103 ms | 0.008 ms | 0.018 ms | 0.005 ms | 0.038 ms | 0.005 ms | 0.005 ms | 0.030 ms |
| 10,000    | 0.293 ms | 0.403 ms | 0.078 ms                    | 0.066 ms | 0.048 ms | 0.061 ms  | 0.046 ms | 10.8 ms  | 0.066 ms | 1.46 ms  | 0.078 ms | 0.182 ms | 0.052 ms | 0.577 ms | 0.048 ms | 0.047 ms | 0.394 ms |
| 100,000   | 3.54 ms  | 5.21 ms  | 0.780 ms                    | 0.684 ms | 0.475 ms | n/a       | 0.462 ms | 156 ms   | 0.654 ms | 18.8 ms  | 0.782 ms | 1.76 ms  | 0.522 ms | 7.27 ms  | 0.475 ms | 0.463 ms | 4.96 ms  |
| 1,000,000 | 41.7 ms  | 64.5 ms  | 7.92 ms                     | 6.63 ms  | 4.82 ms  | n/a       | 4.75 ms  | 2.01 s   | 6.66 ms  | 236 ms   | 7.90 ms  | 18.0 ms  | 5.64 ms  | 100 ms   | 4.83 ms  | 4.89 ms  | 60.1 ms  |

### Nearly Sorted (2% swaps)

| List Size | Ghost    | Meteor   | Pattern-Defeating QuickSort | Grail    | Power    | Insertion | Tim      | Jesse    | Green    | Ska      | Ipn      | Smooth   | Block    | IPS4o    | Power+   | Glide    | Flux     |
| --------- | -------- | -------- | --------------------------- | -------- | -------- | --------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- |
| 100       | 0.001 ms | 0.002 ms | 0.002 ms                    | 0.001 ms | 0.001 ms | 0.001 ms  | 0.001 ms | 0.053 ms | 0.001 ms | 0.006 ms | 0.002 ms | 0.002 ms | 0.001 ms | 0.002 ms | 0.002 ms | 0.001 ms | 0.002 ms |
| 1,000     | 0.024 ms | 0.028 ms | 0.033 ms                    | 0.008 ms | 0.016 ms | 0.007 ms  | 0.014 ms | 0.784 ms | 0.007 ms | 0.104 ms | 0.033 ms | 0.018 ms | 0.007 ms | 0.045 ms | 0.023 ms | 0.015 ms | 0.031 ms |
| 10,000    | 0.299 ms | 0.407 ms | 0.486 ms                    | 0.074 ms | 0.241 ms | 0.064 ms  | 0.177 ms | 10.5 ms  | 0.072 ms | 1.47 ms  | 0.416 ms | 0.183 ms | 0.070 ms | 0.681 ms | 0.386 ms | 0.170 ms | 0.397 ms |
| 100,000   | 3.58 ms  | 5.25 ms  | 5.40 ms                     | 0.785 ms | 3.71 ms  | n/a       | 2.56 ms  | 153 ms   | 0.750 ms | 18.8 ms  | 5.05 ms  | 1.80 ms  | 0.644 ms | 8.46 ms  | 5.63 ms  | 2.31 ms  | 5.02 ms  |
| 1,000,000 | 42.2 ms  | 65.5 ms  | 63.0 ms                     | 8.40 ms  | 52.2 ms  | n/a       | 27.7 ms  | 2.20 s   | 7.88 ms  | 230 ms   | 59.0 ms  | 18.5 ms  | 8.17 ms  | 108 ms   | 80.1 ms  | 19.6 ms  | 60.2 ms  |

### Shuffled (deterministic)

| List Size | Ghost    | Meteor   | Pattern-Defeating QuickSort | Grail    | Power    | Insertion | Tim      | Jesse    | Green    | Ska      | Ipn      | Smooth   | Block    | IPS4o    | Power+   | Glide    | Flux     |
| --------- | -------- | -------- | --------------------------- | -------- | -------- | --------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- |
| 100       | 0.008 ms | 0.007 ms | 0.005 ms                    | 0.006 ms | 0.007 ms | 0.017 ms  | 0.010 ms | 0.029 ms | 0.007 ms | 0.009 ms | 0.007 ms | 0.011 ms | 0.005 ms | 0.005 ms | 0.022 ms | 0.011 ms | 0.006 ms |
| 1,000     | 0.156 ms | 0.134 ms | 0.097 ms                    | 0.111 ms | 0.111 ms | 1.51 ms   | 0.149 ms | 0.428 ms | 0.113 ms | 0.134 ms | 0.100 ms | 0.196 ms | 0.091 ms | 0.107 ms | 0.432 ms | 0.161 ms | 0.100 ms |
| 10,000    | 2.19 ms  | 1.95 ms  | 1.25 ms                     | 1.48 ms  | 1.53 ms  | 148 ms    | 1.63 ms  | 5.59 ms  | 1.42 ms  | 1.83 ms  | 1.33 ms  | 2.80 ms  | 1.21 ms  | 1.59 ms  | 6.06 ms  | 1.88 ms  | 1.45 ms  |
| 100,000   | 31.7 ms  | 26.2 ms  | 15.8 ms                     | 18.6 ms  | 18.8 ms  | n/a       | 21.0 ms  | 70.8 ms  | 18.6 ms  | 23.2 ms  | 16.3 ms  | 35.9 ms  | 15.1 ms  | 20.2 ms  | 76.4 ms  | 24.3 ms  | 18.1 ms  |
| 1,000,000 | 444 ms   | 329 ms   | 187 ms                      | 226 ms   | 228 ms   | n/a       | 267 ms   | 880 ms   | 232 ms   | 303 ms   | 202 ms   | 447 ms   | 189 ms   | 246 ms   | 1.05 s   | 312 ms   | 221 ms   |

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
