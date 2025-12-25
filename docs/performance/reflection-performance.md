# Reflection Performance Benchmarks

Unity Helpers replaces ad-hoc reflection with cached delegates that favour expression lambdas on IL2CPP-safe platforms and fall back to dynamic IL emit or plain reflection where available. These benchmarks compare raw `System.Reflection` against the helpers for common access patterns.

Each run updates the table for the current operating system only. Sections that still show `_No benchmark data generated yet._` simply have not been executed on that platform.

## Windows

<!-- REFLECTION_PERFORMANCE_WINDOWS_START -->

Generated on 2025-12-25 07:10:56 UTC

### Strategy: Default (auto)

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 21.06M           | 7.16M                       | 2.94x                 |
| Instance Field Set (boxed)     | 23.23M           | 5.42M                       | 4.29x                 |
| Static Field Get (boxed)       | 23.07M           | 6.01M                       | 3.84x                 |
| Static Field Set (boxed)       | 23.34M           | 6.17M                       | 3.78x                 |
| Instance Property Get (boxed)  | 22.74M           | 21.29M                      | 1.07x                 |
| Instance Property Set (boxed)  | 23.57M           | 2.02M                       | 11.68x                |
| Static Property Get (boxed)    | 23.23M           | 22.92M                      | 1.01x                 |
| Static Property Set (boxed)    | 3.84M            | 897.4K                      | 4.28x                 |
| Instance Method Invoke (boxed) | 22.52M           | 1.94M                       | 11.60x                |
| Static Method Invoke (boxed)   | 21.74M           | 2.58M                       | 8.42x                 |
| Constructor Invoke (boxed)     | 21.40M           | 2.57M                       | 8.32x                 |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 595.57M          | 669.37M                     | 7.16M                       | 0.89x               | 83.20x                |
| Instance Field Set (typed)     | 633.95M          | 663.46M                     | 5.42M                       | 0.96x               | 116.98x               |
| Static Field Get (typed)       | 634.71M          | 687.62M                     | 6.01M                       | 0.92x               | 105.68x               |
| Static Field Set (typed)       | 632.48M          | 661.35M                     | 6.17M                       | 0.96x               | 102.54x               |
| Instance Property Get (typed)  | 570.10M          | 687.57M                     | 21.29M                      | 0.83x               | 26.77x                |
| Instance Property Set (typed)  | 629.82M          | 694.80M                     | 2.02M                       | 0.91x               | 312.15x               |
| Static Property Get (typed)    | 620.17M          | 683.29M                     | 22.92M                      | 0.91x               | 27.06x                |
| Static Property Set (typed)    | 619.82M          | 653.10M                     | 897.4K                      | 0.95x               | 690.69x               |
| Instance Method Invoke (typed) | 626.31M          | 681.23M                     | 1.94M                       | 0.92x               | 322.67x               |
| Static Method Invoke (typed)   | 604.11M          | 678.21M                     | 2.58M                       | 0.89x               | 234.00x               |

### Strategy: Expressions

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 24.85M           | 4.98M                       | 4.99x                 |
| Instance Field Set (boxed)     | 23.87M           | 5.42M                       | 4.40x                 |
| Static Field Get (boxed)       | 23.22M           | 8.61M                       | 2.70x                 |
| Static Field Set (boxed)       | 22.90M           | 6.20M                       | 3.69x                 |
| Instance Property Get (boxed)  | 23.49M           | 21.76M                      | 1.08x                 |
| Instance Property Set (boxed)  | 23.40M           | 2.03M                       | 11.53x                |
| Static Property Get (boxed)    | 21.53M           | 22.37M                      | 0.96x                 |
| Static Property Set (boxed)    | 22.76M           | 2.89M                       | 7.88x                 |
| Instance Method Invoke (boxed) | 22.07M           | 1.95M                       | 11.31x                |
| Static Method Invoke (boxed)   | 22.73M           | 2.68M                       | 8.48x                 |
| Constructor Invoke (boxed)     | 23.30M           | 2.56M                       | 9.09x                 |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 611.19M          | 673.45M                     | 4.98M                       | 0.91x               | 122.69x               |
| Instance Field Set (typed)     | 634.03M          | 664.03M                     | 5.42M                       | 0.95x               | 116.91x               |
| Static Field Get (typed)       | 629.62M          | 685.31M                     | 8.61M                       | 0.92x               | 73.09x                |
| Static Field Set (typed)       | 621.21M          | 658.76M                     | 6.20M                       | 0.94x               | 100.14x               |
| Instance Property Get (typed)  | 568.99M          | 687.60M                     | 21.76M                      | 0.83x               | 26.15x                |
| Instance Property Set (typed)  | 631.04M          | 696.82M                     | 2.03M                       | 0.91x               | 310.93x               |
| Static Property Get (typed)    | 619.87M          | 681.20M                     | 22.37M                      | 0.91x               | 27.71x                |
| Static Property Set (typed)    | 617.59M          | 655.72M                     | 2.89M                       | 0.94x               | 213.69x               |
| Instance Method Invoke (typed) | 626.65M          | 681.10M                     | 1.95M                       | 0.92x               | 321.18x               |
| Static Method Invoke (typed)   | 599.46M          | 677.00M                     | 2.68M                       | 0.89x               | 223.56x               |

### Strategy: Dynamic IL

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 23.59M           | 7.26M                       | 3.25x                 |
| Instance Field Set (boxed)     | 22.80M           | 5.40M                       | 4.22x                 |
| Static Field Get (boxed)       | 22.56M           | 6.12M                       | 3.68x                 |
| Static Field Set (boxed)       | 4.11M            | 1.99M                       | 2.07x                 |
| Instance Property Get (boxed)  | 24.53M           | 21.17M                      | 1.16x                 |
| Instance Property Set (boxed)  | 21.23M           | 2.03M                       | 10.47x                |
| Static Property Get (boxed)    | 23.62M           | 22.35M                      | 1.06x                 |
| Static Property Set (boxed)    | 23.33M           | 2.88M                       | 8.09x                 |
| Instance Method Invoke (boxed) | 22.09M           | 1.95M                       | 11.36x                |
| Static Method Invoke (boxed)   | 22.82M           | 2.66M                       | 8.57x                 |
| Constructor Invoke (boxed)     | 22.52M           | 2.55M                       | 8.81x                 |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 596.26M          | 666.42M                     | 7.26M                       | 0.89x               | 82.16x                |
| Instance Field Set (typed)     | 627.45M          | 658.96M                     | 5.40M                       | 0.95x               | 116.21x               |
| Static Field Get (typed)       | 629.88M          | 682.15M                     | 6.12M                       | 0.92x               | 102.87x               |
| Static Field Set (typed)       | 630.40M          | 661.66M                     | 1.99M                       | 0.95x               | 316.88x               |
| Instance Property Get (typed)  | 570.19M          | 690.25M                     | 21.17M                      | 0.83x               | 26.93x                |
| Instance Property Set (typed)  | 632.19M          | 693.64M                     | 2.03M                       | 0.91x               | 311.68x               |
| Static Property Get (typed)    | 622.38M          | 684.56M                     | 22.35M                      | 0.91x               | 27.85x                |
| Static Property Set (typed)    | 615.09M          | 658.53M                     | 2.88M                       | 0.93x               | 213.33x               |
| Instance Method Invoke (typed) | 626.10M          | 683.32M                     | 1.95M                       | 0.92x               | 321.86x               |
| Static Method Invoke (typed)   | 596.76M          | 680.65M                     | 2.66M                       | 0.88x               | 224.16x               |

### Strategy: Reflection Fallback

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 4.83M            | 7.28M                       | 0.66x                 |
| Instance Field Set (boxed)     | 5.41M            | 5.42M                       | 1.00x                 |
| Static Field Get (boxed)       | 5.72M            | 8.53M                       | 0.67x                 |
| Static Field Set (boxed)       | 5.81M            | 6.09M                       | 0.95x                 |
| Instance Property Get (boxed)  | 16.90M           | 21.40M                      | 0.79x                 |
| Instance Property Set (boxed)  | 2.09M            | 2.04M                       | 1.03x                 |
| Static Property Get (boxed)    | 20.78M           | 22.08M                      | 0.94x                 |
| Static Property Set (boxed)    | 2.88M            | 2.87M                       | 1.00x                 |
| Instance Method Invoke (boxed) | 1.30M            | 1.95M                       | 0.67x                 |
| Static Method Invoke (boxed)   | 2.68M            | 2.64M                       | 1.01x                 |
| Constructor Invoke (boxed)     | 2.55M            | 2.54M                       | 1.01x                 |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 7.33M            | 664.64M                     | 7.28M                       | 0.01x               | 1.01x                 |
| Instance Field Set (typed)     | 5.00M            | 658.53M                     | 5.42M                       | 0.01x               | 0.92x                 |
| Static Field Get (typed)       | 6.44M            | 681.70M                     | 8.53M                       | 0.01x               | 0.75x                 |
| Static Field Set (typed)       | 6.14M            | 661.08M                     | 6.09M                       | 0.01x               | 1.01x                 |
| Instance Property Get (typed)  | 568.66M          | 689.25M                     | 21.40M                      | 0.83x               | 26.57x                |
| Instance Property Set (typed)  | 628.51M          | 689.86M                     | 2.04M                       | 0.91x               | 308.55x               |
| Static Property Get (typed)    | 618.31M          | 678.83M                     | 22.08M                      | 0.91x               | 28.00x                |
| Static Property Set (typed)    | 616.37M          | 658.05M                     | 2.87M                       | 0.94x               | 214.50x               |
| Instance Method Invoke (typed) | 625.62M          | 681.39M                     | 1.95M                       | 0.92x               | 321.34x               |
| Static Method Invoke (typed)   | 594.19M          | 679.92M                     | 2.64M                       | 0.87x               | 224.99x               |

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
