namespace WallstopStudios.UnityHelpers.Core.Serialization
{
    using System;
    using ProtoBuf;
    using ProtoBuf.Meta;
    using UnityEngine;

    // Surrogates allow protobuf-net to serialize Unity structs we cannot annotate directly.
    [ProtoContract]
    internal struct Vector2Surrogate
    {
        [ProtoMember(1)]
        public float x;

        [ProtoMember(2)]
        public float y;

        public static implicit operator Vector2Surrogate(Vector2 v) =>
            new Vector2Surrogate { x = v.x, y = v.y };

        public static implicit operator Vector2(Vector2Surrogate s) => new Vector2(s.x, s.y);
    }

    [ProtoContract]
    internal struct Vector3Surrogate
    {
        [ProtoMember(1)]
        public float x;

        [ProtoMember(2)]
        public float y;

        [ProtoMember(3)]
        public float z;

        public static implicit operator Vector3Surrogate(Vector3 v) =>
            new Vector3Surrogate
            {
                x = v.x,
                y = v.y,
                z = v.z,
            };

        public static implicit operator Vector3(Vector3Surrogate s) => new Vector3(s.x, s.y, s.z);
    }

    [ProtoContract]
    internal struct QuaternionSurrogate
    {
        [ProtoMember(1)]
        public float x;

        [ProtoMember(2)]
        public float y;

        [ProtoMember(3)]
        public float z;

        [ProtoMember(4)]
        public float w;

        public static implicit operator QuaternionSurrogate(Quaternion q) =>
            new QuaternionSurrogate
            {
                x = q.x,
                y = q.y,
                z = q.z,
                w = q.w,
            };

        public static implicit operator Quaternion(QuaternionSurrogate s) =>
            new Quaternion(s.x, s.y, s.z, s.w);
    }

    [ProtoContract]
    internal struct ColorSurrogate
    {
        [ProtoMember(1)]
        public float r;

        [ProtoMember(2)]
        public float g;

        [ProtoMember(3)]
        public float b;

        [ProtoMember(4)]
        public float a;

        public static implicit operator ColorSurrogate(Color c) =>
            new ColorSurrogate
            {
                r = c.r,
                g = c.g,
                b = c.b,
                a = c.a,
            };

        public static implicit operator Color(ColorSurrogate s) => new Color(s.r, s.g, s.b, s.a);
    }

    [ProtoContract]
    internal struct Color32Surrogate
    {
        [ProtoMember(1)]
        public byte r;

        [ProtoMember(2)]
        public byte g;

        [ProtoMember(3)]
        public byte b;

        [ProtoMember(4)]
        public byte a;

        public static implicit operator Color32Surrogate(Color32 c) =>
            new Color32Surrogate
            {
                r = c.r,
                g = c.g,
                b = c.b,
                a = c.a,
            };

        public static implicit operator Color32(Color32Surrogate s) =>
            new Color32(s.r, s.g, s.b, s.a);
    }

    [ProtoContract]
    internal struct RectSurrogate
    {
        [ProtoMember(1)]
        public float x;

        [ProtoMember(2)]
        public float y;

        [ProtoMember(3)]
        public float width;

        [ProtoMember(4)]
        public float height;

        public static implicit operator RectSurrogate(Rect r) =>
            new RectSurrogate
            {
                x = r.x,
                y = r.y,
                width = r.width,
                height = r.height,
            };

        public static implicit operator Rect(RectSurrogate s) =>
            new Rect(s.x, s.y, s.width, s.height);
    }

    [ProtoContract]
    internal struct RectIntSurrogate
    {
        [ProtoMember(1)]
        public int x;

        [ProtoMember(2)]
        public int y;

        [ProtoMember(3)]
        public int width;

        [ProtoMember(4)]
        public int height;

        public static implicit operator RectIntSurrogate(RectInt r) =>
            new RectIntSurrogate
            {
                x = r.x,
                y = r.y,
                width = r.width,
                height = r.height,
            };

        public static implicit operator RectInt(RectIntSurrogate s) =>
            new RectInt(s.x, s.y, s.width, s.height);
    }

    [ProtoContract]
    internal struct BoundsSurrogate
    {
        [ProtoMember(1)]
        public float cx;

        [ProtoMember(2)]
        public float cy;

        [ProtoMember(3)]
        public float cz;

        [ProtoMember(4)]
        public float sx;

        [ProtoMember(5)]
        public float sy;

        [ProtoMember(6)]
        public float sz;

        public static implicit operator BoundsSurrogate(Bounds b) =>
            new BoundsSurrogate
            {
                cx = b.center.x,
                cy = b.center.y,
                cz = b.center.z,
                sx = b.size.x,
                sy = b.size.y,
                sz = b.size.z,
            };

        public static implicit operator Bounds(BoundsSurrogate s) =>
            new Bounds(new Vector3(s.cx, s.cy, s.cz), new Vector3(s.sx, s.sy, s.sz));
    }

    [ProtoContract]
    internal struct BoundsIntSurrogate
    {
        [ProtoMember(1)]
        public int px;

        [ProtoMember(2)]
        public int py;

        [ProtoMember(3)]
        public int pz;

        [ProtoMember(4)]
        public int sx;

        [ProtoMember(5)]
        public int sy;

        [ProtoMember(6)]
        public int sz;

        public static implicit operator BoundsIntSurrogate(BoundsInt b) =>
            new BoundsIntSurrogate
            {
                px = b.position.x,
                py = b.position.y,
                pz = b.position.z,
                sx = b.size.x,
                sy = b.size.y,
                sz = b.size.z,
            };

        public static implicit operator BoundsInt(BoundsIntSurrogate s) =>
            new BoundsInt(new Vector3Int(s.px, s.py, s.pz), new Vector3Int(s.sx, s.sy, s.sz));
    }

    [ProtoContract]
    internal struct Vector2IntSurrogate
    {
        [ProtoMember(1)]
        public int x;

        [ProtoMember(2)]
        public int y;

        public static implicit operator Vector2IntSurrogate(Vector2Int v) =>
            new Vector2IntSurrogate { x = v.x, y = v.y };

        public static implicit operator Vector2Int(Vector2IntSurrogate s) =>
            new Vector2Int(s.x, s.y);
    }

    [ProtoContract]
    internal struct Vector3IntSurrogate
    {
        [ProtoMember(1)]
        public int x;

        [ProtoMember(2)]
        public int y;

        [ProtoMember(3)]
        public int z;

        public static implicit operator Vector3IntSurrogate(Vector3Int v) =>
            new Vector3IntSurrogate
            {
                x = v.x,
                y = v.y,
                z = v.z,
            };

        public static implicit operator Vector3Int(Vector3IntSurrogate s) =>
            new Vector3Int(s.x, s.y, s.z);
    }

    [ProtoContract]
    internal struct ResolutionSurrogate
    {
        [ProtoMember(1)]
        public int width;

        [ProtoMember(2)]
        public int height;

        [ProtoMember(3)]
        public int refreshRate;

        [Obsolete("Obsolete")]
        public static implicit operator ResolutionSurrogate(Resolution r) =>
            new ResolutionSurrogate
            {
                width = r.width,
                height = r.height,
                refreshRate = r.refreshRate,
            };

        public static implicit operator Resolution(ResolutionSurrogate s)
        {
            Resolution r = new Resolution { width = s.width, height = s.height };
#if !UNITY_2022_2_OR_NEWER
            r.refreshRate = s.refreshRate;
#endif
            return r;
        }
    }

    internal static class ProtobufUnityModel
    {
        static ProtobufUnityModel()
        {
            try
            {
                RuntimeTypeModel model = RuntimeTypeModel.Default;
                model
                    .Add(typeof(Vector2), applyDefaultBehaviour: false)
                    .SetSurrogate(typeof(Vector2Surrogate));
                model
                    .Add(typeof(Vector3), applyDefaultBehaviour: false)
                    .SetSurrogate(typeof(Vector3Surrogate));
                model
                    .Add(typeof(Quaternion), applyDefaultBehaviour: false)
                    .SetSurrogate(typeof(QuaternionSurrogate));
                model
                    .Add(typeof(Color), applyDefaultBehaviour: false)
                    .SetSurrogate(typeof(ColorSurrogate));
                model
                    .Add(typeof(Color32), applyDefaultBehaviour: false)
                    .SetSurrogate(typeof(Color32Surrogate));
                // Common math/geometry types
                model
                    .Add(typeof(Rect), applyDefaultBehaviour: false)
                    .SetSurrogate(typeof(RectSurrogate));
                model
                    .Add(typeof(RectInt), applyDefaultBehaviour: false)
                    .SetSurrogate(typeof(RectIntSurrogate));
                model
                    .Add(typeof(Bounds), applyDefaultBehaviour: false)
                    .SetSurrogate(typeof(BoundsSurrogate));
                model
                    .Add(typeof(BoundsInt), applyDefaultBehaviour: false)
                    .SetSurrogate(typeof(BoundsIntSurrogate));
                model
                    .Add(typeof(Vector2Int), applyDefaultBehaviour: false)
                    .SetSurrogate(typeof(Vector2IntSurrogate));
                model
                    .Add(typeof(Vector3Int), applyDefaultBehaviour: false)
                    .SetSurrogate(typeof(Vector3IntSurrogate));
                model
                    .Add(typeof(Resolution), applyDefaultBehaviour: false)
                    .SetSurrogate(typeof(ResolutionSurrogate));
            }
            catch
            {
                // In restricted environments, model mutation may fail; ignore to keep JSON-only scenarios working.
            }
        }

        internal static void EnsureInitialized() { /* triggers static ctor */
        }
    }
}
