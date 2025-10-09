**Serialization Guide**

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
