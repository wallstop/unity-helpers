# Reflection Performance Benchmarks

Unity Helpers replaces ad-hoc reflection with cached delegates that favour expression lambdas on IL2CPP-safe platforms and fall back to dynamic IL emit or plain reflection where available. These benchmarks compare raw `System.Reflection` against the helpers for common access patterns.

Each run updates the table for the current operating system only. Sections that still show `_No benchmark data generated yet._` simply have not been executed on that platform.

## Windows

<!-- REFLECTION_PERFORMANCE_WINDOWS_START -->

Generated on 2025-11-25 22:59:16 UTC

### Strategy: Default (auto)

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 15.93M           | 7.08M                       | 2.25x                 |
| Instance Field Set (boxed)     | 19.90M           | 5.45M                       | 3.65x                 |
| Static Field Get (boxed)       | 19.70M           | 8.45M                       | 2.33x                 |
| Static Field Set (boxed)       | 13.00M           | 6.18M                       | 2.10x                 |
| Instance Property Get (boxed)  | 19.39M           | 15.34M                      | 1.26x                 |
| Instance Property Set (boxed)  | 20.45M           | 1.96M                       | 10.41x                |
| Static Property Get (boxed)    | 16.44M           | 14.11M                      | 1.16x                 |
| Static Property Set (boxed)    | 17.23M           | 2.76M                       | 6.25x                 |
| Instance Method Invoke (boxed) | 15.73M           | 1.96M                       | 8.01x                 |
| Static Method Invoke (boxed)   | 19.62M           | 2.68M                       | 7.33x                 |
| Constructor Invoke (boxed)     | 20.81M           | 2.53M                       | 8.23x                 |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 667.45M          | 652.17M                     | 7.08M                       | 1.02x               | 94.32x                |
| Instance Field Set (typed)     | 630.02M          | 656.77M                     | 5.45M                       | 0.96x               | 115.50x               |
| Static Field Get (typed)       | 659.82M          | 680.16M                     | 8.45M                       | 0.97x               | 78.12x                |
| Static Field Set (typed)       | 655.07M          | 648.93M                     | 6.18M                       | 1.01x               | 105.98x               |
| Instance Property Get (typed)  | 636.76M          | 683.39M                     | 15.34M                      | 0.93x               | 41.51x                |
| Instance Property Set (typed)  | 637.03M          | 686.89M                     | 1.96M                       | 0.93x               | 324.30x               |
| Static Property Get (typed)    | 647.93M          | 683.52M                     | 14.11M                      | 0.95x               | 45.92x                |
| Static Property Set (typed)    | 660.97M          | 655.33M                     | 2.76M                       | 1.01x               | 239.69x               |
| Instance Method Invoke (typed) | 615.51M          | 680.25M                     | 1.96M                       | 0.90x               | 313.70x               |
| Static Method Invoke (typed)   | 618.78M          | 678.20M                     | 2.68M                       | 0.91x               | 231.03x               |

### Strategy: Expressions

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 20.31M           | 7.13M                       | 2.85x                 |
| Instance Field Set (boxed)     | 19.78M           | 5.42M                       | 3.65x                 |
| Static Field Get (boxed)       | 22.79M           | 5.74M                       | 3.97x                 |
| Static Field Set (boxed)       | 22.38M           | 6.10M                       | 3.67x                 |
| Instance Property Get (boxed)  | 21.66M           | 8.55M                       | 2.53x                 |
| Instance Property Set (boxed)  | 3.21M            | 358.7K                      | 8.95x                 |
| Static Property Get (boxed)    | 20.31M           | 20.52M                      | 0.99x                 |
| Static Property Set (boxed)    | 21.65M           | 2.77M                       | 7.82x                 |
| Instance Method Invoke (boxed) | 15.10M           | 1.91M                       | 7.90x                 |
| Static Method Invoke (boxed)   | 18.82M           | 2.65M                       | 7.10x                 |
| Constructor Invoke (boxed)     | 19.52M           | 2.50M                       | 7.80x                 |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 667.27M          | 649.72M                     | 7.13M                       | 1.03x               | 93.60x                |
| Instance Field Set (typed)     | 623.70M          | 647.51M                     | 5.42M                       | 0.96x               | 115.07x               |
| Static Field Get (typed)       | 645.99M          | 672.12M                     | 5.74M                       | 0.96x               | 112.48x               |
| Static Field Set (typed)       | 641.48M          | 663.67M                     | 6.10M                       | 0.97x               | 105.14x               |
| Instance Property Get (typed)  | 639.45M          | 676.52M                     | 8.55M                       | 0.95x               | 74.78x                |
| Instance Property Set (typed)  | 632.79M          | 691.15M                     | 358.7K                      | 0.92x               | 1763.97x              |
| Static Property Get (typed)    | 653.52M          | 699.04M                     | 20.52M                      | 0.93x               | 31.84x                |
| Static Property Set (typed)    | 667.75M          | 666.52M                     | 2.77M                       | 1.00x               | 241.21x               |
| Instance Method Invoke (typed) | 628.46M          | 687.69M                     | 1.91M                       | 0.91x               | 328.86x               |
| Static Method Invoke (typed)   | 626.33M          | 686.65M                     | 2.65M                       | 0.91x               | 236.12x               |

### Strategy: Dynamic IL

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 23.81M           | 7.21M                       | 3.30x                 |
| Instance Field Set (boxed)     | 22.95M           | 3.86M                       | 5.95x                 |
| Static Field Get (boxed)       | 23.66M           | 8.64M                       | 2.74x                 |
| Static Field Set (boxed)       | 23.85M           | 5.61M                       | 4.25x                 |
| Instance Property Get (boxed)  | 18.48M           | 21.56M                      | 0.86x                 |
| Instance Property Set (boxed)  | 22.31M           | 1.99M                       | 11.19x                |
| Static Property Get (boxed)    | 20.06M           | 21.94M                      | 0.91x                 |
| Static Property Set (boxed)    | 24.26M           | 2.88M                       | 8.42x                 |
| Instance Method Invoke (boxed) | 18.12M           | 1.97M                       | 9.20x                 |
| Static Method Invoke (boxed)   | 7.44M            | 1.60M                       | 4.66x                 |
| Constructor Invoke (boxed)     | 1.65M            | 2.59M                       | 0.64x                 |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 683.05M          | 669.38M                     | 7.21M                       | 1.02x               | 94.77x                |
| Instance Field Set (typed)     | 651.86M          | 666.55M                     | 3.86M                       | 0.98x               | 168.88x               |
| Static Field Get (typed)       | 649.15M          | 680.10M                     | 8.64M                       | 0.95x               | 75.14x                |
| Static Field Set (typed)       | 653.78M          | 653.37M                     | 5.61M                       | 1.00x               | 116.62x               |
| Instance Property Get (typed)  | 654.13M          | 694.27M                     | 21.56M                      | 0.94x               | 30.35x                |
| Instance Property Set (typed)  | 650.88M          | 705.39M                     | 1.99M                       | 0.92x               | 326.33x               |
| Static Property Get (typed)    | 653.96M          | 695.75M                     | 21.94M                      | 0.94x               | 29.81x                |
| Static Property Set (typed)    | 651.04M          | 658.99M                     | 2.88M                       | 0.99x               | 225.94x               |
| Instance Method Invoke (typed) | 612.05M          | 687.17M                     | 1.97M                       | 0.89x               | 310.68x               |
| Static Method Invoke (typed)   | 634.11M          | 685.81M                     | 1.60M                       | 0.92x               | 396.82x               |

### Strategy: Reflection Fallback

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 7.10M            | 7.19M                       | 0.99x                 |
| Instance Field Set (boxed)     | 3.71M            | 5.56M                       | 0.67x                 |
| Static Field Get (boxed)       | 8.67M            | 6.10M                       | 1.42x                 |
| Static Field Set (boxed)       | 6.15M            | 6.19M                       | 0.99x                 |
| Instance Property Get (boxed)  | 22.19M           | 22.52M                      | 0.99x                 |
| Instance Property Set (boxed)  | 2.04M            | 1.09M                       | 1.87x                 |
| Static Property Get (boxed)    | 19.78M           | 20.91M                      | 0.95x                 |
| Static Property Set (boxed)    | 2.76M            | 2.88M                       | 0.96x                 |
| Instance Method Invoke (boxed) | 1.90M            | 1.93M                       | 0.98x                 |
| Static Method Invoke (boxed)   | 2.58M            | 1.58M                       | 1.63x                 |
| Constructor Invoke (boxed)     | 2.60M            | 2.60M                       | 1.00x                 |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 7.35M            | 668.71M                     | 7.19M                       | 0.01x               | 1.02x                 |
| Instance Field Set (typed)     | 5.42M            | 653.11M                     | 5.56M                       | 0.01x               | 0.97x                 |
| Static Field Get (typed)       | 5.73M            | 680.73M                     | 6.10M                       | 0.01x               | 0.94x                 |
| Static Field Set (typed)       | 6.15M            | 650.14M                     | 6.19M                       | 0.01x               | 0.99x                 |
| Instance Property Get (typed)  | 656.17M          | 694.56M                     | 22.52M                      | 0.94x               | 29.14x                |
| Instance Property Set (typed)  | 651.13M          | 702.64M                     | 1.09M                       | 0.93x               | 597.21x               |
| Static Property Get (typed)    | 627.72M          | 693.69M                     | 20.91M                      | 0.90x               | 30.02x                |
| Static Property Set (typed)    | 657.50M          | 655.75M                     | 2.88M                       | 1.00x               | 228.36x               |
| Instance Method Invoke (typed) | 608.69M          | 673.80M                     | 1.93M                       | 0.90x               | 315.11x               |
| Static Method Invoke (typed)   | 618.64M          | 672.53M                     | 1.58M                       | 0.92x               | 392.02x               |

<!-- REFLECTION_PERFORMANCE_WINDOWS_END -->

## macOS

<!-- REFLECTION_PERFORMANCE_MACOS_START -->

_No benchmark data generated yet._

<!-- REFLECTION_PERFORMANCE_MACOS_END -->

## Linux

<!-- REFLECTION_PERFORMANCE_LINUX_START -->

_No benchmark data generated yet._

<!-- REFLECTION_PERFORMANCE_LINUX_END -->

## Unknown / Other

<!-- REFLECTION_PERFORMANCE_UNKNOWN_START -->

_No benchmark data generated yet._

<!-- REFLECTION_PERFORMANCE_UNKNOWN_END -->
