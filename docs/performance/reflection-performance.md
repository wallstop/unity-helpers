# Reflection Performance Benchmarks

Unity Helpers replaces ad-hoc reflection with cached delegates that favour expression lambdas on IL2CPP-safe platforms and fall back to dynamic IL emit or plain reflection where available. These benchmarks compare raw `System.Reflection` against the helpers for common access patterns.

Each run updates the table for the current operating system only. Sections that still show `_No benchmark data generated yet._` simply have not been executed on that platform.

## Windows

<!-- REFLECTION_PERFORMANCE_WINDOWS_START -->

Generated on 2026-04-22 22:11:32 UTC

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
    <tr><td align="left">Instance Field Get (boxed)</td><td align="right">22.62M</td><td align="right">7.27M</td><td align="right">3.11x</td></tr>
    <tr><td align="left">Instance Field Set (boxed)</td><td align="right">21.97M</td><td align="right">5.47M</td><td align="right">4.02x</td></tr>
    <tr><td align="left">Static Field Get (boxed)</td><td align="right">21.09M</td><td align="right">8.59M</td><td align="right">2.45x</td></tr>
    <tr><td align="left">Static Field Set (boxed)</td><td align="right">22.83M</td><td align="right">6.14M</td><td align="right">3.72x</td></tr>
    <tr><td align="left">Instance Property Get (boxed)</td><td align="right">23.39M</td><td align="right">21.74M</td><td align="right">1.08x</td></tr>
    <tr><td align="left">Instance Property Set (boxed)</td><td align="right">21.72M</td><td align="right">2.06M</td><td align="right">10.54x</td></tr>
    <tr><td align="left">Static Property Get (boxed)</td><td align="right">20.11M</td><td align="right">21.72M</td><td align="right">0.93x</td></tr>
    <tr><td align="left">Static Property Set (boxed)</td><td align="right">23.22M</td><td align="right">2.89M</td><td align="right">8.03x</td></tr>
    <tr><td align="left">Instance Method Invoke (boxed)</td><td align="right">15.03M</td><td align="right">1.98M</td><td align="right">7.58x</td></tr>
    <tr><td align="left">Static Method Invoke (boxed)</td><td align="right">22.70M</td><td align="right">2.66M</td><td align="right">8.55x</td></tr>
    <tr><td align="left">Constructor Invoke (boxed)</td><td align="right">22.47M</td><td align="right">2.58M</td><td align="right">8.70x</td></tr>
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
    <tr><td align="left">Instance Field Get (typed)</td><td align="right">606.46M</td><td align="right">658.01M</td><td align="right">7.27M</td><td align="right">0.92x</td><td align="right">83.42x</td></tr>
    <tr><td align="left">Instance Field Set (typed)</td><td align="right">637.35M</td><td align="right">656.91M</td><td align="right">5.47M</td><td align="right">0.97x</td><td align="right">116.55x</td></tr>
    <tr><td align="left">Static Field Get (typed)</td><td align="right">621.85M</td><td align="right">692.09M</td><td align="right">8.59M</td><td align="right">0.90x</td><td align="right">72.35x</td></tr>
    <tr><td align="left">Static Field Set (typed)</td><td align="right">625.47M</td><td align="right">671.36M</td><td align="right">6.14M</td><td align="right">0.93x</td><td align="right">101.80x</td></tr>
    <tr><td align="left">Instance Property Get (typed)</td><td align="right">600.74M</td><td align="right">688.18M</td><td align="right">21.74M</td><td align="right">0.87x</td><td align="right">27.63x</td></tr>
    <tr><td align="left">Instance Property Set (typed)</td><td align="right">624.84M</td><td align="right">691.35M</td><td align="right">2.06M</td><td align="right">0.90x</td><td align="right">303.38x</td></tr>
    <tr><td align="left">Static Property Get (typed)</td><td align="right">612.86M</td><td align="right">683.35M</td><td align="right">21.72M</td><td align="right">0.90x</td><td align="right">28.22x</td></tr>
    <tr><td align="left">Static Property Set (typed)</td><td align="right">611.11M</td><td align="right">654.86M</td><td align="right">2.89M</td><td align="right">0.93x</td><td align="right">211.28x</td></tr>
    <tr><td align="left">Instance Method Invoke (typed)</td><td align="right">590.08M</td><td align="right">678.28M</td><td align="right">1.98M</td><td align="right">0.87x</td><td align="right">297.48x</td></tr>
    <tr><td align="left">Static Method Invoke (typed)</td><td align="right">625.72M</td><td align="right">676.09M</td><td align="right">2.66M</td><td align="right">0.93x</td><td align="right">235.57x</td></tr>
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
    <tr><td align="left">Instance Field Get (boxed)</td><td align="right">23.47M</td><td align="right">7.32M</td><td align="right">3.21x</td></tr>
    <tr><td align="left">Instance Field Set (boxed)</td><td align="right">23.17M</td><td align="right">5.44M</td><td align="right">4.26x</td></tr>
    <tr><td align="left">Static Field Get (boxed)</td><td align="right">22.07M</td><td align="right">8.60M</td><td align="right">2.57x</td></tr>
    <tr><td align="left">Static Field Set (boxed)</td><td align="right">10.70M</td><td align="right">2.36M</td><td align="right">4.54x</td></tr>
    <tr><td align="left">Instance Property Get (boxed)</td><td align="right">9.42M</td><td align="right">20.66M</td><td align="right">0.46x</td></tr>
    <tr><td align="left">Instance Property Set (boxed)</td><td align="right">21.57M</td><td align="right">2.05M</td><td align="right">10.50x</td></tr>
    <tr><td align="left">Static Property Get (boxed)</td><td align="right">20.44M</td><td align="right">23.00M</td><td align="right">0.89x</td></tr>
    <tr><td align="left">Static Property Set (boxed)</td><td align="right">23.55M</td><td align="right">2.87M</td><td align="right">8.19x</td></tr>
    <tr><td align="left">Instance Method Invoke (boxed)</td><td align="right">17.58M</td><td align="right">1.95M</td><td align="right">9.03x</td></tr>
    <tr><td align="left">Static Method Invoke (boxed)</td><td align="right">22.43M</td><td align="right">2.66M</td><td align="right">8.42x</td></tr>
    <tr><td align="left">Constructor Invoke (boxed)</td><td align="right">23.28M</td><td align="right">2.55M</td><td align="right">9.12x</td></tr>
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
    <tr><td align="left">Instance Field Get (typed)</td><td align="right">586.62M</td><td align="right">653.32M</td><td align="right">7.32M</td><td align="right">0.90x</td><td align="right">80.18x</td></tr>
    <tr><td align="left">Instance Field Set (typed)</td><td align="right">637.71M</td><td align="right">664.97M</td><td align="right">5.44M</td><td align="right">0.96x</td><td align="right">117.24x</td></tr>
    <tr><td align="left">Static Field Get (typed)</td><td align="right">621.80M</td><td align="right">692.98M</td><td align="right">8.60M</td><td align="right">0.90x</td><td align="right">72.28x</td></tr>
    <tr><td align="left">Static Field Set (typed)</td><td align="right">611.56M</td><td align="right">652.12M</td><td align="right">2.36M</td><td align="right">0.94x</td><td align="right">259.56x</td></tr>
    <tr><td align="left">Instance Property Get (typed)</td><td align="right">594.57M</td><td align="right">696.32M</td><td align="right">20.66M</td><td align="right">0.85x</td><td align="right">28.77x</td></tr>
    <tr><td align="left">Instance Property Set (typed)</td><td align="right">628.75M</td><td align="right">694.19M</td><td align="right">2.05M</td><td align="right">0.91x</td><td align="right">306.18x</td></tr>
    <tr><td align="left">Static Property Get (typed)</td><td align="right">613.37M</td><td align="right">681.83M</td><td align="right">23.00M</td><td align="right">0.90x</td><td align="right">26.67x</td></tr>
    <tr><td align="left">Static Property Set (typed)</td><td align="right">612.51M</td><td align="right">654.56M</td><td align="right">2.87M</td><td align="right">0.94x</td><td align="right">213.09x</td></tr>
    <tr><td align="left">Instance Method Invoke (typed)</td><td align="right">583.62M</td><td align="right">677.29M</td><td align="right">1.95M</td><td align="right">0.86x</td><td align="right">299.64x</td></tr>
    <tr><td align="left">Static Method Invoke (typed)</td><td align="right">628.39M</td><td align="right">683.12M</td><td align="right">2.66M</td><td align="right">0.92x</td><td align="right">235.84x</td></tr>
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
    <tr><td align="left">Instance Field Get (boxed)</td><td align="right">23.05M</td><td align="right">7.29M</td><td align="right">3.16x</td></tr>
    <tr><td align="left">Instance Field Set (boxed)</td><td align="right">23.24M</td><td align="right">5.43M</td><td align="right">4.28x</td></tr>
    <tr><td align="left">Static Field Get (boxed)</td><td align="right">21.82M</td><td align="right">8.59M</td><td align="right">2.54x</td></tr>
    <tr><td align="left">Static Field Set (boxed)</td><td align="right">23.74M</td><td align="right">6.16M</td><td align="right">3.86x</td></tr>
    <tr><td align="left">Instance Property Get (boxed)</td><td align="right">23.17M</td><td align="right">22.17M</td><td align="right">1.05x</td></tr>
    <tr><td align="left">Instance Property Set (boxed)</td><td align="right">21.90M</td><td align="right">2.05M</td><td align="right">10.67x</td></tr>
    <tr><td align="left">Static Property Get (boxed)</td><td align="right">19.71M</td><td align="right">21.61M</td><td align="right">0.91x</td></tr>
    <tr><td align="left">Static Property Set (boxed)</td><td align="right">23.43M</td><td align="right">2.93M</td><td align="right">7.99x</td></tr>
    <tr><td align="left">Instance Method Invoke (boxed)</td><td align="right">15.77M</td><td align="right">1.99M</td><td align="right">7.92x</td></tr>
    <tr><td align="left">Static Method Invoke (boxed)</td><td align="right">21.32M</td><td align="right">1.75M</td><td align="right">12.19x</td></tr>
    <tr><td align="left">Constructor Invoke (boxed)</td><td align="right">3.11M</td><td align="right">1.30M</td><td align="right">2.39x</td></tr>
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
    <tr><td align="left">Instance Field Get (typed)</td><td align="right">611.96M</td><td align="right">651.59M</td><td align="right">7.29M</td><td align="right">0.94x</td><td align="right">83.98x</td></tr>
    <tr><td align="left">Instance Field Set (typed)</td><td align="right">626.22M</td><td align="right">656.37M</td><td align="right">5.43M</td><td align="right">0.95x</td><td align="right">115.34x</td></tr>
    <tr><td align="left">Static Field Get (typed)</td><td align="right">608.85M</td><td align="right">680.73M</td><td align="right">8.59M</td><td align="right">0.89x</td><td align="right">70.89x</td></tr>
    <tr><td align="left">Static Field Set (typed)</td><td align="right">624.15M</td><td align="right">652.36M</td><td align="right">6.16M</td><td align="right">0.96x</td><td align="right">101.37x</td></tr>
    <tr><td align="left">Instance Property Get (typed)</td><td align="right">588.35M</td><td align="right">682.49M</td><td align="right">22.17M</td><td align="right">0.86x</td><td align="right">26.54x</td></tr>
    <tr><td align="left">Instance Property Set (typed)</td><td align="right">624.70M</td><td align="right">694.66M</td><td align="right">2.05M</td><td align="right">0.90x</td><td align="right">304.30x</td></tr>
    <tr><td align="left">Static Property Get (typed)</td><td align="right">613.69M</td><td align="right">681.86M</td><td align="right">21.61M</td><td align="right">0.90x</td><td align="right">28.40x</td></tr>
    <tr><td align="left">Static Property Set (typed)</td><td align="right">613.09M</td><td align="right">655.63M</td><td align="right">2.93M</td><td align="right">0.94x</td><td align="right">209.14x</td></tr>
    <tr><td align="left">Instance Method Invoke (typed)</td><td align="right">584.07M</td><td align="right">677.66M</td><td align="right">1.99M</td><td align="right">0.86x</td><td align="right">293.52x</td></tr>
    <tr><td align="left">Static Method Invoke (typed)</td><td align="right">628.55M</td><td align="right">676.39M</td><td align="right">1.75M</td><td align="right">0.93x</td><td align="right">359.42x</td></tr>
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
    <tr><td align="left">Instance Field Get (boxed)</td><td align="right">7.10M</td><td align="right">7.28M</td><td align="right">0.97x</td></tr>
    <tr><td align="left">Instance Field Set (boxed)</td><td align="right">5.42M</td><td align="right">4.59M</td><td align="right">1.18x</td></tr>
    <tr><td align="left">Static Field Get (boxed)</td><td align="right">6.58M</td><td align="right">8.60M</td><td align="right">0.76x</td></tr>
    <tr><td align="left">Static Field Set (boxed)</td><td align="right">6.12M</td><td align="right">4.11M</td><td align="right">1.49x</td></tr>
    <tr><td align="left">Instance Property Get (boxed)</td><td align="right">23.01M</td><td align="right">22.00M</td><td align="right">1.05x</td></tr>
    <tr><td align="left">Instance Property Set (boxed)</td><td align="right">2.04M</td><td align="right">2.07M</td><td align="right">0.98x</td></tr>
    <tr><td align="left">Static Property Get (boxed)</td><td align="right">17.69M</td><td align="right">22.30M</td><td align="right">0.79x</td></tr>
    <tr><td align="left">Static Property Set (boxed)</td><td align="right">2.87M</td><td align="right">2.91M</td><td align="right">0.98x</td></tr>
    <tr><td align="left">Instance Method Invoke (boxed)</td><td align="right">1.97M</td><td align="right">1.95M</td><td align="right">1.01x</td></tr>
    <tr><td align="left">Static Method Invoke (boxed)</td><td align="right">2.63M</td><td align="right">2.66M</td><td align="right">0.99x</td></tr>
    <tr><td align="left">Constructor Invoke (boxed)</td><td align="right">2.33M</td><td align="right">1.32M</td><td align="right">1.77x</td></tr>
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
    <tr><td align="left">Instance Field Get (typed)</td><td align="right">7.18M</td><td align="right">659.87M</td><td align="right">7.28M</td><td align="right">0.01x</td><td align="right">0.99x</td></tr>
    <tr><td align="left">Instance Field Set (typed)</td><td align="right">5.46M</td><td align="right">656.66M</td><td align="right">4.59M</td><td align="right">0.01x</td><td align="right">1.19x</td></tr>
    <tr><td align="left">Static Field Get (typed)</td><td align="right">8.23M</td><td align="right">681.48M</td><td align="right">8.60M</td><td align="right">0.01x</td><td align="right">0.96x</td></tr>
    <tr><td align="left">Static Field Set (typed)</td><td align="right">4.17M</td><td align="right">652.72M</td><td align="right">4.11M</td><td align="right">0.01x</td><td align="right">1.01x</td></tr>
    <tr><td align="left">Instance Property Get (typed)</td><td align="right">588.37M</td><td align="right">685.00M</td><td align="right">22.00M</td><td align="right">0.86x</td><td align="right">26.74x</td></tr>
    <tr><td align="left">Instance Property Set (typed)</td><td align="right">624.63M</td><td align="right">690.38M</td><td align="right">2.07M</td><td align="right">0.90x</td><td align="right">301.69x</td></tr>
    <tr><td align="left">Static Property Get (typed)</td><td align="right">610.71M</td><td align="right">682.48M</td><td align="right">22.30M</td><td align="right">0.89x</td><td align="right">27.38x</td></tr>
    <tr><td align="left">Static Property Set (typed)</td><td align="right">614.38M</td><td align="right">655.45M</td><td align="right">2.91M</td><td align="right">0.94x</td><td align="right">210.91x</td></tr>
    <tr><td align="left">Instance Method Invoke (typed)</td><td align="right">573.44M</td><td align="right">678.96M</td><td align="right">1.95M</td><td align="right">0.84x</td><td align="right">294.38x</td></tr>
    <tr><td align="left">Static Method Invoke (typed)</td><td align="right">628.94M</td><td align="right">675.41M</td><td align="right">2.66M</td><td align="right">0.93x</td><td align="right">236.59x</td></tr>
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
