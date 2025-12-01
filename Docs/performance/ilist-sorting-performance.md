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

_Last updated 2025-12-01 04:00 UTC on Windows 11 (10.0.26200) 64bit_

Times are single-pass measurements in milliseconds (lower is better). `n/a` indicates the algorithm was skipped for the dataset size.

### Sorted

| List Size | Ghost    | Meteor   | Pattern-Defeating QuickSort | Grail    | Power    | Insertion | Tim      | Jesse    | Green    | Ska      | Ipn      | Smooth   | Block    | IPS4o    | Power+   | Glide    | Flux     |
| --------- | -------- | -------- | --------------------------- | -------- | -------- | --------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- |
| 100       | 0.032 ms | 0.002 ms | 0.001 ms                    | 0.001 ms | 0.001 ms | 0.001 ms  | 0.001 ms | 0.054 ms | 0.001 ms | 0.006 ms | 0.001 ms | 0.002 ms | 0.001 ms | 0.001 ms | 0.001 ms | 0.001 ms | 0.002 ms |
| 1,000     | 0.024 ms | 0.028 ms | 0.008 ms                    | 0.008 ms | 0.005 ms | 0.007 ms  | 0.005 ms | 0.788 ms | 0.007 ms | 0.107 ms | 0.008 ms | 0.018 ms | 0.006 ms | 0.036 ms | 0.005 ms | 0.005 ms | 0.029 ms |
| 10,000    | 0.306 ms | 0.416 ms | 0.079 ms                    | 0.071 ms | 0.049 ms | 0.067 ms  | 0.048 ms | 10.7 ms  | 0.066 ms | 1.53 ms  | 0.079 ms | 0.181 ms | 0.054 ms | 0.562 ms | 0.049 ms | 0.049 ms | 0.401 ms |
| 100,000   | 3.64 ms  | 5.36 ms  | 0.805 ms                    | 0.722 ms | 0.510 ms | n/a       | 0.477 ms | 183 ms   | 0.660 ms | 20.0 ms  | 0.929 ms | 1.83 ms  | 0.543 ms | 7.07 ms  | 0.493 ms | 0.487 ms | 5.26 ms  |
| 1,000,000 | 43.9 ms  | 67.5 ms  | 8.17 ms                     | 7.31 ms  | 5.01 ms  | n/a       | 5.05 ms  | 2.09 s   | 6.76 ms  | 243 ms   | 7.92 ms  | 18.5 ms  | 5.76 ms  | 94.2 ms  | 4.90 ms  | 5.06 ms  | 60.9 ms  |

### Nearly Sorted (2% swaps)

| List Size | Ghost    | Meteor   | Pattern-Defeating QuickSort | Grail    | Power    | Insertion | Tim      | Jesse    | Green    | Ska      | Ipn      | Smooth   | Block    | IPS4o    | Power+   | Glide    | Flux     |
| --------- | -------- | -------- | --------------------------- | -------- | -------- | --------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- |
| 100       | 0.001 ms | 0.002 ms | 0.002 ms                    | 0.001 ms | 0.001 ms | 0.001 ms  | 0.001 ms | 0.052 ms | 0.001 ms | 0.006 ms | 0.002 ms | 0.002 ms | 0.001 ms | 0.002 ms | 0.002 ms | 0.001 ms | 0.002 ms |
| 1,000     | 0.024 ms | 0.029 ms | 0.033 ms                    | 0.008 ms | 0.015 ms | 0.007 ms  | 0.014 ms | 0.769 ms | 0.007 ms | 0.106 ms | 0.029 ms | 0.018 ms | 0.007 ms | 0.043 ms | 0.022 ms | 0.014 ms | 0.031 ms |
| 10,000    | 0.298 ms | 0.411 ms | 0.552 ms                    | 0.081 ms | 0.232 ms | 0.064 ms  | 0.175 ms | 10.3 ms  | 0.073 ms | 1.53 ms  | 0.411 ms | 0.183 ms | 0.070 ms | 0.648 ms | 0.372 ms | 0.167 ms | 0.542 ms |
| 100,000   | 3.80 ms  | 5.39 ms  | 5.29 ms                     | 0.895 ms | 3.97 ms  | n/a       | 2.64 ms  | 168 ms   | 0.749 ms | 19.8 ms  | 5.10 ms  | 1.87 ms  | 0.686 ms | 8.09 ms  | 5.57 ms  | 2.50 ms  | 5.10 ms  |
| 1,000,000 | 43.7 ms  | 67.1 ms  | 62.6 ms                     | 8.46 ms  | 48.8 ms  | n/a       | 26.3 ms  | 2.00 s   | 7.89 ms  | 246 ms   | 60.9 ms  | 18.6 ms  | 7.84 ms  | 99.1 ms  | 73.5 ms  | 19.2 ms  | 61.0 ms  |

### Shuffled (deterministic)

| List Size | Ghost    | Meteor   | Pattern-Defeating QuickSort | Grail    | Power    | Insertion | Tim      | Jesse    | Green    | Ska      | Ipn      | Smooth   | Block    | IPS4o    | Power+   | Glide    | Flux     |
| --------- | -------- | -------- | --------------------------- | -------- | -------- | --------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- |
| 100       | 0.008 ms | 0.007 ms | 0.006 ms                    | 0.007 ms | 0.007 ms | 0.017 ms  | 0.011 ms | 0.029 ms | 0.007 ms | 0.009 ms | 0.007 ms | 0.011 ms | 0.005 ms | 0.006 ms | 0.022 ms | 0.011 ms | 0.007 ms |
| 1,000     | 0.156 ms | 0.132 ms | 0.091 ms                    | 0.105 ms | 0.108 ms | 1.54 ms   | 0.152 ms | 0.396 ms | 0.111 ms | 0.135 ms | 0.099 ms | 0.195 ms | 0.088 ms | 0.103 ms | 0.424 ms | 0.165 ms | 0.099 ms |
| 10,000    | 2.20 ms  | 1.95 ms  | 1.24 ms                     | 1.44 ms  | 1.53 ms  | 152 ms    | 1.60 ms  | 5.22 ms  | 1.40 ms  | 1.85 ms  | 1.32 ms  | 2.81 ms  | 1.19 ms  | 1.53 ms  | 5.93 ms  | 1.87 ms  | 1.44 ms  |
| 100,000   | 31.9 ms  | 25.8 ms  | 15.5 ms                     | 18.4 ms  | 18.6 ms  | n/a       | 21.1 ms  | 66.3 ms  | 18.4 ms  | 23.9 ms  | 16.2 ms  | 35.6 ms  | 14.9 ms  | 19.2 ms  | 75.4 ms  | 24.6 ms  | 17.9 ms  |
| 1,000,000 | 449 ms   | 327 ms   | 185 ms                      | 219 ms   | 224 ms   | n/a       | 263 ms   | 866 ms   | 229 ms   | 295 ms   | 192 ms   | 434 ms   | 178 ms   | 236 ms   | 1.02 s   | 310 ms   | 218 ms   |

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
