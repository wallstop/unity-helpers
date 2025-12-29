---
---

# Reflection Performance Benchmarks

Unity Helpers replaces ad-hoc reflection with cached delegates that favour expression lambdas on IL2CPP-safe platforms and fall back to dynamic IL emit or plain reflection where available. These benchmarks compare raw `System.Reflection` against the helpers for common access patterns.

Each run updates the table for the current operating system only. Sections that still show `_No benchmark data generated yet._` simply have not been executed on that platform.

## Windows

<!-- REFLECTION_PERFORMANCE_WINDOWS_START -->

Generated on 2025-12-28 04:07:15 UTC

### Strategy: Default (auto)

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 20.08M           | 7.11M                       | 2.83x                 |
| Instance Field Set (boxed)     | 21.69M           | 5.37M                       | 4.04x                 |
| Static Field Get (boxed)       | 22.11M           | 8.60M                       | 2.57x                 |
| Static Field Set (boxed)       | 23.12M           | 6.10M                       | 3.79x                 |
| Instance Property Get (boxed)  | 22.84M           | 21.65M                      | 1.05x                 |
| Instance Property Set (boxed)  | 23.87M           | 2.07M                       | 11.54x                |
| Static Property Get (boxed)    | 24.17M           | 30.69M                      | 0.79x                 |
| Static Property Set (boxed)    | 25.80M           | 2.04M                       | 12.66x                |
| Instance Method Invoke (boxed) | 30.54M           | 1.41M                       | 21.65x                |
| Static Method Invoke (boxed)   | 29.34M           | 2.14M                       | 13.73x                |
| Constructor Invoke (boxed)     | 28.12M           | 1.47M                       | 19.11x                |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 663.86M          | 671.11M                     | 7.11M                       | 0.99x               | 93.40x                |
| Instance Field Set (typed)     | 652.31M          | 707.21M                     | 5.37M                       | 0.92x               | 121.48x               |
| Static Field Get (typed)       | 653.56M          | 702.19M                     | 8.60M                       | 0.93x               | 75.95x                |
| Static Field Set (typed)       | 665.59M          | 718.36M                     | 6.10M                       | 0.93x               | 109.10x               |
| Instance Property Get (typed)  | 671.55M          | 705.55M                     | 21.65M                      | 0.95x               | 31.02x                |
| Instance Property Set (typed)  | 664.79M          | 702.45M                     | 2.07M                       | 0.95x               | 321.27x               |
| Static Property Get (typed)    | 649.02M          | 705.64M                     | 30.69M                      | 0.92x               | 21.15x                |
| Static Property Set (typed)    | 669.18M          | 695.59M                     | 2.04M                       | 0.96x               | 328.37x               |
| Instance Method Invoke (typed) | 626.55M          | 682.12M                     | 1.41M                       | 0.92x               | 444.25x               |
| Static Method Invoke (typed)   | 655.31M          | 694.11M                     | 2.14M                       | 0.94x               | 306.58x               |

### Strategy: Expressions

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 3.51M            | 1.25M                       | 2.81x                 |
| Instance Field Set (boxed)     | 7.65M            | 5.42M                       | 1.41x                 |
| Static Field Get (boxed)       | 21.73M           | 8.65M                       | 2.51x                 |
| Static Field Set (boxed)       | 23.07M           | 6.14M                       | 3.76x                 |
| Instance Property Get (boxed)  | 21.72M           | 30.55M                      | 0.71x                 |
| Instance Property Set (boxed)  | 25.09M           | 2.05M                       | 12.22x                |
| Static Property Get (boxed)    | 21.64M           | 19.87M                      | 1.09x                 |
| Static Property Set (boxed)    | 22.67M           | 2.94M                       | 7.72x                 |
| Instance Method Invoke (boxed) | 20.55M           | 1.94M                       | 10.59x                |
| Static Method Invoke (boxed)   | 28.64M           | 2.18M                       | 13.13x                |
| Constructor Invoke (boxed)     | 28.49M           | 2.02M                       | 14.13x                |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 665.23M          | 670.91M                     | 1.25M                       | 0.99x               | 532.60x               |
| Instance Field Set (typed)     | 644.26M          | 677.03M                     | 5.42M                       | 0.95x               | 118.93x               |
| Static Field Get (typed)       | 657.46M          | 684.99M                     | 8.65M                       | 0.96x               | 76.02x                |
| Static Field Set (typed)       | 658.34M          | 765.59M                     | 6.14M                       | 0.86x               | 107.28x               |
| Instance Property Get (typed)  | 671.90M          | 688.82M                     | 30.55M                      | 0.98x               | 22.00x                |
| Instance Property Set (typed)  | 659.71M          | 685.09M                     | 2.05M                       | 0.96x               | 321.46x               |
| Static Property Get (typed)    | 654.18M          | 744.91M                     | 19.87M                      | 0.88x               | 32.92x                |
| Static Property Set (typed)    | 666.91M          | 664.26M                     | 2.94M                       | 1.00x               | 227.10x               |
| Instance Method Invoke (typed) | 621.03M          | 682.41M                     | 1.94M                       | 0.91x               | 320.08x               |
| Static Method Invoke (typed)   | 651.79M          | 684.81M                     | 2.18M                       | 0.95x               | 298.86x               |

### Strategy: Dynamic IL

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 28.88M           | 5.62M                       | 5.13x                 |
| Instance Field Set (boxed)     | 22.39M           | 5.38M                       | 4.16x                 |
| Static Field Get (boxed)       | 23.23M           | 8.63M                       | 2.69x                 |
| Static Field Set (boxed)       | 22.21M           | 6.08M                       | 3.65x                 |
| Instance Property Get (boxed)  | 23.22M           | 30.57M                      | 0.76x                 |
| Instance Property Set (boxed)  | 25.64M           | 2.05M                       | 12.51x                |
| Static Property Get (boxed)    | 21.53M           | 19.85M                      | 1.08x                 |
| Static Property Set (boxed)    | 21.28M           | 2.90M                       | 7.33x                 |
| Instance Method Invoke (boxed) | 23.59M           | 1.32M                       | 17.89x                |
| Static Method Invoke (boxed)   | 3.72M            | 868.3K                      | 4.29x                 |
| Constructor Invoke (boxed)     | 10.45M           | 2.53M                       | 4.13x                 |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 655.46M          | 681.96M                     | 5.62M                       | 0.96x               | 116.53x               |
| Instance Field Set (typed)     | 648.29M          | 676.08M                     | 5.38M                       | 0.96x               | 120.58x               |
| Static Field Get (typed)       | 653.08M          | 683.68M                     | 8.63M                       | 0.96x               | 75.72x                |
| Static Field Set (typed)       | 667.92M          | 661.25M                     | 6.08M                       | 1.01x               | 109.90x               |
| Instance Property Get (typed)  | 675.32M          | 767.07M                     | 30.57M                      | 0.88x               | 22.09x                |
| Instance Property Set (typed)  | 657.65M          | 698.90M                     | 2.05M                       | 0.94x               | 320.98x               |
| Static Property Get (typed)    | 653.13M          | 719.17M                     | 19.85M                      | 0.91x               | 32.90x                |
| Static Property Set (typed)    | 670.54M          | 660.27M                     | 2.90M                       | 1.02x               | 230.83x               |
| Instance Method Invoke (typed) | 629.16M          | 683.29M                     | 1.32M                       | 0.92x               | 477.12x               |
| Static Method Invoke (typed)   | 654.90M          | 697.62M                     | 868.3K                      | 0.94x               | 754.19x               |

### Strategy: Reflection Fallback

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 7.26M            | 7.28M                       | 1.00x                 |
| Instance Field Set (boxed)     | 5.08M            | 3.99M                       | 1.27x                 |
| Static Field Get (boxed)       | 8.59M            | 8.65M                       | 0.99x                 |
| Static Field Set (boxed)       | 6.07M            | 3.99M                       | 1.52x                 |
| Instance Property Get (boxed)  | 30.60M           | 22.57M                      | 1.36x                 |
| Instance Property Set (boxed)  | 2.05M            | 1.37M                       | 1.49x                 |
| Static Property Get (boxed)    | 30.62M           | 21.71M                      | 1.41x                 |
| Static Property Set (boxed)    | 2.87M            | 1.81M                       | 1.58x                 |
| Instance Method Invoke (boxed) | 1.93M            | 1.94M                       | 1.00x                 |
| Static Method Invoke (boxed)   | 2.62M            | 2.67M                       | 0.98x                 |
| Constructor Invoke (boxed)     | 2.51M            | 2.52M                       | 1.00x                 |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 7.20M            | 734.18M                     | 7.28M                       | 0.01x               | 0.99x                 |
| Instance Field Set (typed)     | 5.25M            | 677.79M                     | 3.99M                       | 0.01x               | 1.32x                 |
| Static Field Get (typed)       | 5.47M            | 705.29M                     | 8.65M                       | 0.01x               | 0.63x                 |
| Static Field Set (typed)       | 6.00M            | 698.12M                     | 3.99M                       | 0.01x               | 1.50x                 |
| Instance Property Get (typed)  | 675.00M          | 846.23M                     | 22.57M                      | 0.80x               | 29.90x                |
| Instance Property Set (typed)  | 660.66M          | 706.11M                     | 1.37M                       | 0.94x               | 482.31x               |
| Static Property Get (typed)    | 655.51M          | 821.36M                     | 21.71M                      | 0.80x               | 30.19x                |
| Static Property Set (typed)    | 662.58M          | 704.01M                     | 1.81M                       | 0.94x               | 365.45x               |
| Instance Method Invoke (typed) | 598.54M          | 712.80M                     | 1.94M                       | 0.84x               | 308.09x               |
| Static Method Invoke (typed)   | 656.81M          | 676.66M                     | 2.67M                       | 0.97x               | 246.15x               |

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
