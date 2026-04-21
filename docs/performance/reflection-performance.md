# Reflection Performance Benchmarks

Unity Helpers replaces ad-hoc reflection with cached delegates that favour expression lambdas on IL2CPP-safe platforms and fall back to dynamic IL emit or plain reflection where available. These benchmarks compare raw `System.Reflection` against the helpers for common access patterns.

Each run updates the table for the current operating system only. Sections that still show `_No benchmark data generated yet._` simply have not been executed on that platform.

## Windows

<!-- REFLECTION_PERFORMANCE_WINDOWS_START -->

Generated on 2026-04-21 03:18:51 UTC

### Strategy: Default (auto)

#### Boxed Access (object)

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Scenario</th>
      <th align="right">Helper (ops/sec)</th>
      <th align="right">System.Reflection (ops/sec)</th>
      <th align="right">Speedup vs Reflection</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Instance Field Get (boxed)</td><td align="right">19.47M</td><td align="right">7.32M</td><td align="right">2.66x</td></tr>
    <tr><td align="left">Instance Field Set (boxed)</td><td align="right">22.97M</td><td align="right">5.36M</td><td align="right">4.29x</td></tr>
    <tr><td align="left">Static Field Get (boxed)</td><td align="right">21.28M</td><td align="right">8.60M</td><td align="right">2.47x</td></tr>
    <tr><td align="left">Static Field Set (boxed)</td><td align="right">22.91M</td><td align="right">6.17M</td><td align="right">3.71x</td></tr>
    <tr><td align="left">Instance Property Get (boxed)</td><td align="right">22.97M</td><td align="right">22.71M</td><td align="right">1.01x</td></tr>
    <tr><td align="left">Instance Property Set (boxed)</td><td align="right">21.46M</td><td align="right">2.05M</td><td align="right">10.46x</td></tr>
    <tr><td align="left">Static Property Get (boxed)</td><td align="right">18.58M</td><td align="right">21.19M</td><td align="right">0.88x</td></tr>
    <tr><td align="left">Static Property Set (boxed)</td><td align="right">23.44M</td><td align="right">2.90M</td><td align="right">8.08x</td></tr>
    <tr><td align="left">Instance Method Invoke (boxed)</td><td align="right">17.65M</td><td align="right">1.95M</td><td align="right">9.05x</td></tr>
    <tr><td align="left">Static Method Invoke (boxed)</td><td align="right">23.10M</td><td align="right">2.73M</td><td align="right">8.46x</td></tr>
    <tr><td align="left">Constructor Invoke (boxed)</td><td align="right">22.93M</td><td align="right">1.67M</td><td align="right">13.76x</td></tr>
  </tbody>
</table>

#### Typed Access (no boxing)

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Scenario</th>
      <th align="right">Helper (ops/sec)</th>
      <th align="right">Baseline Delegate (ops/sec)</th>
      <th align="right">System.Reflection (ops/sec)</th>
      <th align="right">Speedup vs Delegate</th>
      <th align="right">Speedup vs Reflection</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Instance Field Get (typed)</td><td align="right">612.73M</td><td align="right">660.71M</td><td align="right">7.32M</td><td align="right">0.93x</td><td align="right">83.65x</td></tr>
    <tr><td align="left">Instance Field Set (typed)</td><td align="right">630.08M</td><td align="right">664.60M</td><td align="right">5.36M</td><td align="right">0.95x</td><td align="right">117.65x</td></tr>
    <tr><td align="left">Static Field Get (typed)</td><td align="right">625.63M</td><td align="right">691.62M</td><td align="right">8.60M</td><td align="right">0.90x</td><td align="right">72.76x</td></tr>
    <tr><td align="left">Static Field Set (typed)</td><td align="right">636.30M</td><td align="right">667.62M</td><td align="right">6.17M</td><td align="right">0.95x</td><td align="right">103.16x</td></tr>
    <tr><td align="left">Instance Property Get (typed)</td><td align="right">577.76M</td><td align="right">695.19M</td><td align="right">22.71M</td><td align="right">0.83x</td><td align="right">25.44x</td></tr>
    <tr><td align="left">Instance Property Set (typed)</td><td align="right">638.69M</td><td align="right">702.42M</td><td align="right">2.05M</td><td align="right">0.91x</td><td align="right">311.50x</td></tr>
    <tr><td align="left">Static Property Get (typed)</td><td align="right">619.33M</td><td align="right">691.17M</td><td align="right">21.19M</td><td align="right">0.90x</td><td align="right">29.23x</td></tr>
    <tr><td align="left">Static Property Set (typed)</td><td align="right">616.60M</td><td align="right">663.40M</td><td align="right">2.90M</td><td align="right">0.93x</td><td align="right">212.58x</td></tr>
    <tr><td align="left">Instance Method Invoke (typed)</td><td align="right">631.97M</td><td align="right">687.16M</td><td align="right">1.95M</td><td align="right">0.92x</td><td align="right">323.96x</td></tr>
    <tr><td align="left">Static Method Invoke (typed)</td><td align="right">599.89M</td><td align="right">673.32M</td><td align="right">2.73M</td><td align="right">0.89x</td><td align="right">219.80x</td></tr>
  </tbody>
</table>

### Strategy: Expressions

#### Boxed Access (object)

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Scenario</th>
      <th align="right">Helper (ops/sec)</th>
      <th align="right">System.Reflection (ops/sec)</th>
      <th align="right">Speedup vs Reflection</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Instance Field Get (boxed)</td><td align="right">2.93M</td><td align="right">3.44M</td><td align="right">0.85x</td></tr>
    <tr><td align="left">Instance Field Set (boxed)</td><td align="right">23.08M</td><td align="right">5.34M</td><td align="right">4.32x</td></tr>
    <tr><td align="left">Static Field Get (boxed)</td><td align="right">21.30M</td><td align="right">8.73M</td><td align="right">2.44x</td></tr>
    <tr><td align="left">Static Field Set (boxed)</td><td align="right">22.56M</td><td align="right">6.19M</td><td align="right">3.65x</td></tr>
    <tr><td align="left">Instance Property Get (boxed)</td><td align="right">22.98M</td><td align="right">20.58M</td><td align="right">1.12x</td></tr>
    <tr><td align="left">Instance Property Set (boxed)</td><td align="right">21.59M</td><td align="right">2.07M</td><td align="right">10.41x</td></tr>
    <tr><td align="left">Static Property Get (boxed)</td><td align="right">18.91M</td><td align="right">23.05M</td><td align="right">0.82x</td></tr>
    <tr><td align="left">Static Property Set (boxed)</td><td align="right">24.11M</td><td align="right">2.94M</td><td align="right">8.21x</td></tr>
    <tr><td align="left">Instance Method Invoke (boxed)</td><td align="right">17.94M</td><td align="right">1.97M</td><td align="right">9.12x</td></tr>
    <tr><td align="left">Static Method Invoke (boxed)</td><td align="right">23.96M</td><td align="right">2.69M</td><td align="right">8.89x</td></tr>
    <tr><td align="left">Constructor Invoke (boxed)</td><td align="right">23.29M</td><td align="right">2.60M</td><td align="right">8.94x</td></tr>
  </tbody>
</table>

#### Typed Access (no boxing)

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Scenario</th>
      <th align="right">Helper (ops/sec)</th>
      <th align="right">Baseline Delegate (ops/sec)</th>
      <th align="right">System.Reflection (ops/sec)</th>
      <th align="right">Speedup vs Delegate</th>
      <th align="right">Speedup vs Reflection</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Instance Field Get (typed)</td><td align="right">633.60M</td><td align="right">673.55M</td><td align="right">3.44M</td><td align="right">0.94x</td><td align="right">184.07x</td></tr>
    <tr><td align="left">Instance Field Set (typed)</td><td align="right">639.86M</td><td align="right">666.50M</td><td align="right">5.34M</td><td align="right">0.96x</td><td align="right">119.77x</td></tr>
    <tr><td align="left">Static Field Get (typed)</td><td align="right">624.14M</td><td align="right">691.82M</td><td align="right">8.73M</td><td align="right">0.90x</td><td align="right">71.50x</td></tr>
    <tr><td align="left">Static Field Set (typed)</td><td align="right">620.62M</td><td align="right">667.19M</td><td align="right">6.19M</td><td align="right">0.93x</td><td align="right">100.32x</td></tr>
    <tr><td align="left">Instance Property Get (typed)</td><td align="right">575.01M</td><td align="right">697.98M</td><td align="right">20.58M</td><td align="right">0.82x</td><td align="right">27.95x</td></tr>
    <tr><td align="left">Instance Property Set (typed)</td><td align="right">637.80M</td><td align="right">700.31M</td><td align="right">2.07M</td><td align="right">0.91x</td><td align="right">307.44x</td></tr>
    <tr><td align="left">Static Property Get (typed)</td><td align="right">617.42M</td><td align="right">692.17M</td><td align="right">23.05M</td><td align="right">0.89x</td><td align="right">26.78x</td></tr>
    <tr><td align="left">Static Property Set (typed)</td><td align="right">620.64M</td><td align="right">662.68M</td><td align="right">2.94M</td><td align="right">0.94x</td><td align="right">211.39x</td></tr>
    <tr><td align="left">Instance Method Invoke (typed)</td><td align="right">632.39M</td><td align="right">691.15M</td><td align="right">1.97M</td><td align="right">0.91x</td><td align="right">321.63x</td></tr>
    <tr><td align="left">Static Method Invoke (typed)</td><td align="right">615.24M</td><td align="right">685.00M</td><td align="right">2.69M</td><td align="right">0.90x</td><td align="right">228.36x</td></tr>
  </tbody>
</table>

### Strategy: Dynamic IL

#### Boxed Access (object)

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Scenario</th>
      <th align="right">Helper (ops/sec)</th>
      <th align="right">System.Reflection (ops/sec)</th>
      <th align="right">Speedup vs Reflection</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Instance Field Get (boxed)</td><td align="right">23.15M</td><td align="right">7.39M</td><td align="right">3.13x</td></tr>
    <tr><td align="left">Instance Field Set (boxed)</td><td align="right">24.84M</td><td align="right">5.47M</td><td align="right">4.54x</td></tr>
    <tr><td align="left">Static Field Get (boxed)</td><td align="right">22.17M</td><td align="right">8.73M</td><td align="right">2.54x</td></tr>
    <tr><td align="left">Static Field Set (boxed)</td><td align="right">24.27M</td><td align="right">5.26M</td><td align="right">4.61x</td></tr>
    <tr><td align="left">Instance Property Get (boxed)</td><td align="right">25.16M</td><td align="right">23.28M</td><td align="right">1.08x</td></tr>
    <tr><td align="left">Instance Property Set (boxed)</td><td align="right">22.48M</td><td align="right">1.85M</td><td align="right">12.12x</td></tr>
    <tr><td align="left">Static Property Get (boxed)</td><td align="right">3.80M</td><td align="right">2.18M</td><td align="right">1.74x</td></tr>
    <tr><td align="left">Static Property Set (boxed)</td><td align="right">24.76M</td><td align="right">2.94M</td><td align="right">8.43x</td></tr>
    <tr><td align="left">Instance Method Invoke (boxed)</td><td align="right">19.77M</td><td align="right">1.98M</td><td align="right">9.98x</td></tr>
    <tr><td align="left">Static Method Invoke (boxed)</td><td align="right">23.00M</td><td align="right">2.68M</td><td align="right">8.59x</td></tr>
    <tr><td align="left">Constructor Invoke (boxed)</td><td align="right">22.50M</td><td align="right">2.61M</td><td align="right">8.63x</td></tr>
  </tbody>
</table>

#### Typed Access (no boxing)

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Scenario</th>
      <th align="right">Helper (ops/sec)</th>
      <th align="right">Baseline Delegate (ops/sec)</th>
      <th align="right">System.Reflection (ops/sec)</th>
      <th align="right">Speedup vs Delegate</th>
      <th align="right">Speedup vs Reflection</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Instance Field Get (typed)</td><td align="right">614.97M</td><td align="right">674.10M</td><td align="right">7.39M</td><td align="right">0.91x</td><td align="right">83.27x</td></tr>
    <tr><td align="left">Instance Field Set (typed)</td><td align="right">639.26M</td><td align="right">664.73M</td><td align="right">5.47M</td><td align="right">0.96x</td><td align="right">116.85x</td></tr>
    <tr><td align="left">Static Field Get (typed)</td><td align="right">617.13M</td><td align="right">692.64M</td><td align="right">8.73M</td><td align="right">0.89x</td><td align="right">70.67x</td></tr>
    <tr><td align="left">Static Field Set (typed)</td><td align="right">635.79M</td><td align="right">667.62M</td><td align="right">5.26M</td><td align="right">0.95x</td><td align="right">120.86x</td></tr>
    <tr><td align="left">Instance Property Get (typed)</td><td align="right">587.73M</td><td align="right">695.70M</td><td align="right">23.28M</td><td align="right">0.84x</td><td align="right">25.25x</td></tr>
    <tr><td align="left">Instance Property Set (typed)</td><td align="right">638.85M</td><td align="right">702.14M</td><td align="right">1.85M</td><td align="right">0.91x</td><td align="right">344.41x</td></tr>
    <tr><td align="left">Static Property Get (typed)</td><td align="right">618.98M</td><td align="right">692.84M</td><td align="right">2.18M</td><td align="right">0.89x</td><td align="right">284.05x</td></tr>
    <tr><td align="left">Static Property Set (typed)</td><td align="right">615.38M</td><td align="right">657.24M</td><td align="right">2.94M</td><td align="right">0.94x</td><td align="right">209.61x</td></tr>
    <tr><td align="left">Instance Method Invoke (typed)</td><td align="right">621.08M</td><td align="right">679.85M</td><td align="right">1.98M</td><td align="right">0.91x</td><td align="right">313.60x</td></tr>
    <tr><td align="left">Static Method Invoke (typed)</td><td align="right">608.12M</td><td align="right">683.73M</td><td align="right">2.68M</td><td align="right">0.89x</td><td align="right">227.17x</td></tr>
  </tbody>
</table>

### Strategy: Reflection Fallback

#### Boxed Access (object)

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Scenario</th>
      <th align="right">Helper (ops/sec)</th>
      <th align="right">System.Reflection (ops/sec)</th>
      <th align="right">Speedup vs Reflection</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Instance Field Get (boxed)</td><td align="right">7.01M</td><td align="right">7.40M</td><td align="right">0.95x</td></tr>
    <tr><td align="left">Instance Field Set (boxed)</td><td align="right">5.52M</td><td align="right">3.73M</td><td align="right">1.48x</td></tr>
    <tr><td align="left">Static Field Get (boxed)</td><td align="right">8.18M</td><td align="right">8.73M</td><td align="right">0.94x</td></tr>
    <tr><td align="left">Static Field Set (boxed)</td><td align="right">5.92M</td><td align="right">4.33M</td><td align="right">1.37x</td></tr>
    <tr><td align="left">Instance Property Get (boxed)</td><td align="right">21.09M</td><td align="right">21.88M</td><td align="right">0.96x</td></tr>
    <tr><td align="left">Instance Property Set (boxed)</td><td align="right">2.05M</td><td align="right">2.10M</td><td align="right">0.98x</td></tr>
    <tr><td align="left">Static Property Get (boxed)</td><td align="right">16.46M</td><td align="right">22.90M</td><td align="right">0.72x</td></tr>
    <tr><td align="left">Static Property Set (boxed)</td><td align="right">2.93M</td><td align="right">2.99M</td><td align="right">0.98x</td></tr>
    <tr><td align="left">Instance Method Invoke (boxed)</td><td align="right">2.01M</td><td align="right">1.99M</td><td align="right">1.01x</td></tr>
    <tr><td align="left">Static Method Invoke (boxed)</td><td align="right">2.69M</td><td align="right">2.73M</td><td align="right">0.99x</td></tr>
    <tr><td align="left">Constructor Invoke (boxed)</td><td align="right">2.23M</td><td align="right">1.62M</td><td align="right">1.38x</td></tr>
  </tbody>
</table>

#### Typed Access (no boxing)

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Scenario</th>
      <th align="right">Helper (ops/sec)</th>
      <th align="right">Baseline Delegate (ops/sec)</th>
      <th align="right">System.Reflection (ops/sec)</th>
      <th align="right">Speedup vs Delegate</th>
      <th align="right">Speedup vs Reflection</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Instance Field Get (typed)</td><td align="right">6.77M</td><td align="right">669.47M</td><td align="right">7.40M</td><td align="right">0.01x</td><td align="right">0.91x</td></tr>
    <tr><td align="left">Instance Field Set (typed)</td><td align="right">5.52M</td><td align="right">659.03M</td><td align="right">3.73M</td><td align="right">0.01x</td><td align="right">1.48x</td></tr>
    <tr><td align="left">Static Field Get (typed)</td><td align="right">8.03M</td><td align="right">686.98M</td><td align="right">8.73M</td><td align="right">0.01x</td><td align="right">0.92x</td></tr>
    <tr><td align="left">Static Field Set (typed)</td><td align="right">3.90M</td><td align="right">669.93M</td><td align="right">4.33M</td><td align="right">0.01x</td><td align="right">0.90x</td></tr>
    <tr><td align="left">Instance Property Get (typed)</td><td align="right">576.71M</td><td align="right">695.18M</td><td align="right">21.88M</td><td align="right">0.83x</td><td align="right">26.36x</td></tr>
    <tr><td align="left">Instance Property Set (typed)</td><td align="right">638.85M</td><td align="right">701.32M</td><td align="right">2.10M</td><td align="right">0.91x</td><td align="right">303.61x</td></tr>
    <tr><td align="left">Static Property Get (typed)</td><td align="right">609.05M</td><td align="right">680.50M</td><td align="right">22.90M</td><td align="right">0.90x</td><td align="right">26.59x</td></tr>
    <tr><td align="left">Static Property Set (typed)</td><td align="right">609.89M</td><td align="right">652.01M</td><td align="right">2.99M</td><td align="right">0.94x</td><td align="right">203.85x</td></tr>
    <tr><td align="left">Instance Method Invoke (typed)</td><td align="right">630.49M</td><td align="right">687.37M</td><td align="right">1.99M</td><td align="right">0.92x</td><td align="right">317.05x</td></tr>
    <tr><td align="left">Static Method Invoke (typed)</td><td align="right">611.20M</td><td align="right">683.09M</td><td align="right">2.73M</td><td align="right">0.89x</td><td align="right">224.12x</td></tr>
  </tbody>
</table>

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
