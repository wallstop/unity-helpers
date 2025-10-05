namespace WallstopStudios.UnityHelpers.Core.Serialization
{
    using System;
    using System.Buffers;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;
    using JsonConverters;
    using ProtoBuf;
    using TypeConverter = JsonConverters.TypeConverter;

    internal static class SerializerEncoding
    {
        public static readonly Encoding Encoding;
        public static readonly JsonSerializerOptions NormalJsonOptions;
        public static readonly JsonSerializerOptions PrettyJsonOptions;

        static SerializerEncoding()
        {
            Encoding = Encoding.UTF8;
            NormalJsonOptions = new JsonSerializerOptions
            {
                IgnoreReadOnlyFields = false,
                IgnoreReadOnlyProperties = false,
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                IncludeFields = true,
                PropertyNameCaseInsensitive = true,
                Converters =
                {
                    new JsonStringEnumConverter(),
                    Vector3Converter.Instance,
                    Vector2Converter.Instance,
                    Vector4Converter.Instance,
                    Matrix4x4Converter.Instance,
                    TypeConverter.Instance,
                    GameObjectConverter.Instance,
                    ColorConverter.Instance,
                },
            };

            PrettyJsonOptions = new JsonSerializerOptions
            {
                IgnoreReadOnlyFields = false,
                IgnoreReadOnlyProperties = false,
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                PropertyNameCaseInsensitive = true,
                IncludeFields = true,
                Converters =
                {
                    new JsonStringEnumConverter(),
                    Vector3Converter.Instance,
                    Vector2Converter.Instance,
                    Vector4Converter.Instance,
                    Matrix4x4Converter.Instance,
                    TypeConverter.Instance,
                    GameObjectConverter.Instance,
                    ColorConverter.Instance,
                },
                WriteIndented = true,
            };
        }
    }

    public enum SerializationType
    {
        None = 0,
        SystemBinary = 1,
        Protobuf = 2,
        Json = 3,
    }

    public static class Serializer
    {
        private static readonly Utils.WallstopGenericPool<BinaryFormatter> BinaryFormatterPool =
            new(() => new BinaryFormatter());

        private static readonly Utils.WallstopGenericPool<Utf8JsonWriter> JsonWriterPool = new(
            () => new Utf8JsonWriter(Stream.Null),
            onRelease: writer =>
            {
                writer.Reset(Stream.Null);
            },
            onDisposal: stream => stream.Dispose()
        );

        public static T Deserialize<T>(byte[] serialized, SerializationType serializationType)
        {
            switch (serializationType)
            {
                case SerializationType.SystemBinary:
                {
                    return BinaryDeserialize<T>(serialized);
                }
                case SerializationType.Protobuf:
                {
                    return ProtoDeserialize<T>(serialized);
                }
                case SerializationType.Json:
                {
                    string serializedString = SerializerEncoding.Encoding.GetString(serialized);
                    return JsonDeserialize<T>(serializedString);
                }
                default:
                {
                    throw new InvalidEnumArgumentException(
                        nameof(serializationType),
                        (int)serializationType,
                        typeof(SerializationType)
                    );
                }
            }
        }

        public static byte[] Serialize<T>(T instance, SerializationType serializationType)
        {
            switch (serializationType)
            {
                case SerializationType.SystemBinary:
                {
                    return BinarySerialize(instance);
                }
                case SerializationType.Protobuf:
                {
                    return ProtoSerialize(instance);
                }
                case SerializationType.Json:
                {
                    return JsonSerialize(instance);
                }
                default:
                {
                    throw new InvalidEnumArgumentException(
                        nameof(serializationType),
                        (int)serializationType,
                        typeof(SerializationType)
                    );
                }
            }
        }

        public static int Serialize<T>(
            T instance,
            SerializationType serializationType,
            ref byte[] buffer
        )
        {
            switch (serializationType)
            {
                case SerializationType.SystemBinary:
                {
                    return BinarySerialize(instance, ref buffer);
                }
                case SerializationType.Protobuf:
                {
                    return ProtoSerialize(instance, ref buffer);
                }
                case SerializationType.Json:
                {
                    return JsonSerialize(instance, ref buffer);
                }
                default:
                {
                    throw new InvalidEnumArgumentException(
                        nameof(serializationType),
                        (int)serializationType,
                        typeof(SerializationType)
                    );
                }
            }
        }

        public static T BinaryDeserialize<T>(byte[] data)
        {
            using Utils.PooledResource<PooledReadOnlyMemoryStream> lease =
                PooledReadOnlyMemoryStream.Rent(out PooledReadOnlyMemoryStream stream);
            stream.SetBuffer(data);
            using Utils.PooledResource<BinaryFormatter> fmtLease = BinaryFormatterPool.Get(
                out BinaryFormatter binaryFormatter
            );
            return (T)binaryFormatter.Deserialize(stream);
        }

        public static byte[] BinarySerialize<T>(T input)
        {
            using Utils.PooledResource<PooledBufferStream> lease = PooledBufferStream.Rent(
                out PooledBufferStream stream
            );
            using Utils.PooledResource<BinaryFormatter> fmtLease = BinaryFormatterPool.Get(
                out BinaryFormatter binaryFormatter
            );
            binaryFormatter.Serialize(stream, input);
            byte[] buffer = null;
            stream.ToArrayExact(ref buffer);
            return buffer;
        }

        public static int BinarySerialize<T>(T input, ref byte[] buffer)
        {
            using Utils.PooledResource<PooledBufferStream> lease = PooledBufferStream.Rent(
                out PooledBufferStream stream
            );
            using Utils.PooledResource<BinaryFormatter> fmtLease = BinaryFormatterPool.Get(
                out BinaryFormatter binaryFormatter
            );
            binaryFormatter.Serialize(stream, input);
            return stream.ToArrayExact(ref buffer);
        }

        public static T ProtoDeserialize<T>(byte[] data)
        {
            if (data == null)
            {
                throw new ProtoException("No data provided for Protobuf deserialization.");
            }

            using Utils.PooledResource<PooledReadOnlyMemoryStream> lease =
                PooledReadOnlyMemoryStream.Rent(out PooledReadOnlyMemoryStream stream);
            stream.SetBuffer(data);
            try
            {
                return ProtoBuf.Serializer.Deserialize<T>(stream);
            }
            catch (ProtoException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ProtoException(
                    "Protobuf deserialization failed: invalid or corrupted data.",
                    ex
                );
            }
        }

        public static T ProtoDeserialize<T>(byte[] data, Type type)
        {
            if (data == null)
            {
                throw new ArgumentException(nameof(data));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            using Utils.PooledResource<PooledReadOnlyMemoryStream> lease =
                PooledReadOnlyMemoryStream.Rent(out PooledReadOnlyMemoryStream stream);
            stream.SetBuffer(data);
            try
            {
                return (T)ProtoBuf.Serializer.Deserialize(type, stream);
            }
            catch (ProtoException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ProtoException(
                    "Protobuf deserialization failed: invalid or corrupted data.",
                    ex
                );
            }
        }

        public static byte[] ProtoSerialize<T>(T input)
        {
            using Utils.PooledResource<PooledBufferStream> lease = PooledBufferStream.Rent(
                out PooledBufferStream stream
            );
            ProtoBuf.Serializer.Serialize(stream, input);
            byte[] buffer = null;
            stream.ToArrayExact(ref buffer);
            return buffer;
        }

        public static int ProtoSerialize<T>(T input, ref byte[] buffer)
        {
            using Utils.PooledResource<PooledBufferStream> lease = PooledBufferStream.Rent(
                out PooledBufferStream stream
            );
            ProtoBuf.Serializer.Serialize(stream, input);
            return stream.ToArrayExact(ref buffer);
        }

        public static T JsonDeserialize<T>(
            string data,
            Type type = null,
            JsonSerializerOptions options = null
        )
        {
            return (T)
                JsonSerializer.Deserialize(
                    data,
                    type ?? typeof(T),
                    options ?? SerializerEncoding.NormalJsonOptions
                );
        }

        public static byte[] JsonSerialize<T>(T input)
        {
            using Utils.PooledResource<PooledBufferStream> lease = PooledBufferStream.Rent(
                out PooledBufferStream stream
            );
            WriteJsonToStream(input, SerializerEncoding.NormalJsonOptions, stream);
            byte[] buffer = null;
            stream.ToArrayExact(ref buffer);
            return buffer;
        }

        public static int JsonSerialize<T>(T input, ref byte[] buffer)
        {
            using Utils.PooledResource<PooledBufferStream> lease = PooledBufferStream.Rent(
                out PooledBufferStream stream
            );
            WriteJsonToStream(input, SerializerEncoding.NormalJsonOptions, stream);
            return stream.ToArrayExact(ref buffer);
        }

        private static void WriteJsonToStream<T>(
            T input,
            JsonSerializerOptions options,
            Stream stream
        )
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            using (JsonWriterPool.Get(out Utf8JsonWriter writer))
            {
                writer.Reset(stream);
                Type parameterType = typeof(T);
                if (
                    parameterType.IsAbstract
                    || parameterType.IsInterface
                    || parameterType == typeof(object)
                )
                {
                    object data = input;
                    if (data == null)
                    {
                        writer.WriteStartObject();
                        writer.WriteEndObject();
                        writer.Flush();
                        return;
                    }

                    Type type = data.GetType();
                    JsonSerializer.Serialize(writer, data, type, options);
                }
                else
                {
                    JsonSerializer.Serialize(writer, input, options);
                }
                writer.Flush();
            }
        }

        public static string JsonStringify<T>(T input, bool pretty = false)
        {
            JsonSerializerOptions options = pretty
                ? SerializerEncoding.PrettyJsonOptions
                : SerializerEncoding.NormalJsonOptions;

            return JsonStringify(input, options);
        }

        public static string JsonStringify<T>(T input, JsonSerializerOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            Type parameterType = typeof(T);
            if (
                parameterType.IsAbstract
                || parameterType.IsInterface
                || parameterType == typeof(object)
            )
            {
                object data = input;
                if (data == null)
                {
                    return "{}";
                }

                Type type = data.GetType();
                return JsonSerializer.Serialize(data, type, options);
            }

            return JsonSerializer.Serialize(input, options);
        }

        public static T ReadFromJsonFile<T>(string path)
        {
            string settingsAsText = File.ReadAllText(path, SerializerEncoding.Encoding);
            return JsonDeserialize<T>(settingsAsText);
        }

        public static async Task<T> ReadFromJsonFileAsync<T>(string path)
        {
            string settingsAsText = await File.ReadAllTextAsync(path, SerializerEncoding.Encoding);
            return JsonDeserialize<T>(settingsAsText);
        }

        public static void WriteToJsonFile<T>(T input, string path, bool pretty = true)
        {
            string jsonAsText = JsonStringify(input, pretty);
            File.WriteAllText(path, jsonAsText);
        }

        public static async Task WriteToJsonFileAsync<T>(T input, string path, bool pretty = true)
        {
            string jsonAsText = JsonStringify(input, pretty);
            await File.WriteAllTextAsync(path, jsonAsText);
        }

        public static void WriteToJsonFile<T>(T input, string path, JsonSerializerOptions options)
        {
            string jsonAsText = JsonStringify(input, options);
            File.WriteAllText(path, jsonAsText);
        }

        public static async Task WriteToJsonFileAsync<T>(
            T input,
            string path,
            JsonSerializerOptions options
        )
        {
            string jsonAsText = JsonStringify(input, options);
            await File.WriteAllTextAsync(path, jsonAsText);
        }
    }

    // Internal pooled, growable write stream backed by ArrayPool<byte> to reduce allocations
    internal sealed class PooledBufferStream : Stream
    {
        private const int DefaultInitialCapacity = 256;

        private byte[] _buffer;
        private int _length;
        private int _position;
        private bool _disposed;

        private static readonly Utils.WallstopGenericPool<PooledBufferStream> Pool = new(
            producer: () => new PooledBufferStream(),
            onRelease: stream => stream.ResetForReuse(),
            onDisposal: stream => stream.Dispose()
        );

        public static Utils.PooledResource<PooledBufferStream> Rent(
            out PooledBufferStream stream
        ) => Pool.Get(out stream);

        private PooledBufferStream(int initialCapacity = DefaultInitialCapacity)
        {
            if (initialCapacity < 1)
            {
                initialCapacity = DefaultInitialCapacity;
            }

            _buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
            _length = 0;
            _position = 0;
        }

        internal ArraySegment<byte> GetWrittenSegment()
        {
            return new ArraySegment<byte>(_buffer, 0, _length);
        }

        private void ResetForReuse()
        {
            _length = 0;
            _position = 0;
            _disposed = false;
        }

        public override bool CanRead => false;
        public override bool CanSeek => true;
        public override bool CanWrite => true;
        public override long Length => _length;

        public override long Position
        {
            get => _position;
            set => Seek(value, SeekOrigin.Begin);
        }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            int basePos = origin switch
            {
                SeekOrigin.Begin => 0,
                SeekOrigin.Current => _position,
                SeekOrigin.End => _length,
                _ => 0,
            };
            long newPos = basePos + offset;
            if (newPos is < 0 or > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            _position = (int)newPos;
            return _position;
        }

        public override void SetLength(long value)
        {
            if (value is < 0 or > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
            int newLen = (int)value;
            EnsureCapacity(newLen);
            _length = newLen;
            if (_position > _length)
            {
                _position = _length;
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            int endPos = _position + count;
            EnsureCapacity(endPos);
            Array.Copy(buffer, offset, _buffer, _position, count);
            _position = endPos;
            if (endPos > _length)
            {
                _length = endPos;
            }
        }

#if NETSTANDARD2_1 || NET5_0_OR_GREATER
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            int count = buffer.Length;
            int endPos = _position + count;
            EnsureCapacity(endPos);
            buffer.CopyTo(new Span<byte>(_buffer, _position, count));
            _position = endPos;
            if (endPos > _length)
            {
                _length = endPos;
            }
        }
#endif

        public override void WriteByte(byte value)
        {
            int endPos = _position + 1;
            EnsureCapacity(endPos);
            _buffer[_position] = value;
            _position = endPos;
            if (endPos > _length)
            {
                _length = endPos;
            }
        }

        private void EnsureCapacity(int required)
        {
            if (_buffer.Length >= required)
            {
                return;
            }

            int newSize = _buffer.Length;
            if (newSize < DefaultInitialCapacity)
            {
                newSize = DefaultInitialCapacity;
            }

            while (newSize < required)
            {
                newSize = newSize < 1024 ? newSize * 2 : newSize + (newSize >> 1);
            }
            byte[] newBuf = ArrayPool<byte>.Shared.Rent(newSize);
            if (_length > 0)
            {
                Array.Copy(_buffer, newBuf, _length);
            }
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = newBuf;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (_buffer != null)
                {
                    ArrayPool<byte>.Shared.Return(_buffer);
                    _buffer = Array.Empty<byte>();
                }
                _length = 0;
                _position = 0;
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        public int ToArrayExact(ref byte[] buffer)
        {
            if (buffer == null || buffer.Length < _length)
            {
                buffer = new byte[_length];
            }

            if (_length > 0)
            {
                Array.Copy(_buffer, buffer, _length);
            }

            return _length;
        }
    }

    // Internal pooled read-only stream over an existing byte[] to avoid MemoryStream allocation in deserialization paths
    internal sealed class PooledReadOnlyMemoryStream : Stream
    {
        private byte[] _buffer = Array.Empty<byte>();
        private int _position;
        private int _length;

        private static readonly Utils.WallstopGenericPool<PooledReadOnlyMemoryStream> Pool = new(
            producer: () => new PooledReadOnlyMemoryStream(),
            onRelease: s =>
            {
                s.ResetForReuse();
            }
        );

        public static Utils.PooledResource<PooledReadOnlyMemoryStream> Rent(
            out PooledReadOnlyMemoryStream stream
        ) => Pool.Get(out stream);

        public void SetBuffer(byte[] buffer)
        {
            _buffer = buffer ?? Array.Empty<byte>();
            _position = 0;
            _length = _buffer.Length;
        }

        private void ResetForReuse()
        {
            SetBuffer(Array.Empty<byte>());
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => _length;

        public override long Position
        {
            get => _position;
            set
            {
                if (value is < 0 or > int.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _position = (int)value;
            }
        }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if ((uint)offset > buffer.Length || (uint)count > buffer.Length - offset)
            {
                throw new ArgumentOutOfRangeException();
            }
            int remaining = _length - _position;
            if (remaining <= 0)
            {
                return 0;
            }
            if (count > remaining)
            {
                count = remaining;
            }

            Array.Copy(_buffer, _position, buffer, offset, count);
            _position += count;
            return count;
        }

        public override int ReadByte()
        {
            if (_position >= _length)
            {
                return -1;
            }

            return _buffer[_position++];
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            int basePos = origin switch
            {
                SeekOrigin.Begin => 0,
                SeekOrigin.Current => _position,
                SeekOrigin.End => _length,
                _ => 0,
            };
            long newPos = basePos + offset;
            if (newPos is < 0 or > int.MaxValue)
            {
                throw new IOException("Attempted to seek outside the stream bounds.");
            }
            _position = (int)newPos;
            return _position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override void WriteByte(byte value)
        {
            throw new NotSupportedException();
        }
    }
}
