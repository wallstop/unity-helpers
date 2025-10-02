namespace WallstopStudios.UnityHelpers.Core.Extension
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.CompilerServices;
    using WallstopStudios.UnityHelpers.Core.Serialization;
    using WallstopStudios.UnityHelpers.Utils;
    using PbSerializer = ProtoBuf.Serializer;

    /// <summary>
    /// Provides extensions and equality comparers that use protobuf-net serialization output
    /// to compare values for equality, avoiding intermediate byte[] allocations.
    /// </summary>
    public static class ProtoEqualityExtensions
    {
        /// <summary>
        /// Compares two instances for equality by serializing them via protobuf and comparing the resulting bytes.
        /// This implementation writes to reusable MemoryStreams and compares their backing buffers without ToArray().
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ProtoEquals<T>(this T self, T other)
        {
            if (!typeof(T).IsValueType)
            {
                if (ReferenceEquals(self, other))
                {
                    return true;
                }

                if (self is null || other is null)
                {
                    return false;
                }
            }

            using PooledResource<PooledBufferStream> aLease = ProtoBufferComparer.RentStreamA(
                out PooledBufferStream a
            );
            using PooledResource<PooledBufferStream> bLease = ProtoBufferComparer.RentStreamB(
                out PooledBufferStream b
            );
            PbSerializer.Serialize(a, self);
            PbSerializer.Serialize(b, other);
            return ProtoBufferComparer.StreamContentEquals(a, b);
        }

        /// <summary>
        /// Returns an equality comparer that compares values by their protobuf serialization.
        /// </summary>
        public static IEqualityComparer<T> GetProtoComparer<T>()
        {
            return ProtoEqualityComparer<T>.Instance;
        }
    }

    /// <summary>
    /// Generic proto-based equality comparer using protobuf serialization output, with minimal allocations.
    /// </summary>
    public sealed class ProtoEqualityComparer<T> : IEqualityComparer<T>
    {
        public static readonly ProtoEqualityComparer<T> Instance = new();

        private readonly bool _isValueType;

        private ProtoEqualityComparer()
        {
            _isValueType = typeof(T).IsValueType;
        }

        public bool Equals(T x, T y)
        {
            if (!_isValueType)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x is null || y is null)
                {
                    return false;
                }
            }

            using PooledResource<PooledBufferStream> aLease = ProtoBufferComparer.RentStreamA(
                out PooledBufferStream a
            );
            using PooledResource<PooledBufferStream> bLease = ProtoBufferComparer.RentStreamB(
                out PooledBufferStream b
            );
            PbSerializer.Serialize(a, x);
            PbSerializer.Serialize(b, y);
            return ProtoBufferComparer.StreamContentEquals(a, b);
        }

        public int GetHashCode(T obj)
        {
            using PooledResource<PooledBufferStream> sLease = ProtoBufferComparer.RentStreamA(
                out PooledBufferStream s
            );
            PbSerializer.Serialize(s, obj);
            return ProtoBufferComparer.Fnv1A32(s);
        }
    }

    internal static class ProtoBufferComparer
    {
        // We avoid retaining large arrays across calls; each call creates a local MemoryStream,
        // but we ensure no extra array copies by using TryGetBuffer and not calling ToArray().

        private static readonly WallstopGenericPool<PooledBufferStream> StreamPool = new(
            producer: () => new PooledBufferStream(),
            onRelease: s => s.ResetForReuse(),
            onDisposal: stream => stream.Dispose()
        );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PooledResource<PooledBufferStream> RentStreamA(
            out PooledBufferStream stream
        ) => StreamPool.Get(out stream);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PooledResource<PooledBufferStream> RentStreamB(
            out PooledBufferStream stream
        ) => StreamPool.Get(out stream);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StreamContentEquals(PooledBufferStream a, PooledBufferStream b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (a is null || b is null)
            {
                return false;
            }

            ArraySegment<byte> segA = a.GetWrittenSegment();
            ArraySegment<byte> segB = b.GetWrittenSegment();

            if (segA.Count != segB.Count)
            {
                return false;
            }

            // Use Span<T>.SequenceEqual for efficient memory comparison
            return segA.AsSpan().SequenceEqual(segB.AsSpan());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Fnv1A32(PooledBufferStream s)
        {
            const uint fnvOffset = 2166136261;
            const uint fnvPrime = 16777619;
            uint hash = fnvOffset;

            ArraySegment<byte> seg = s.GetWrittenSegment();
            foreach (byte element in seg)
            {
                hash ^= element;
                hash *= fnvPrime;
            }
            return unchecked((int)hash);
        }

        private static bool StreamCompareByRead(MemoryStream a, MemoryStream b)
        {
            long posA = a.Position;
            long posB = b.Position;
            try
            {
                a.Position = 0;
                b.Position = 0;
                int ba;
                do
                {
                    ba = a.ReadByte();
                    int bb = b.ReadByte();
                    if (ba != bb)
                    {
                        return false;
                    }
                } while (ba != -1);
                return true;
            }
            finally
            {
                a.Position = posA;
                b.Position = posB;
            }
        }
    }
}
