Serialization – JSON Converters

Demonstrates serialization of Unity types using System.Text.Json with Unity Helpers’ built-in converters and helper APIs.

How to use

- Add `JsonSerializationDemo` to any GameObject and press Play.
- Check the Console for serialized JSON, byte sizes, and a round-trip decode.

What it shows

- `Serializer.JsonStringify` and `Serializer.JsonDeserialize<T>` for text workflows.
- `Serializer.JsonSerialize` and `Serializer.JsonDeserialize<T>(byte[])` for UTF-8 bytes.
- Using `Serializer.CreateFastJsonOptions()` for hot paths.
