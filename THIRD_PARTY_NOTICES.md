This package contains third-party software components governed by the license(s) indicated below.

protobuf-net
- Description: .NET runtime/library for Protocol Buffers serialization by Marc Gravell.
- Upstream: https://github.com/protobuf-net/protobuf-net
- License: Apache License 2.0
- License URL: https://www.apache.org/licenses/LICENSE-2.0
- Notes: Uses attributes such as [ProtoContract]/[ProtoMember] and runtime `ProtoBuf.Serializer`.

7-Zip LZMA SDK
- Description: LZMA compression/decompression implementation (encoder/decoder) used via `SevenZip.Compression.LZMA`.
- Upstream: https://www.7-zip.org/sdk.html
- License: Public Domain (per 7-Zip LZMA SDK)
- Notes: Integrated sources under `Runtime/Utils/SevenZip/Compress/LZMA`.

Additional notes
- System.Text.Json and other .NET BCL components are used as part of the .NET runtime and are subject to their respective licenses (e.g., MIT for dotnet/runtime). No vendored sources from these components are included in this repository.
