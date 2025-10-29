# Reflection Performance Benchmarks

Unity Helpers replaces ad-hoc reflection with cached delegates that favour expression lambdas on IL2CPP-safe platforms and fall back to dynamic IL emit or plain reflection where available. These benchmarks compare raw `System.Reflection` against the helpers for common access patterns.

Each run updates the table for the current operating system only. Sections that still show `_No benchmark data generated yet._` simply have not been executed on that platform.

## Windows

<!-- REFLECTION_PERFORMANCE_WINDOWS_START -->

Generated on 2025-10-29 01:51:16 UTC

### Strategy: Default (auto)

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 6.28M            | 3.23M                       | 1.94x                 |
| Instance Field Set (boxed)     | 27.12M           | 5.56M                       | 4.88x                 |
| Static Field Get (boxed)       | 23.06M           | 8.68M                       | 2.66x                 |
| Static Field Set (boxed)       | 26.91M           | 5.51M                       | 4.89x                 |
| Instance Property Get (boxed)  | 29.73M           | 21.35M                      | 1.39x                 |
| Instance Property Set (boxed)  | 29.85M           | 2.08M                       | 14.38x                |
| Static Property Get (boxed)    | 21.28M           | 24.12M                      | 0.88x                 |
| Static Property Set (boxed)    | 28.39M           | 2.17M                       | 13.06x                |
| Instance Method Invoke (boxed) | 26.25M           | 1.96M                       | 13.36x                |
| Static Method Invoke (boxed)   | 23.69M           | 2.75M                       | 8.61x                 |
| Constructor Invoke (boxed)     | 28.42M           | 2.59M                       | 10.95x                |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 610.87M          | 674.51M                     | 3.23M                       | 0.91x               | 188.92x               |
| Instance Field Set (typed)     | 654.80M          | 666.60M                     | 5.56M                       | 0.98x               | 117.80x               |
| Static Field Get (typed)       | 626.41M          | 692.87M                     | 8.68M                       | 0.90x               | 72.15x                |
| Static Field Set (typed)       | 675.50M          | 663.17M                     | 5.51M                       | 1.02x               | 122.67x               |
| Instance Property Get (typed)  | 656.72M          | 698.22M                     | 21.35M                      | 0.94x               | 30.75x                |
| Instance Property Set (typed)  | 664.69M          | 703.28M                     | 2.08M                       | 0.95x               | 320.30x               |
| Static Property Get (typed)    | 617.71M          | 691.83M                     | 24.12M                      | 0.89x               | 25.61x                |
| Static Property Set (typed)    | 675.33M          | 663.45M                     | 2.17M                       | 1.02x               | 310.65x               |
| Instance Method Invoke (typed) | 652.37M          | 687.35M                     | 1.96M                       | 0.95x               | 332.14x               |
| Static Method Invoke (typed)   | 625.09M          | 685.33M                     | 2.75M                       | 0.91x               | 227.19x               |

### Strategy: Expressions

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 22.20M           | 2.97M                       | 7.48x                 |
| Instance Field Set (boxed)     | 17.49M           | 4.57M                       | 3.83x                 |
| Static Field Get (boxed)       | 29.02M           | 7.13M                       | 4.07x                 |
| Static Field Set (boxed)       | 28.79M           | 6.29M                       | 4.58x                 |
| Instance Property Get (boxed)  | 23.15M           | 27.19M                      | 0.85x                 |
| Instance Property Set (boxed)  | 23.83M           | 2.07M                       | 11.49x                |
| Static Property Get (boxed)    | 22.80M           | 27.08M                      | 0.84x                 |
| Static Property Set (boxed)    | 22.19M           | 2.94M                       | 7.56x                 |
| Instance Method Invoke (boxed) | 22.72M           | 1.96M                       | 11.57x                |
| Static Method Invoke (boxed)   | 22.04M           | 2.74M                       | 8.03x                 |
| Constructor Invoke (boxed)     | 28.31M           | 2.60M                       | 10.88x                |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 605.77M          | 671.38M                     | 2.97M                       | 0.90x               | 204.04x               |
| Instance Field Set (typed)     | 654.72M          | 666.95M                     | 4.57M                       | 0.98x               | 143.31x               |
| Static Field Get (typed)       | 625.10M          | 693.40M                     | 7.13M                       | 0.90x               | 87.66x                |
| Static Field Set (typed)       | 674.61M          | 662.65M                     | 6.29M                       | 1.02x               | 107.24x               |
| Instance Property Get (typed)  | 655.38M          | 695.54M                     | 27.19M                      | 0.94x               | 24.10x                |
| Instance Property Set (typed)  | 655.70M          | 657.58M                     | 2.07M                       | 1.00x               | 316.18x               |
| Static Property Get (typed)    | 617.82M          | 694.04M                     | 27.08M                      | 0.89x               | 22.81x                |
| Static Property Set (typed)    | 679.95M          | 663.56M                     | 2.94M                       | 1.02x               | 231.52x               |
| Instance Method Invoke (typed) | 635.32M          | 687.22M                     | 1.96M                       | 0.92x               | 323.44x               |
| Static Method Invoke (typed)   | 629.71M          | 685.80M                     | 2.74M                       | 0.92x               | 229.58x               |

### Strategy: Dynamic IL

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 22.87M           | 7.40M                       | 3.09x                 |
| Instance Field Set (boxed)     | 12.75M           | 2.28M                       | 5.59x                 |
| Static Field Get (boxed)       | 24.12M           | 8.69M                       | 2.78x                 |
| Static Field Set (boxed)       | 22.37M           | 6.31M                       | 3.55x                 |
| Instance Property Get (boxed)  | 28.69M           | 21.26M                      | 1.35x                 |
| Instance Property Set (boxed)  | 29.54M           | 2.08M                       | 14.17x                |
| Static Property Get (boxed)    | 21.76M           | 23.34M                      | 0.93x                 |
| Static Property Set (boxed)    | 28.61M           | 1.99M                       | 14.35x                |
| Instance Method Invoke (boxed) | 27.05M           | 1.97M                       | 13.71x                |
| Static Method Invoke (boxed)   | 24.10M           | 2.74M                       | 8.79x                 |
| Constructor Invoke (boxed)     | 27.48M           | 2.59M                       | 10.62x                |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 620.18M          | 622.51M                     | 7.40M                       | 1.00x               | 83.83x                |
| Instance Field Set (typed)     | 658.82M          | 665.80M                     | 2.28M                       | 0.99x               | 288.75x               |
| Static Field Get (typed)       | 620.82M          | 695.14M                     | 8.69M                       | 0.89x               | 71.43x                |
| Static Field Set (typed)       | 679.32M          | 663.78M                     | 6.31M                       | 1.02x               | 107.70x               |
| Instance Property Get (typed)  | 660.71M          | 698.15M                     | 21.26M                      | 0.95x               | 31.08x                |
| Instance Property Set (typed)  | 663.28M          | 684.14M                     | 2.08M                       | 0.97x               | 318.27x               |
| Static Property Get (typed)    | 617.90M          | 694.08M                     | 23.34M                      | 0.89x               | 26.47x                |
| Static Property Set (typed)    | 684.18M          | 663.53M                     | 1.99M                       | 1.03x               | 343.05x               |
| Instance Method Invoke (typed) | 640.73M          | 687.72M                     | 1.97M                       | 0.93x               | 324.78x               |
| Static Method Invoke (typed)   | 636.04M          | 684.31M                     | 2.74M                       | 0.93x               | 232.03x               |

### Strategy: Reflection Fallback

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 6.05M            | 7.37M                       | 0.82x                 |
| Instance Field Set (boxed)     | 5.50M            | 4.54M                       | 1.21x                 |
| Static Field Get (boxed)       | 8.78M            | 8.70M                       | 1.01x                 |
| Static Field Set (boxed)       | 4.98M            | 6.31M                       | 0.79x                 |
| Instance Property Get (boxed)  | 10.59M           | 13.51M                      | 0.78x                 |
| Instance Property Set (boxed)  | 2.09M            | 1.42M                       | 1.46x                 |
| Static Property Get (boxed)    | 26.68M           | 27.81M                      | 0.96x                 |
| Static Property Set (boxed)    | 2.31M            | 2.96M                       | 0.78x                 |
| Instance Method Invoke (boxed) | 1.95M            | 1.97M                       | 0.99x                 |
| Static Method Invoke (boxed)   | 2.73M            | 1.93M                       | 1.41x                 |
| Constructor Invoke (boxed)     | 2.60M            | 2.60M                       | 1.00x                 |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 7.51M            | 654.81M                     | 7.37M                       | 0.01x               | 1.02x                 |
| Instance Field Set (typed)     | 5.40M            | 666.70M                     | 4.54M                       | 0.01x               | 1.19x                 |
| Static Field Get (typed)       | 7.56M            | 692.60M                     | 8.70M                       | 0.01x               | 0.87x                 |
| Static Field Set (typed)       | 6.25M            | 667.66M                     | 6.31M                       | 0.01x               | 0.99x                 |
| Instance Property Get (typed)  | 659.90M          | 695.46M                     | 13.51M                      | 0.95x               | 48.86x                |
| Instance Property Set (typed)  | 664.27M          | 703.34M                     | 1.42M                       | 0.94x               | 466.49x               |
| Static Property Get (typed)    | 616.40M          | 694.11M                     | 27.81M                      | 0.89x               | 22.16x                |
| Static Property Set (typed)    | 679.48M          | 664.16M                     | 2.96M                       | 1.02x               | 229.19x               |
| Instance Method Invoke (typed) | 643.48M          | 687.65M                     | 1.97M                       | 0.94x               | 326.58x               |
| Static Method Invoke (typed)   | 638.21M          | 685.44M                     | 1.93M                       | 0.93x               | 330.33x               |

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
