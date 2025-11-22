# Reflection Performance Benchmarks

Unity Helpers replaces ad-hoc reflection with cached delegates that favour expression lambdas on IL2CPP-safe platforms and fall back to dynamic IL emit or plain reflection where available. These benchmarks compare raw `System.Reflection` against the helpers for common access patterns.

Each run updates the table for the current operating system only. Sections that still show `_No benchmark data generated yet._` simply have not been executed on that platform.

## Windows

<!-- REFLECTION_PERFORMANCE_WINDOWS_START -->

Generated on 2025-11-22 03:09:48 UTC

### Strategy: Default (auto)

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 19.74M           | 7.27M                       | 2.72x                 |
| Instance Field Set (boxed)     | 14.05M           | 2.41M                       | 5.84x                 |
| Static Field Get (boxed)       | 18.03M           | 8.53M                       | 2.12x                 |
| Static Field Set (boxed)       | 28.07M           | 5.02M                       | 5.60x                 |
| Instance Property Get (boxed)  | 26.99M           | 23.23M                      | 1.16x                 |
| Instance Property Set (boxed)  | 20.04M           | 2.00M                       | 10.03x                |
| Static Property Get (boxed)    | 23.98M           | 25.08M                      | 0.96x                 |
| Static Property Set (boxed)    | 21.71M           | 2.93M                       | 7.42x                 |
| Instance Method Invoke (boxed) | 23.37M           | 1.98M                       | 11.81x                |
| Static Method Invoke (boxed)   | 25.06M           | 2.13M                       | 11.77x                |
| Constructor Invoke (boxed)     | 26.21M           | 2.26M                       | 11.59x                |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 668.77M          | 686.54M                     | 7.27M                       | 0.97x               | 92.01x                |
| Instance Field Set (typed)     | 644.19M          | 666.18M                     | 2.41M                       | 0.97x               | 267.47x               |
| Static Field Get (typed)       | 676.81M          | 703.05M                     | 8.53M                       | 0.96x               | 79.39x                |
| Static Field Set (typed)       | 667.57M          | 665.66M                     | 5.02M                       | 1.00x               | 133.09x               |
| Instance Property Get (typed)  | 654.42M          | 700.05M                     | 23.23M                      | 0.93x               | 28.17x                |
| Instance Property Set (typed)  | 655.61M          | 700.53M                     | 2.00M                       | 0.94x               | 328.18x               |
| Static Property Get (typed)    | 657.90M          | 692.31M                     | 25.08M                      | 0.95x               | 26.24x                |
| Static Property Set (typed)    | 664.93M          | 662.11M                     | 2.93M                       | 1.00x               | 227.32x               |
| Instance Method Invoke (typed) | 623.84M          | 687.98M                     | 1.98M                       | 0.91x               | 315.10x               |
| Static Method Invoke (typed)   | 621.78M          | 684.68M                     | 2.13M                       | 0.91x               | 291.90x               |

### Strategy: Expressions

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 26.16M           | 5.44M                       | 4.81x                 |
| Instance Field Set (boxed)     | 27.21M           | 5.40M                       | 5.04x                 |
| Static Field Get (boxed)       | 19.38M           | 2.99M                       | 6.49x                 |
| Static Field Set (boxed)       | 9.65M            | 6.30M                       | 1.53x                 |
| Instance Property Get (boxed)  | 21.84M           | 24.44M                      | 0.89x                 |
| Instance Property Set (boxed)  | 27.53M           | 2.00M                       | 13.78x                |
| Static Property Get (boxed)    | 18.32M           | 24.53M                      | 0.75x                 |
| Static Property Set (boxed)    | 26.64M           | 2.66M                       | 10.02x                |
| Instance Method Invoke (boxed) | 18.03M           | 1.96M                       | 9.18x                 |
| Static Method Invoke (boxed)   | 25.74M           | 2.63M                       | 9.79x                 |
| Constructor Invoke (boxed)     | 24.60M           | 1.80M                       | 13.68x                |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 669.76M          | 687.41M                     | 5.44M                       | 0.97x               | 123.22x               |
| Instance Field Set (typed)     | 641.12M          | 672.07M                     | 5.40M                       | 0.95x               | 118.67x               |
| Static Field Get (typed)       | 680.26M          | 702.37M                     | 2.99M                       | 0.97x               | 227.68x               |
| Static Field Set (typed)       | 671.67M          | 665.40M                     | 6.30M                       | 1.01x               | 106.56x               |
| Instance Property Get (typed)  | 642.91M          | 697.52M                     | 24.44M                      | 0.92x               | 26.30x                |
| Instance Property Set (typed)  | 654.20M          | 701.45M                     | 2.00M                       | 0.93x               | 327.35x               |
| Static Property Get (typed)    | 666.34M          | 692.47M                     | 24.53M                      | 0.96x               | 27.17x                |
| Static Property Set (typed)    | 664.05M          | 660.47M                     | 2.66M                       | 1.01x               | 249.91x               |
| Instance Method Invoke (typed) | 621.35M          | 687.56M                     | 1.96M                       | 0.90x               | 316.22x               |
| Static Method Invoke (typed)   | 628.63M          | 683.70M                     | 2.63M                       | 0.92x               | 239.06x               |

### Strategy: Dynamic IL

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 28.06M           | 7.26M                       | 3.86x                 |
| Instance Field Set (boxed)     | 20.90M           | 5.54M                       | 3.78x                 |
| Static Field Get (boxed)       | 26.39M           | 6.53M                       | 4.04x                 |
| Static Field Set (boxed)       | 27.85M           | 6.31M                       | 4.41x                 |
| Instance Property Get (boxed)  | 6.68M            | 10.83M                      | 0.62x                 |
| Instance Property Set (boxed)  | 27.78M           | 1.53M                       | 18.14x                |
| Static Property Get (boxed)    | 25.89M           | 23.77M                      | 1.09x                 |
| Static Property Set (boxed)    | 24.53M           | 2.35M                       | 10.46x                |
| Instance Method Invoke (boxed) | 21.06M           | 1.95M                       | 10.80x                |
| Static Method Invoke (boxed)   | 24.03M           | 2.66M                       | 9.03x                 |
| Constructor Invoke (boxed)     | 20.52M           | 2.29M                       | 8.96x                 |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 668.89M          | 686.72M                     | 7.26M                       | 0.97x               | 92.12x                |
| Instance Field Set (typed)     | 651.35M          | 672.87M                     | 5.54M                       | 0.97x               | 117.68x               |
| Static Field Get (typed)       | 661.99M          | 701.02M                     | 6.53M                       | 0.94x               | 101.38x               |
| Static Field Set (typed)       | 666.13M          | 665.46M                     | 6.31M                       | 1.00x               | 105.48x               |
| Instance Property Get (typed)  | 666.69M          | 700.03M                     | 10.83M                      | 0.95x               | 61.58x                |
| Instance Property Set (typed)  | 656.19M          | 697.05M                     | 1.53M                       | 0.94x               | 428.40x               |
| Static Property Get (typed)    | 664.03M          | 693.75M                     | 23.77M                      | 0.96x               | 27.93x                |
| Static Property Set (typed)    | 667.37M          | 662.31M                     | 2.35M                       | 1.01x               | 284.55x               |
| Instance Method Invoke (typed) | 636.51M          | 691.47M                     | 1.95M                       | 0.92x               | 326.50x               |
| Static Method Invoke (typed)   | 619.17M          | 684.96M                     | 2.66M                       | 0.90x               | 232.54x               |

### Strategy: Reflection Fallback

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 7.31M            | 7.25M                       | 1.01x                 |
| Instance Field Set (boxed)     | 4.31M            | 5.56M                       | 0.78x                 |
| Static Field Get (boxed)       | 8.59M            | 6.67M                       | 1.29x                 |
| Static Field Set (boxed)       | 6.12M            | 6.32M                       | 0.97x                 |
| Instance Property Get (boxed)  | 19.58M           | 24.88M                      | 0.79x                 |
| Instance Property Set (boxed)  | 2.00M            | 2.03M                       | 0.99x                 |
| Static Property Get (boxed)    | 20.47M           | 22.09M                      | 0.93x                 |
| Static Property Set (boxed)    | 2.37M            | 2.89M                       | 0.82x                 |
| Instance Method Invoke (boxed) | 1.98M            | 1.97M                       | 1.01x                 |
| Static Method Invoke (boxed)   | 2.55M            | 1.90M                       | 1.34x                 |
| Constructor Invoke (boxed)     | 2.32M            | 2.30M                       | 1.01x                 |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 7.32M            | 684.86M                     | 7.25M                       | 0.01x               | 1.01x                 |
| Instance Field Set (typed)     | 5.60M            | 671.20M                     | 5.56M                       | 0.01x               | 1.01x                 |
| Static Field Get (typed)       | 2.80M            | 689.72M                     | 6.67M                       | 0.00x               | 0.42x                 |
| Static Field Set (typed)       | 2.48M            | 669.19M                     | 6.32M                       | 0.00x               | 0.39x                 |
| Instance Property Get (typed)  | 645.69M          | 700.94M                     | 24.88M                      | 0.92x               | 25.95x                |
| Instance Property Set (typed)  | 654.94M          | 699.73M                     | 2.03M                       | 0.94x               | 323.34x               |
| Static Property Get (typed)    | 673.01M          | 694.08M                     | 22.09M                      | 0.97x               | 30.46x                |
| Static Property Set (typed)    | 668.75M          | 665.20M                     | 2.89M                       | 1.01x               | 231.19x               |
| Instance Method Invoke (typed) | 639.21M          | 689.52M                     | 1.97M                       | 0.93x               | 324.10x               |
| Static Method Invoke (typed)   | 621.78M          | 684.80M                     | 1.90M                       | 0.91x               | 326.86x               |

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
