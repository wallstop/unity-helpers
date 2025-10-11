**Serialization Guide**

## TL;DR — What Problem This Solves

- Save/load data and configs reliably with JSON or Protobuf using one unified API.
- Unity‑aware converters handle common engine types; pooled buffers keep GC low.
- Pick Pretty/Normal for human‑readable; Fast/FastPOCO for hot paths.

Visuals

![Serialization Flow](Docs/Images/serialization_flow.svg)

This package provides fast, compact serialization for save systems, configuration, and networking with a unified API.

- Json — System.Text.Json with Unity-aware converters
- Protobuf — protobuf-net for compact, schema-evolvable binary
- SystemBinary — .NET BinaryFormatter for legacy/trusted-only scenarios

All formats are exposed via `WallstopStudios.UnityHelpers.Core.Serialization.Serializer` and selected with `SerializationType`.

**Formats Provided**
- Json
  - Human-readable; ideal for settings, debug, modding, and Git diffs.
  - Includes converters for Unity types (Vector2/3/4, Color, Matrix4x4, GameObject, Type, enums as strings), ignores cycles, includes fields, case-insensitive.
- Protobuf (protobuf-net)
  - Small and fast; best for networking and large save payloads.
  - Forward/backward compatible message evolution (see tips below).
- SystemBinary (BinaryFormatter)
  - Only for legacy or trusted, same-version, local data. Avoid for long-term persistence or untrusted input.

**When To Use What**
- Use Json for:
  - Player/tool settings, human-readable saves, serverless workflows, text diffs.
  - Quick iteration and debugging.
- Use Protobuf for:
  - Network payloads and large, bandwidth-sensitive saves.
  - Cases where schema evolves across versions.
- Use SystemBinary only for:
  - Transient caches in trusted environments with exact version match.

**JSON Examples (Unity-aware)**
- Serialize/deserialize and write/read files
```csharp
using System.Collections.Generic;
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Serialization;

public class SaveData
{
    public Vector3 position;
    public Color playerColor;
    public List<GameObject> inventory;
}

var data = new SaveData
{
    position = new Vector3(1, 2, 3),
    playerColor = Color.cyan,
    inventory = new List<GameObject>()
};

// Serialize to UTF-8 JSON bytes (Unity types supported)
byte[] jsonBytes = Serializer.JsonSerialize(data);

// Pretty stringify and parse from string
string jsonText = Serializer.JsonStringify(data, pretty: true);
SaveData fromText = Serializer.JsonDeserialize<SaveData>(jsonText);

// File helpers
Serializer.WriteToJsonFile(data, path: "save.json", pretty: true);
SaveData fromFile = Serializer.ReadFromJsonFile<SaveData>("save.json");

// Generic entry points (choose format at runtime)
byte[] bytes = Serializer.Serialize(data, SerializationType.Json);
SaveData loaded = Serializer.Deserialize<SaveData>(bytes, SerializationType.Json);
```

**Protobuf Examples (Compact + Evolvable)**
- Basic usage
```csharp
using ProtoBuf; // protobuf-net
using WallstopStudios.UnityHelpers.Core.Serialization;

[ProtoContract]
public class PlayerInfo
{
    [ProtoMember(1)] public int id;
    [ProtoMember(2)] public string name;
}

var info = new PlayerInfo { id = 1, name = "Hero" };
byte[] buf = Serializer.ProtoSerialize(info);
PlayerInfo again = Serializer.ProtoDeserialize<PlayerInfo>(buf);

// Generic entry points
byte[] buf2 = Serializer.Serialize(info, SerializationType.Protobuf);
PlayerInfo again2 = Serializer.Deserialize<PlayerInfo>(buf2, SerializationType.Protobuf);

// Buffer reuse (reduce GC in hot paths)
byte[] buffer = null;
int len = Serializer.Serialize(info, SerializationType.Protobuf, ref buffer);
PlayerInfo sliced = Serializer.Deserialize<PlayerInfo>(buffer.AsSpan(0, len).ToArray(), SerializationType.Protobuf);
```

- Unity types with Protobuf: prefer DTOs/surrogates
```csharp
using ProtoBuf;
using UnityEngine;

[ProtoContract]
public struct Vector3DTO
{
    [ProtoMember(1)] public float x;
    [ProtoMember(2)] public float y;
    [ProtoMember(3)] public float z;

    public static implicit operator Vector3(Vector3DTO d) => new Vector3(d.x, d.y, d.z);
    public static implicit operator Vector3DTO(Vector3 v) => new Vector3DTO { x = v.x, y = v.y, z = v.z };
}

[ProtoContract]
public class NetworkMessage
{
    [ProtoMember(1)] public int playerId;
    [ProtoMember(2)] public Vector3DTO position;
}
```

**Protobuf Compatibility Tips**
- Add fields with new numbers; old clients ignore unknown fields; new clients default missing fields.
- Never reuse or renumber existing field tags; reserve removed numbers if needed.
- Avoid changing scalar types on the same number.
- Prefer optional/repeated instead of required.
- Use sensible defaults to minimize payloads.

**Protobuf Polymorphism (Inheritance + Interfaces)**
- Abstract base with [ProtoInclude] (recommended)
  - Protobuf-net does not infer subtype graphs unless you tell it. The recommended pattern is to put `[ProtoContract]` on an abstract base and list all concrete subtypes with `[ProtoInclude(tag, typeof(Subtype))]`.
  - Declare your fields/properties as the abstract base so protobuf can deserialize to the correct subtype.
```csharp
using ProtoBuf;

[ProtoContract]
public abstract class Message { }

[ProtoContract]
public sealed class Ping : Message { [ProtoMember(1)] public int id; }

[ProtoContract]
[ProtoInclude(100, typeof(Ping))]
public abstract class MessageBase : Message { }

[ProtoContract]
public sealed class Envelope { [ProtoMember(1)] public MessageBase payload; }

// round-trip works: Envelope.payload will be Ping at runtime
byte[] bytes = Serializer.ProtoSerialize(new Envelope { payload = new Ping { id = 7 } });
Envelope again = Serializer.ProtoDeserialize<Envelope>(bytes);
```

- Interfaces require a root mapping
  - Protobuf cannot deserialize directly to an interface because it needs a concrete root. You have three options:
    - Use an abstract base with `[ProtoInclude]` and declare fields as that base (preferred).
    - Register a mapping from the interface to a concrete root type at startup:
```csharp
Serializer.RegisterProtobufRoot<IMsg, Ping>();
IMsg msg = Serializer.ProtoDeserialize<IMsg>(bytes);
```
    - Or specify the concrete type with the overload:
```csharp
IMsg msg = Serializer.ProtoDeserialize<IMsg>(bytes, typeof(Ping));
```

- Random system example
  - All PRNGs derive from `AbstractRandom`, which is `[ProtoContract]` and declares each implementation via `[ProtoInclude]`.
  - Do this in your models:
```csharp
[ProtoContract]
public class RNGHolder { [ProtoMember(1)] public AbstractRandom rng; }

// Serialize any implementation without surprises
RNGHolder holder = new RNGHolder { rng = new PcgRandom(seed: 123) };
byte[] buf = Serializer.ProtoSerialize(holder);
RNGHolder rt = Serializer.ProtoDeserialize<RNGHolder>(buf);
```
  - If you truly need an `IRandom` field, register a root or pass the concrete type when deserializing:
```csharp
Serializer.RegisterProtobufRoot<IRandom, PcgRandom>();
IRandom r = Serializer.ProtoDeserialize<IRandom>(bytes);
// or
IRandom r2 = Serializer.ProtoDeserialize<IRandom>(bytes, typeof(PcgRandom));
```

- Tag numbers are API surface
  - Tags in `[ProtoInclude(tag, ...)]` and `[ProtoMember(tag)]` are part of your schema. Add new numbers for new types/fields; never reuse or renumber existing tags once shipped.

**SystemBinary Examples (Legacy/Trusted Only)**
```csharp
using WallstopStudios.UnityHelpers.Core.Serialization;

var obj = new SomeSerializableType();
byte[] bin = Serializer.BinarySerialize(obj);
SomeSerializableType roundtrip = Serializer.BinaryDeserialize<SomeSerializableType>(bin);

// Generic
byte[] bin2 = Serializer.Serialize(obj, SerializationType.SystemBinary);
var round2 = Serializer.Deserialize<SomeSerializableType>(bin2, SerializationType.SystemBinary);
```

Watch-outs
- BinaryFormatter is obsolete for modern .NET and unsafe for untrusted input.
- Version changes often break BinaryFormatter payloads; restrict to same-version caches.

Features
- Unity converters for JSON: Vector2/3/4, Color, Matrix4x4, GameObject, Type
- Protobuf (protobuf-net) integration
- LZMA compression utilities (`Runtime/Utils/LZMA.cs`)
- Pooled buffers/writers to reduce allocations

References
- API: `Runtime/Core/Serialization/Serializer.cs:1`
- LZMA: `Runtime/Utils/LZMA.cs:1`

**Migration**
- Replace direct `System.Text.Json.JsonSerializer` calls in app code with `Serializer.JsonSerialize/JsonDeserialize/JsonStringify`, or with `Serializer.Serialize/Deserialize` + `SerializationType.Json` to centralize options and Unity converters.
- Replace any custom protobuf helpers with `Serializer.ProtoSerialize/ProtoDeserialize` or the generic `Serializer.Serialize/Deserialize` APIs. Ensure models are annotated with `[ProtoContract]` and stable `[ProtoMember(n)]` tags.
- For existing binary saves using BinaryFormatter, prefer migrating to Json or Protobuf. If you must keep BinaryFormatter, scope it to trusted, same-version caches only.
