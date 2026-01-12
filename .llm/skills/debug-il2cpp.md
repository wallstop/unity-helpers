# Skill: Debug IL2CPP

<!-- trigger: il2cpp, aot, build, platform, webgl | IL2CPP build issues or AOT errors | Feature -->

**Trigger**: When debugging IL2CPP build issues, platform-specific problems, or AOT compilation errors.

---

## Common IL2CPP Issues

### 1. Code Stripping

IL2CPP strips unused code. Reflection targets may be removed.

**Symptoms**:

- `TypeLoadException` at runtime
- Missing methods/types in builds
- Works in Editor, fails in build

**Solutions**:

```csharp
// Mark types accessed via reflection
[Preserve]
public class MyReflectedClass
{
    [Preserve]
    public void ReflectedMethod() { }
}
```

Or use `link.xml`:

```xml
<linker>
    <assembly fullname="Assembly-CSharp">
        <type fullname="MyNamespace.MyClass" preserve="all"/>
    </assembly>
</linker>
```

### 2. Generic Virtual Methods

**Symptoms**:

- `ExecutionEngineException`
- Missing method exceptions for generic calls

**Solution**: Avoid generic virtual methods, or ensure concrete instantiations exist:

```csharp
// ❌ Problematic
public virtual T GetValue<T>() { ... }

// ✅ Better - use non-generic
public virtual object GetValue(Type type) { ... }

// ✅ Or ensure instantiations exist
private void EnsureGenericInstantiations()
{
    GetValue<int>();    // Forces AOT compilation
    GetValue<string>();
    GetValue<float>();
}
```

### 3. Reflection.Emit

**Symptoms**:

- `PlatformNotSupportedException`
- Dynamic code generation failures

**Solution**: IL2CPP doesn't support `System.Reflection.Emit`. Use alternatives:

```csharp
// ❌ Not supported
DynamicMethod method = new DynamicMethod(...);

// ✅ Use expression trees (limited support)
Expression<Func<int, int>> expr = x => x * 2;
Func<int, int> func = expr.Compile();

// ✅ Or use source generators (compile-time)
```

---

## Forbidden C# Features

These cause IL2CPP compilation failures or runtime issues:

| Feature                              | Issue                     |
| ------------------------------------ | ------------------------- |
| Nullable reference types (`string?`) | Compilation failures      |
| `#nullable enable`                   | Not supported             |
| Null-forgiving operator (`!`)        | Requires nullable context |
| `required` modifier                  | C# 11, not available      |
| `init` accessors                     | Limited support           |
| File-scoped types                    | C# 11, not available      |
| Raw string literals                  | C# 11, not available      |
| Generic attributes                   | C# 11, not available      |
| Static abstract interface members    | Limited support           |

---

## Platform-Specific Constraints

### WebGL

```csharp
// ❌ No threading
Task.Run(() => { ... });
new Thread(() => { ... });

// ❌ No file system
File.ReadAllText(path);
Directory.GetFiles(path);

// ✅ Use Unity APIs
UnityWebRequest.Get(url);
PlayerPrefs.GetString(key);
```

### iOS (AOT)

```csharp
// ❌ No runtime code generation
Activator.CreateInstance(type);  // May fail for some types

// ✅ Use factory methods
public static T Create<T>() where T : new() => new T();

// ✅ Register types explicitly
[Preserve]
private static void RegisterTypes()
{
    // Force AOT compilation
    var _ = new MyClass();
}
```

### Android

```csharp
// 64-bit requirements - ensure all native plugins support arm64

// JNI limitations - be careful with AndroidJavaObject
using (AndroidJavaClass jc = new AndroidJavaClass("com.example.MyClass"))
{
    // Keep references short-lived
}
```

---

## Debugging Techniques

### 1. Check IL2CPP Logs

Build logs contain IL2CPP errors:

- Windows: `%LOCALAPPDATA%\Unity\Editor\Editor.log`
- macOS: `~/Library/Logs/Unity/Editor.log`

Look for:

- `IL2CPP error`
- `Unresolved extern method`
- `GenericInstanceMethod`

### 2. Development Builds

```csharp
// Enable Development Build in Build Settings
// Provides better error messages and stack traces
```

### 3. Managed Stripping Level

In Player Settings > Other Settings > Managed Stripping Level:

- **Minimal**: Less stripping, larger build
- **Low**: Some stripping
- **Medium**: Balanced (default)
- **High**: Aggressive stripping, smallest build

Try **Low** or **Minimal** if experiencing stripping issues.

### 4. Script Debugging

Enable "Script Debugging" in Build Settings for:

- Breakpoints in IL2CPP builds
- Better stack traces
- Slower performance (debug only)

---

## Testing Checklist

### Before Release

1. **Test on actual hardware** — Simulators may hide issues
2. **Test IL2CPP specifically** — Don't assume Mono behavior matches
3. **Check all platforms** — Each has unique constraints
4. **Verify all reflection usage** — Ensure `[Preserve]` is applied
5. **Test with high stripping** — Catches missing preservations

### Quick IL2CPP Test

```csharp
#if ENABLE_IL2CPP
    Debug.Log("Running on IL2CPP");
#else
    Debug.Log("Running on Mono");
#endif
```

---

## Preserve Patterns

### Class Level

```csharp
[Preserve]
public class MySerializedClass
{
    public int Value;
}
```

### Assembly Level

```csharp
// In AssemblyInfo.cs
[assembly: Preserve]
```

### link.xml (Fine-Grained)

```xml
<linker>
    <!-- Preserve entire assembly -->
    <assembly fullname="MyAssembly" preserve="all"/>

    <!-- Preserve specific type -->
    <assembly fullname="Assembly-CSharp">
        <type fullname="MyNamespace.MyClass" preserve="all"/>
    </assembly>

    <!-- Preserve specific members -->
    <assembly fullname="Assembly-CSharp">
        <type fullname="MyNamespace.MyClass">
            <method name="MyMethod"/>
            <field name="myField"/>
        </type>
    </assembly>
</linker>
```

---

## Common Error Messages

| Error                           | Likely Cause      | Solution                     |
| ------------------------------- | ----------------- | ---------------------------- |
| `TypeLoadException`             | Type stripped     | Add `[Preserve]` or link.xml |
| `MissingMethodException`        | Method stripped   | Add `[Preserve]`             |
| `ExecutionEngineException`      | Generic AOT issue | Avoid generic virtuals       |
| `PlatformNotSupportedException` | Unsupported API   | Use alternative API          |
| `NotSupportedException: IL2CPP` | Reflection.Emit   | Avoid dynamic code gen       |

---

## Unity Helpers Compatibility

This package is tested on IL2CPP with these considerations:

1. **Serialization**: JSON and Protobuf work correctly
2. **Reflection**: Minimal, with `[Preserve]` where needed
3. **PRNGs**: All implementations are AOT-compatible
4. **Collections**: No dynamic code generation
5. **Spatial structures**: Pure managed code

If you encounter IL2CPP issues with Unity Helpers, check:

1. Proper assembly references in `.asmdef`
2. No accidental use of reflection in your code
3. Stripping level settings
