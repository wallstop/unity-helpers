# Reflection Performance Benchmarks

Unity Helpers replaces ad-hoc reflection with cached delegates that favour expression lambdas on IL2CPP-safe platforms and fall back to dynamic IL emit or plain reflection where available. These benchmarks compare raw `System.Reflection` against the helpers for common access patterns.

Each run updates the table for the current operating system only. Sections that still show `_No benchmark data generated yet._` simply have not been executed on that platform.

## Windows

<!-- REFLECTION_PERFORMANCE_WINDOWS_START -->

Generated on 2026-01-12 01:50:03 UTC

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
    <tr><td align="left">Instance Field Get (boxed)</td><td align="right">25.21M</td><td align="right">6.69M</td><td align="right">3.77x</td></tr>
    <tr><td align="left">Instance Field Set (boxed)</td><td align="right">22.18M</td><td align="right">5.50M</td><td align="right">4.03x</td></tr>
    <tr><td align="left">Static Field Get (boxed)</td><td align="right">16.53M</td><td align="right">2.71M</td><td align="right">6.09x</td></tr>
    <tr><td align="left">Static Field Set (boxed)</td><td align="right">18.01M</td><td align="right">4.92M</td><td align="right">3.66x</td></tr>
    <tr><td align="left">Instance Property Get (boxed)</td><td align="right">28.85M</td><td align="right">25.19M</td><td align="right">1.15x</td></tr>
    <tr><td align="left">Instance Property Set (boxed)</td><td align="right">24.78M</td><td align="right">1.49M</td><td align="right">16.61x</td></tr>
    <tr><td align="left">Static Property Get (boxed)</td><td align="right">21.82M</td><td align="right">24.49M</td><td align="right">0.89x</td></tr>
    <tr><td align="left">Static Property Set (boxed)</td><td align="right">26.46M</td><td align="right">2.54M</td><td align="right">10.42x</td></tr>
    <tr><td align="left">Instance Method Invoke (boxed)</td><td align="right">20.55M</td><td align="right">1.71M</td><td align="right">11.99x</td></tr>
    <tr><td align="left">Static Method Invoke (boxed)</td><td align="right">25.30M</td><td align="right">2.68M</td><td align="right">9.42x</td></tr>
    <tr><td align="left">Constructor Invoke (boxed)</td><td align="right">23.48M</td><td align="right">2.52M</td><td align="right">9.33x</td></tr>
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
    <tr><td align="left">Instance Field Get (typed)</td><td align="right">650.81M</td><td align="right">661.45M</td><td align="right">6.69M</td><td align="right">0.98x</td><td align="right">97.25x</td></tr>
    <tr><td align="left">Instance Field Set (typed)</td><td align="right">653.34M</td><td align="right">660.94M</td><td align="right">5.50M</td><td align="right">0.99x</td><td align="right">118.80x</td></tr>
    <tr><td align="left">Static Field Get (typed)</td><td align="right">663.07M</td><td align="right">687.03M</td><td align="right">2.71M</td><td align="right">0.97x</td><td align="right">244.39x</td></tr>
    <tr><td align="left">Static Field Set (typed)</td><td align="right">681.63M</td><td align="right">670.11M</td><td align="right">4.92M</td><td align="right">1.02x</td><td align="right">138.44x</td></tr>
    <tr><td align="left">Instance Property Get (typed)</td><td align="right">685.44M</td><td align="right">692.09M</td><td align="right">25.19M</td><td align="right">0.99x</td><td align="right">27.21x</td></tr>
    <tr><td align="left">Instance Property Set (typed)</td><td align="right">661.24M</td><td align="right">701.70M</td><td align="right">1.49M</td><td align="right">0.94x</td><td align="right">443.26x</td></tr>
    <tr><td align="left">Static Property Get (typed)</td><td align="right">652.49M</td><td align="right">685.16M</td><td align="right">24.49M</td><td align="right">0.95x</td><td align="right">26.64x</td></tr>
    <tr><td align="left">Static Property Set (typed)</td><td align="right">677.05M</td><td align="right">657.03M</td><td align="right">2.54M</td><td align="right">1.03x</td><td align="right">266.65x</td></tr>
    <tr><td align="left">Instance Method Invoke (typed)</td><td align="right">686.13M</td><td align="right">685.59M</td><td align="right">1.71M</td><td align="right">1.00x</td><td align="right">400.58x</td></tr>
    <tr><td align="left">Static Method Invoke (typed)</td><td align="right">659.66M</td><td align="right">678.18M</td><td align="right">2.68M</td><td align="right">0.97x</td><td align="right">245.75x</td></tr>
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
    <tr><td align="left">Instance Field Get (boxed)</td><td align="right">24.78M</td><td align="right">6.08M</td><td align="right">4.07x</td></tr>
    <tr><td align="left">Instance Field Set (boxed)</td><td align="right">28.58M</td><td align="right">5.48M</td><td align="right">5.21x</td></tr>
    <tr><td align="left">Static Field Get (boxed)</td><td align="right">20.95M</td><td align="right">8.51M</td><td align="right">2.46x</td></tr>
    <tr><td align="left">Static Field Set (boxed)</td><td align="right">27.73M</td><td align="right">4.57M</td><td align="right">6.06x</td></tr>
    <tr><td align="left">Instance Property Get (boxed)</td><td align="right">21.15M</td><td align="right">3.49M</td><td align="right">6.06x</td></tr>
    <tr><td align="left">Instance Property Set (boxed)</td><td align="right">12.82M</td><td align="right">2.02M</td><td align="right">6.34x</td></tr>
    <tr><td align="left">Static Property Get (boxed)</td><td align="right">22.50M</td><td align="right">20.45M</td><td align="right">1.10x</td></tr>
    <tr><td align="left">Static Property Set (boxed)</td><td align="right">20.66M</td><td align="right">2.88M</td><td align="right">7.17x</td></tr>
    <tr><td align="left">Instance Method Invoke (boxed)</td><td align="right">23.90M</td><td align="right">1.98M</td><td align="right">12.08x</td></tr>
    <tr><td align="left">Static Method Invoke (boxed)</td><td align="right">27.12M</td><td align="right">2.21M</td><td align="right">12.27x</td></tr>
    <tr><td align="left">Constructor Invoke (boxed)</td><td align="right">27.01M</td><td align="right">2.54M</td><td align="right">10.65x</td></tr>
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
    <tr><td align="left">Instance Field Get (typed)</td><td align="right">692.43M</td><td align="right">658.49M</td><td align="right">6.08M</td><td align="right">1.05x</td><td align="right">113.81x</td></tr>
    <tr><td align="left">Instance Field Set (typed)</td><td align="right">651.66M</td><td align="right">657.97M</td><td align="right">5.48M</td><td align="right">0.99x</td><td align="right">118.84x</td></tr>
    <tr><td align="left">Static Field Get (typed)</td><td align="right">647.30M</td><td align="right">683.07M</td><td align="right">8.51M</td><td align="right">0.95x</td><td align="right">76.02x</td></tr>
    <tr><td align="left">Static Field Set (typed)</td><td align="right">669.78M</td><td align="right">656.50M</td><td align="right">4.57M</td><td align="right">1.02x</td><td align="right">146.40x</td></tr>
    <tr><td align="left">Instance Property Get (typed)</td><td align="right">676.92M</td><td align="right">684.18M</td><td align="right">3.49M</td><td align="right">0.99x</td><td align="right">194.03x</td></tr>
    <tr><td align="left">Instance Property Set (typed)</td><td align="right">657.90M</td><td align="right">691.31M</td><td align="right">2.02M</td><td align="right">0.95x</td><td align="right">325.26x</td></tr>
    <tr><td align="left">Static Property Get (typed)</td><td align="right">643.98M</td><td align="right">684.71M</td><td align="right">20.45M</td><td align="right">0.94x</td><td align="right">31.49x</td></tr>
    <tr><td align="left">Static Property Set (typed)</td><td align="right">669.72M</td><td align="right">653.78M</td><td align="right">2.88M</td><td align="right">1.02x</td><td align="right">232.38x</td></tr>
    <tr><td align="left">Instance Method Invoke (typed)</td><td align="right">680.33M</td><td align="right">680.42M</td><td align="right">1.98M</td><td align="right">1.00x</td><td align="right">343.95x</td></tr>
    <tr><td align="left">Static Method Invoke (typed)</td><td align="right">653.83M</td><td align="right">674.42M</td><td align="right">2.21M</td><td align="right">0.97x</td><td align="right">295.69x</td></tr>
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
    <tr><td align="left">Instance Field Get (boxed)</td><td align="right">27.70M</td><td align="right">5.80M</td><td align="right">4.77x</td></tr>
    <tr><td align="left">Instance Field Set (boxed)</td><td align="right">28.24M</td><td align="right">5.45M</td><td align="right">5.18x</td></tr>
    <tr><td align="left">Static Field Get (boxed)</td><td align="right">21.57M</td><td align="right">8.41M</td><td align="right">2.56x</td></tr>
    <tr><td align="left">Static Field Set (boxed)</td><td align="right">25.65M</td><td align="right">6.16M</td><td align="right">4.17x</td></tr>
    <tr><td align="left">Instance Property Get (boxed)</td><td align="right">18.83M</td><td align="right">22.95M</td><td align="right">0.82x</td></tr>
    <tr><td align="left">Instance Property Set (boxed)</td><td align="right">25.31M</td><td align="right">1.98M</td><td align="right">12.80x</td></tr>
    <tr><td align="left">Static Property Get (boxed)</td><td align="right">23.51M</td><td align="right">8.69M</td><td align="right">2.71x</td></tr>
    <tr><td align="left">Static Property Set (boxed)</td><td align="right">2.06M</td><td align="right">2.86M</td><td align="right">0.72x</td></tr>
    <tr><td align="left">Instance Method Invoke (boxed)</td><td align="right">23.09M</td><td align="right">2.00M</td><td align="right">11.56x</td></tr>
    <tr><td align="left">Static Method Invoke (boxed)</td><td align="right">27.05M</td><td align="right">2.12M</td><td align="right">12.75x</td></tr>
    <tr><td align="left">Constructor Invoke (boxed)</td><td align="right">26.10M</td><td align="right">2.52M</td><td align="right">10.36x</td></tr>
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
    <tr><td align="left">Instance Field Get (typed)</td><td align="right">662.00M</td><td align="right">685.29M</td><td align="right">5.80M</td><td align="right">0.97x</td><td align="right">114.09x</td></tr>
    <tr><td align="left">Instance Field Set (typed)</td><td align="right">654.77M</td><td align="right">661.50M</td><td align="right">5.45M</td><td align="right">0.99x</td><td align="right">120.17x</td></tr>
    <tr><td align="left">Static Field Get (typed)</td><td align="right">657.70M</td><td align="right">683.29M</td><td align="right">8.41M</td><td align="right">0.96x</td><td align="right">78.17x</td></tr>
    <tr><td align="left">Static Field Set (typed)</td><td align="right">675.09M</td><td align="right">659.39M</td><td align="right">6.16M</td><td align="right">1.02x</td><td align="right">109.61x</td></tr>
    <tr><td align="left">Instance Property Get (typed)</td><td align="right">673.39M</td><td align="right">690.30M</td><td align="right">22.95M</td><td align="right">0.98x</td><td align="right">29.34x</td></tr>
    <tr><td align="left">Instance Property Set (typed)</td><td align="right">652.07M</td><td align="right">693.53M</td><td align="right">1.98M</td><td align="right">0.94x</td><td align="right">329.72x</td></tr>
    <tr><td align="left">Static Property Get (typed)</td><td align="right">648.59M</td><td align="right">682.82M</td><td align="right">8.69M</td><td align="right">0.95x</td><td align="right">74.63x</td></tr>
    <tr><td align="left">Static Property Set (typed)</td><td align="right">671.01M</td><td align="right">654.32M</td><td align="right">2.86M</td><td align="right">1.03x</td><td align="right">234.23x</td></tr>
    <tr><td align="left">Instance Method Invoke (typed)</td><td align="right">690.97M</td><td align="right">686.98M</td><td align="right">2.00M</td><td align="right">1.01x</td><td align="right">345.91x</td></tr>
    <tr><td align="left">Static Method Invoke (typed)</td><td align="right">660.09M</td><td align="right">679.79M</td><td align="right">2.12M</td><td align="right">0.97x</td><td align="right">311.08x</td></tr>
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
    <tr><td align="left">Instance Field Get (boxed)</td><td align="right">7.48M</td><td align="right">5.76M</td><td align="right">1.30x</td></tr>
    <tr><td align="left">Instance Field Set (boxed)</td><td align="right">5.57M</td><td align="right">5.47M</td><td align="right">1.02x</td></tr>
    <tr><td align="left">Static Field Get (boxed)</td><td align="right">7.71M</td><td align="right">7.19M</td><td align="right">1.07x</td></tr>
    <tr><td align="left">Static Field Set (boxed)</td><td align="right">6.01M</td><td align="right">6.13M</td><td align="right">0.98x</td></tr>
    <tr><td align="left">Instance Property Get (boxed)</td><td align="right">21.09M</td><td align="right">18.38M</td><td align="right">1.15x</td></tr>
    <tr><td align="left">Instance Property Set (boxed)</td><td align="right">2.09M</td><td align="right">2.06M</td><td align="right">1.01x</td></tr>
    <tr><td align="left">Static Property Get (boxed)</td><td align="right">20.07M</td><td align="right">23.51M</td><td align="right">0.85x</td></tr>
    <tr><td align="left">Static Property Set (boxed)</td><td align="right">2.88M</td><td align="right">2.02M</td><td align="right">1.42x</td></tr>
    <tr><td align="left">Instance Method Invoke (boxed)</td><td align="right">1.99M</td><td align="right">1.98M</td><td align="right">1.01x</td></tr>
    <tr><td align="left">Static Method Invoke (boxed)</td><td align="right">2.69M</td><td align="right">2.72M</td><td align="right">0.99x</td></tr>
    <tr><td align="left">Constructor Invoke (boxed)</td><td align="right">2.55M</td><td align="right">2.50M</td><td align="right">1.02x</td></tr>
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
    <tr><td align="left">Instance Field Get (typed)</td><td align="right">5.24M</td><td align="right">662.95M</td><td align="right">5.76M</td><td align="right">0.01x</td><td align="right">0.91x</td></tr>
    <tr><td align="left">Instance Field Set (typed)</td><td align="right">5.46M</td><td align="right">668.23M</td><td align="right">5.47M</td><td align="right">0.01x</td><td align="right">1.00x</td></tr>
    <tr><td align="left">Static Field Get (typed)</td><td align="right">8.64M</td><td align="right">683.37M</td><td align="right">7.19M</td><td align="right">0.01x</td><td align="right">1.20x</td></tr>
    <tr><td align="left">Static Field Set (typed)</td><td align="right">4.77M</td><td align="right">661.19M</td><td align="right">6.13M</td><td align="right">0.01x</td><td align="right">0.78x</td></tr>
    <tr><td align="left">Instance Property Get (typed)</td><td align="right">675.25M</td><td align="right">683.53M</td><td align="right">18.38M</td><td align="right">0.99x</td><td align="right">36.75x</td></tr>
    <tr><td align="left">Instance Property Set (typed)</td><td align="right">655.22M</td><td align="right">704.28M</td><td align="right">2.06M</td><td align="right">0.93x</td><td align="right">317.56x</td></tr>
    <tr><td align="left">Static Property Get (typed)</td><td align="right">652.21M</td><td align="right">691.83M</td><td align="right">23.51M</td><td align="right">0.94x</td><td align="right">27.75x</td></tr>
    <tr><td align="left">Static Property Set (typed)</td><td align="right">678.92M</td><td align="right">659.94M</td><td align="right">2.02M</td><td align="right">1.03x</td><td align="right">335.95x</td></tr>
    <tr><td align="left">Instance Method Invoke (typed)</td><td align="right">692.36M</td><td align="right">685.00M</td><td align="right">1.98M</td><td align="right">1.01x</td><td align="right">350.50x</td></tr>
    <tr><td align="left">Static Method Invoke (typed)</td><td align="right">661.40M</td><td align="right">678.90M</td><td align="right">2.72M</td><td align="right">0.97x</td><td align="right">242.98x</td></tr>
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
