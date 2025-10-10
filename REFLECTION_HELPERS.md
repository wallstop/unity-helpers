## ReflectionHelpers — Fast, Safe Reflection for Hot Paths

ReflectionHelpers is a set of utilities for high‑performance reflection in Unity projects. It generates and caches delegates to access fields and properties, call methods and constructors, and quickly create common collections — with safe fallbacks when dynamic IL isn’t available.

Why it exists
- Reflection is flexible but slow when used repeatedly (per‑frame, per‑object, per‑element).
- Standard reflection allocates (boxing, object[] argument arrays) and repeats costly lookups.
- ReflectionHelpers compiles or emits delegates once, caches them, then reuses them to remove ongoing overhead.

What it solves
- Field/property access without per‑call reflection.
- Fast instance/static method invocation (boxed or strongly typed variants).
- Allocation‑free typed static invokers for common cases (e.g., two parameters).
- Zero‑allocation collection creation helpers (array/list/hash set creators, cached by element type).
- Resilient type/attribute scanning that swallows loader errors safely.

When to use it
- Hot paths: serialization, (de)hydration, UI/inspector tooling, ECS‑style systems, property grids.
- Repeated reflective operations over the same members or types.
- When you can cache and reuse delegates across many calls.

When not to use it
- One‑off reflection (e.g., editor button pressed infrequently). Simpler `GetValue/SetValue` is fine.
- If you need full runtime codegen in IL2CPP/WebGL: IL emit isn’t available there. ReflectionHelpers still works, but uses expression compilation or reflection fallback — benefits remain for caching and reduced allocations.
- Setting struct instance fields using boxed setters: prefer the generic ref setter to mutate the original struct (see “Struct note” below).

Key APIs at a glance
- Fields
  - `GetFieldGetter(FieldInfo)` → `Func<object, object>`
  - `GetFieldSetter(FieldInfo)` → `Action<object, object>`
  - `GetFieldGetter<TInstance, TValue>(FieldInfo)` → `Func<TInstance, TValue>`
  - `GetFieldSetter<TInstance, TValue>(FieldInfo)` → `FieldSetter<TInstance, TValue>` (ref setter)
  - `GetStaticFieldGetter<T>(FieldInfo)` / `GetStaticFieldSetter<T>(FieldInfo)`
- Properties
  - `GetPropertyGetter(PropertyInfo)` / `GetPropertySetter(PropertyInfo)` (boxed)
  - `GetPropertyGetter<TInstance, TValue>(PropertyInfo)` (typed)
  - `GetStaticPropertyGetter<T>(PropertyInfo)`
- Methods and constructors
  - `GetMethodInvoker(MethodInfo)` / `GetStaticMethodInvoker(MethodInfo)` (boxed)
  - `GetStaticMethodInvoker<T1, T2, TReturn>(MethodInfo)` (typed two‑param)
  - `GetConstructor(ConstructorInfo)` (boxed) and `GetParameterlessConstructor<T>()`
  - `CreateInstance<T>(params object[])` and generic type construction helpers
- Collections
  - `CreateArray(Type, int)`; `GetArrayCreator(Type)`
  - `CreateList(Type)` / `CreateList(Type, int)`; `GetListCreator(Type)`; `GetListWithCapacityCreator(Type)`
  - `CreateHashSet(Type, int)`; `GetHashSetWithCapacityCreator(Type)`; `GetHashSetAdder(Type)`
- Scanning and attributes
  - `GetAllLoadedAssemblies()` / `GetAllLoadedTypes()`
  - Safe attribute helpers: `HasAttributeSafe`, `GetAttributeSafe`, `GetAllAttributesSafe`, etc.

Usage examples
1) Fast field get/set (boxed)
```csharp
public sealed class Player { public int Score; }

FieldInfo score = typeof(Player).GetField("Score");
var getScore = ReflectionHelpers.GetFieldGetter(score);     // object -> object
var setScore = ReflectionHelpers.GetFieldSetter(score);     // (object, object) -> void

var p = new Player();
setScore(p, 42);
UnityEngine.Debug.Log((int)getScore(p)); // 42
```

2) Struct note: use typed ref setter
```csharp
public struct Stat { public int Value; }
FieldInfo valueField = typeof(Stat).GetField("Value");

// Prefer typed ref setter for structs
var setValue = ReflectionHelpers.GetFieldSetter<Stat, int>(valueField);
Stat s = default;
setValue(ref s, 100);
// s.Value == 100
```

3) Typed property getter
```csharp
var prop = typeof(Camera).GetProperty("orthographicSize");
var getSize = ReflectionHelpers.GetPropertyGetter<Camera, float>(prop);
float size = getSize(UnityEngine.Camera.main);
```

4) Fast static method invoker (two params, typed)
```csharp
MethodInfo concat = typeof(string).GetMethod(
    nameof(string.Concat), new[] { typeof(string), typeof(string) }
);
var concat2 = ReflectionHelpers.GetStaticMethodInvoker<string, string, string>(concat);
string joined = concat2("Hello ", "World");
```

5) Low‑allocation constructors
```csharp
// Parameterless constructor
var newList = ReflectionHelpers.GetParameterlessConstructor<List<int>>();
List<int> list = newList();

// Constructor via ConstructorInfo
ConstructorInfo ci = typeof(Dictionary<string, int>)
    .GetConstructor(new[] { typeof(int) });
var ctor = ReflectionHelpers.GetConstructor(ci);
var dict = (Dictionary<string, int>)ctor(new object[] { 128 });
```

6) Collection creators and HashSet adder
```csharp
var makeArray = ReflectionHelpers.GetArrayCreator(typeof(Vector3));
Array positions = makeArray(256); // Vector3[256]

IList names = ReflectionHelpers.CreateList(typeof(string), 64); // List<string>

object set = ReflectionHelpers.CreateHashSet(typeof(int), 0); // HashSet<int>
var add = ReflectionHelpers.GetHashSetAdder(typeof(int));
add(set, 1);
add(set, 1);
add(set, 2);
// set contains {1, 2}
```

7) Safe attribute scanning
```csharp
bool hasObsolete = ReflectionHelpers.HasAttributeSafe<ObsoleteAttribute>(typeof(MyComponent));
var values = ReflectionHelpers.GetAllAttributeValuesSafe(typeof(MyComponent));
// e.g., values["Obsolete"] -> ObsoleteAttribute instance
```

Performance tips
- Cache delegates (getters/setters/invokers) once and reuse them.
- Prefer typed APIs (`GetFieldGetter<TInstance, TValue>`, typed static invokers) to avoid boxing and object[] allocations.
- Use creators (`GetListCreator`, `GetArrayCreator`) in loops to avoid reflection/Activator costs.

IL2CPP/WebGL notes
- Dynamic IL emit is disabled on IL2CPP/WebGL; ReflectionHelpers automatically falls back to expression compilation or direct reflection where necessary.
- Caching still reduces overhead significantly, even without IL emit.

Thread‑safety
- Caches use thread‑safe dictionaries by default. A `SINGLE_THREADED` build flag switches to regular dictionaries for very constrained environments.

Common pitfalls
- Passing a non‑static `FieldInfo`/`PropertyInfo` to static getters/setters will throw clear `ArgumentException`s.
- Read‑only properties do not have setters; using `GetPropertySetter` on those throws.
- Struct instance field writes require the generic ref setter (`FieldSetter<TInstance, TValue>`) to mutate the original struct.

See also
- Runtime/Core/Helper/ReflectionHelpers.cs for full XML docs and additional examples.

