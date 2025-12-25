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

_Last updated 2025-12-09 01:23 UTC on Windows 11 (10.0.26200) 64bit_

Times are single-pass measurements in milliseconds (lower is better). `n/a` indicates the algorithm was skipped for the dataset size.

### Sorted

| List Size | Ghost    | Meteor   | Pattern-Defeating QuickSort | Grail    | Power    | Insertion | Tim      | Jesse    | Green    | Ska      | Ipn      | Smooth   | Block    | IPS4o    | Power+   | Glide    | Flux     |
| --------- | -------- | -------- | --------------------------- | -------- | -------- | --------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- |
| 100       | 0.031 ms | 0.002 ms | 0.001 ms                    | 0.001 ms | 0.001 ms | 0.001 ms  | 0.001 ms | 0.054 ms | 0.001 ms | 0.007 ms | 0.001 ms | 0.002 ms | 0.001 ms | 0.001 ms | 0.001 ms | 0.001 ms | 0.002 ms |
| 1,000     | 0.043 ms | 0.033 ms | 0.010 ms                    | 0.008 ms | 0.006 ms | 0.007 ms  | 0.006 ms | 0.795 ms | 0.008 ms | 0.115 ms | 0.009 ms | 0.019 ms | 0.006 ms | 0.039 ms | 0.006 ms | 0.006 ms | 0.032 ms |
| 10,000    | 0.324 ms | 0.444 ms | 0.088 ms                    | 0.072 ms | 0.054 ms | 0.071 ms  | 0.053 ms | 10.5 ms  | 0.072 ms | 1.63 ms  | 0.090 ms | 0.190 ms | 0.059 ms | 0.612 ms | 0.054 ms | 0.053 ms | 0.439 ms |
| 100,000   | 3.99 ms  | 5.86 ms  | 0.899 ms                    | 0.712 ms | 0.554 ms | n/a       | 0.529 ms | 176 ms   | 0.709 ms | 21.4 ms  | 0.900 ms | 1.96 ms  | 0.606 ms | 7.59 ms  | 0.541 ms | 0.531 ms | 5.62 ms  |
| 1,000,000 | 47.8 ms  | 73.6 ms  | 8.78 ms                     | 7.26 ms  | 5.42 ms  | n/a       | 5.42 ms  | 2.11 s   | 7.30 ms  | 256 ms   | 8.88 ms  | 19.2 ms  | 6.38 ms  | 101 ms   | 5.41 ms  | 5.38 ms  | 65.9 ms  |

### Nearly Sorted (2% swaps)

| List Size | Ghost    | Meteor   | Pattern-Defeating QuickSort | Grail    | Power    | Insertion | Tim      | Jesse    | Green    | Ska      | Ipn      | Smooth   | Block    | IPS4o    | Power+   | Glide    | Flux     |
| --------- | -------- | -------- | --------------------------- | -------- | -------- | --------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- |
| 100       | 0.002 ms | 0.002 ms | 0.003 ms                    | 0.001 ms | 0.002 ms | 0.001 ms  | 0.001 ms | 0.054 ms | 0.001 ms | 0.007 ms | 0.002 ms | 0.002 ms | 0.001 ms | 0.002 ms | 0.002 ms | 0.001 ms | 0.002 ms |
| 1,000     | 0.026 ms | 0.031 ms | 0.037 ms                    | 0.008 ms | 0.017 ms | 0.007 ms  | 0.015 ms | 0.785 ms | 0.008 ms | 0.114 ms | 0.033 ms | 0.022 ms | 0.007 ms | 0.047 ms | 0.024 ms | 0.015 ms | 0.033 ms |
| 10,000    | 0.332 ms | 0.447 ms | 0.485 ms                    | 0.081 ms | 0.271 ms | 0.072 ms  | 0.195 ms | 10.3 ms  | 0.079 ms | 1.61 ms  | 0.456 ms | 0.197 ms | 0.078 ms | 0.702 ms | 0.411 ms | 0.174 ms | 0.438 ms |
| 100,000   | 4.00 ms  | 5.88 ms  | 6.02 ms                     | 0.848 ms | 3.81 ms  | n/a       | 2.66 ms  | 180 ms   | 0.815 ms | 21.3 ms  | 5.72 ms  | 2.01 ms  | 0.734 ms | 8.84 ms  | 5.94 ms  | 2.42 ms  | 5.74 ms  |
| 1,000,000 | 48.1 ms  | 75.2 ms  | 70.7 ms                     | 9.28 ms  | 52.2 ms  | n/a       | 28.3 ms  | 2.05 s   | 8.74 ms  | 258 ms   | 66.5 ms  | 20.0 ms  | 8.74 ms  | 113 ms   | 78.1 ms  | 20.0 ms  | 68.0 ms  |

### Shuffled (deterministic)

| List Size | Ghost    | Meteor   | Pattern-Defeating QuickSort | Grail    | Power    | Insertion | Tim      | Jesse    | Green    | Ska      | Ipn      | Smooth   | Block    | IPS4o    | Power+   | Glide    | Flux     |
| --------- | -------- | -------- | --------------------------- | -------- | -------- | --------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- | -------- |
| 100       | 0.009 ms | 0.008 ms | 0.006 ms                    | 0.007 ms | 0.007 ms | 0.019 ms  | 0.011 ms | 0.029 ms | 0.008 ms | 0.009 ms | 0.015 ms | 0.012 ms | 0.006 ms | 0.006 ms | 0.023 ms | 0.012 ms | 0.007 ms |
| 1,000     | 0.168 ms | 0.141 ms | 0.097 ms                    | 0.113 ms | 0.162 ms | 1.72 ms   | 0.158 ms | 0.402 ms | 0.118 ms | 0.144 ms | 0.105 ms | 0.209 ms | 0.093 ms | 0.116 ms | 0.437 ms | 0.175 ms | 0.106 ms |
| 10,000    | 2.36 ms  | 2.10 ms  | 1.34 ms                     | 1.58 ms  | 1.62 ms  | 170 ms    | 1.80 ms  | 5.52 ms  | 1.52 ms  | 2.06 ms  | 1.44 ms  | 3.04 ms  | 1.26 ms  | 1.77 ms  | 6.17 ms  | 2.10 ms  | 1.60 ms  |
| 100,000   | 34.9 ms  | 27.8 ms  | 16.6 ms                     | 19.1 ms  | 19.2 ms  | n/a       | 22.2 ms  | 68.8 ms  | 19.3 ms  | 25.8 ms  | 17.8 ms  | 39.9 ms  | 16.2 ms  | 21.9 ms  | 79.0 ms  | 27.4 ms  | 19.8 ms  |
| 1,000,000 | 499 ms   | 350 ms   | 198 ms                      | 231 ms   | 231 ms   | n/a       | 279 ms   | 899 ms   | 245 ms   | 322 ms   | 207 ms   | 492 ms   | 189 ms   | 258 ms   | 1.06 s   | 338 ms   | 234 ms   |

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
