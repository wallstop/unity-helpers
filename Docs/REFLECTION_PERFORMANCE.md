# Reflection Performance Benchmarks

Unity Helpers replaces ad-hoc reflection with cached delegates that favour expression lambdas on IL2CPP-safe platforms and fall back to dynamic IL emit or plain reflection where available. These benchmarks compare raw `System.Reflection` against the helpers for common access patterns.

Each run updates the table for the current operating system only. Sections that still show `_No benchmark data generated yet._` simply have not been executed on that platform.

## Windows

<!-- REFLECTION_PERFORMANCE_WINDOWS_START -->

Generated on 2025-10-29 02:15:12 UTC

### Strategy: Default (auto)

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 24.22M           | 6.75M                       | 3.59x                 |
| Instance Field Set (boxed)     | 4.26M            | 3.17M                       | 1.34x                 |
| Static Field Get (boxed)       | 29.54M           | 7.10M                       | 4.16x                 |
| Static Field Set (boxed)       | 28.69M           | 5.27M                       | 5.44x                 |
| Instance Property Get (boxed)  | 29.97M           | 23.26M                      | 1.29x                 |
| Instance Property Set (boxed)  | 29.79M           | 2.05M                       | 14.51x                |
| Static Property Get (boxed)    | 21.08M           | 19.44M                      | 1.08x                 |
| Static Property Set (boxed)    | 28.53M           | 2.89M                       | 9.88x                 |
| Instance Method Invoke (boxed) | 19.14M           | 2.01M                       | 9.53x                 |
| Static Method Invoke (boxed)   | 26.36M           | 2.73M                       | 9.64x                 |
| Constructor Invoke (boxed)     | 21.37M           | 2.58M                       | 8.27x                 |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 609.33M          | 663.84M                     | 6.75M                       | 0.92x               | 90.30x                |
| Instance Field Set (typed)     | 654.50M          | 666.66M                     | 3.17M                       | 0.98x               | 206.25x               |
| Static Field Get (typed)       | 626.14M          | 691.60M                     | 7.10M                       | 0.91x               | 88.18x                |
| Static Field Set (typed)       | 677.38M          | 662.69M                     | 5.27M                       | 1.02x               | 128.47x               |
| Instance Property Get (typed)  | 607.97M          | 696.04M                     | 23.26M                      | 0.87x               | 26.14x                |
| Instance Property Set (typed)  | 662.57M          | 702.85M                     | 2.05M                       | 0.94x               | 322.79x               |
| Static Property Get (typed)    | 623.08M          | 693.81M                     | 19.44M                      | 0.90x               | 32.06x                |
| Static Property Set (typed)    | 673.23M          | 662.22M                     | 2.89M                       | 1.02x               | 233.04x               |
| Instance Method Invoke (typed) | 636.35M          | 687.26M                     | 2.01M                       | 0.93x               | 317.07x               |
| Static Method Invoke (typed)   | 640.43M          | 686.08M                     | 2.73M                       | 0.93x               | 234.25x               |

### Strategy: Expressions

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 29.26M           | 5.95M                       | 4.92x                 |
| Instance Field Set (boxed)     | 19.09M           | 2.52M                       | 7.57x                 |
| Static Field Get (boxed)       | 19.03M           | 8.65M                       | 2.20x                 |
| Static Field Set (boxed)       | 27.58M           | 5.27M                       | 5.23x                 |
| Instance Property Get (boxed)  | 29.12M           | 21.43M                      | 1.36x                 |
| Instance Property Set (boxed)  | 29.63M           | 2.07M                       | 14.34x                |
| Static Property Get (boxed)    | 18.66M           | 26.67M                      | 0.70x                 |
| Static Property Set (boxed)    | 27.99M           | 2.52M                       | 11.10x                |
| Instance Method Invoke (boxed) | 23.88M           | 2.01M                       | 11.87x                |
| Static Method Invoke (boxed)   | 23.18M           | 2.72M                       | 8.51x                 |
| Constructor Invoke (boxed)     | 27.85M           | 2.59M                       | 10.74x                |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 602.71M          | 663.82M                     | 5.95M                       | 0.91x               | 101.35x               |
| Instance Field Set (typed)     | 654.81M          | 667.05M                     | 2.52M                       | 0.98x               | 259.54x               |
| Static Field Get (typed)       | 624.81M          | 694.44M                     | 8.65M                       | 0.90x               | 72.25x                |
| Static Field Set (typed)       | 678.29M          | 664.36M                     | 5.27M                       | 1.02x               | 128.59x               |
| Instance Property Get (typed)  | 629.51M          | 694.83M                     | 21.43M                      | 0.91x               | 29.37x                |
| Instance Property Set (typed)  | 664.27M          | 703.58M                     | 2.07M                       | 0.94x               | 321.54x               |
| Static Property Get (typed)    | 628.81M          | 695.41M                     | 26.67M                      | 0.90x               | 23.57x                |
| Static Property Set (typed)    | 672.38M          | 663.33M                     | 2.52M                       | 1.01x               | 266.74x               |
| Instance Method Invoke (typed) | 641.14M          | 691.34M                     | 2.01M                       | 0.93x               | 318.75x               |
| Static Method Invoke (typed)   | 619.39M          | 686.15M                     | 2.72M                       | 0.90x               | 227.32x               |

### Strategy: Dynamic IL

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 22.40M           | 7.36M                       | 3.04x                 |
| Instance Field Set (boxed)     | 27.29M           | 2.59M                       | 10.53x                |
| Static Field Get (boxed)       | 12.02M           | 8.53M                       | 1.41x                 |
| Static Field Set (boxed)       | 24.01M           | 6.27M                       | 3.83x                 |
| Instance Property Get (boxed)  | 22.88M           | 26.96M                      | 0.85x                 |
| Instance Property Set (boxed)  | 27.91M           | 1.83M                       | 15.29x                |
| Static Property Get (boxed)    | 25.43M           | 26.03M                      | 0.98x                 |
| Static Property Set (boxed)    | 26.34M           | 2.93M                       | 8.98x                 |
| Instance Method Invoke (boxed) | 22.25M           | 1.75M                       | 12.72x                |
| Static Method Invoke (boxed)   | 28.37M           | 2.73M                       | 10.39x                |
| Constructor Invoke (boxed)     | 21.35M           | 2.57M                       | 8.29x                 |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 602.54M          | 662.96M                     | 7.36M                       | 0.91x               | 81.90x                |
| Instance Field Set (typed)     | 660.25M          | 666.56M                     | 2.59M                       | 0.99x               | 254.74x               |
| Static Field Get (typed)       | 621.57M          | 692.27M                     | 8.53M                       | 0.90x               | 72.87x                |
| Static Field Set (typed)       | 681.22M          | 664.62M                     | 6.27M                       | 1.02x               | 108.57x               |
| Instance Property Get (typed)  | 620.91M          | 694.29M                     | 26.96M                      | 0.89x               | 23.03x                |
| Instance Property Set (typed)  | 662.36M          | 704.33M                     | 1.83M                       | 0.94x               | 362.87x               |
| Static Property Get (typed)    | 628.52M          | 692.68M                     | 26.03M                      | 0.91x               | 24.15x                |
| Static Property Set (typed)    | 674.39M          | 663.08M                     | 2.93M                       | 1.02x               | 229.78x               |
| Instance Method Invoke (typed) | 643.06M          | 687.17M                     | 1.75M                       | 0.94x               | 367.65x               |
| Static Method Invoke (typed)   | 629.08M          | 686.20M                     | 2.73M                       | 0.92x               | 230.39x               |

### Strategy: Reflection Fallback

#### Boxed Access (object)

| Scenario                       | Helper (ops/sec) | System.Reflection (ops/sec) | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------- |
| Instance Field Get (boxed)     | 7.51M            | 7.36M                       | 1.02x                 |
| Instance Field Set (boxed)     | 4.45M            | 5.54M                       | 0.80x                 |
| Static Field Get (boxed)       | 8.81M            | 7.04M                       | 1.25x                 |
| Static Field Set (boxed)       | 6.19M            | 5.25M                       | 1.18x                 |
| Instance Property Get (boxed)  | 19.85M           | 3.37M                       | 5.89x                 |
| Instance Property Set (boxed)  | 2.07M            | 2.07M                       | 1.00x                 |
| Static Property Get (boxed)    | 22.51M           | 23.24M                      | 0.97x                 |
| Static Property Set (boxed)    | 2.89M            | 2.94M                       | 0.98x                 |
| Instance Method Invoke (boxed) | 1.99M            | 1.96M                       | 1.02x                 |
| Static Method Invoke (boxed)   | 1.88M            | 2.74M                       | 0.69x                 |
| Constructor Invoke (boxed)     | 2.60M            | 2.59M                       | 1.00x                 |

#### Typed Access (no boxing)

| Scenario                       | Helper (ops/sec) | Baseline Delegate (ops/sec) | System.Reflection (ops/sec) | Speedup vs Delegate | Speedup vs Reflection |
| ------------------------------ | ---------------- | --------------------------- | --------------------------- | ------------------- | --------------------- |
| Instance Field Get (typed)     | 7.50M            | 656.02M                     | 7.36M                       | 0.01x               | 1.02x                 |
| Instance Field Set (typed)     | 4.54M            | 666.62M                     | 5.54M                       | 0.01x               | 0.82x                 |
| Static Field Get (typed)       | 8.84M            | 692.95M                     | 7.04M                       | 0.01x               | 1.25x                 |
| Static Field Set (typed)       | 6.22M            | 665.51M                     | 5.25M                       | 0.01x               | 1.19x                 |
| Instance Property Get (typed)  | 638.50M          | 694.96M                     | 3.37M                       | 0.92x               | 189.58x               |
| Instance Property Set (typed)  | 664.45M          | 703.00M                     | 2.07M                       | 0.95x               | 321.34x               |
| Static Property Get (typed)    | 628.75M          | 694.10M                     | 23.24M                      | 0.91x               | 27.06x                |
| Static Property Set (typed)    | 676.65M          | 663.62M                     | 2.94M                       | 1.02x               | 230.50x               |
| Instance Method Invoke (typed) | 637.70M          | 687.64M                     | 1.96M                       | 0.93x               | 326.04x               |
| Static Method Invoke (typed)   | 614.11M          | 686.05M                     | 2.74M                       | 0.90x               | 224.07x               |

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
