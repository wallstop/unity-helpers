namespace UnityHelpers.Core.DataStructure.Adapters
{
    using System;
    using System.Runtime.Serialization;
    using Helper;
    using ProtoBuf;
    using UnityEngine;

    [Serializable]
    [DataContract]
    [ProtoContract]
    public struct KVector2 : IEquatable<KVector2>, IEquatable<Vector2>
    {
        [DataMember]
        [ProtoMember(1)]
        public float x;
        [DataMember]
        [ProtoMember(2)]
        public float y;

        public float Magnitude => (float)Math.Sqrt(x * (double)x + y * (double)y);

        public KVector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public KVector2(Vector2 vector)
        {
            x = vector.x;
            y = vector.y;
        }

        public static implicit operator Vector2(KVector2 vector)
        {
            return new Vector2(vector.x, vector.y);
        }

        public static implicit operator KVector2(Vector2 vector)
        {
            return new KVector2(vector);
        }

        public bool Equals(Vector2 vector)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (x != vector.x)
            {
                return false;
            }

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            return y == vector.y;
        }

        public bool Equals(KVector2 vector)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (x != vector.x)
            {
                return false;
            }

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            return y == vector.y;
        }

        public override bool Equals(object obj)
        {
            return obj is KVector2 vector && Equals(vector);
        }

        public override int GetHashCode()
        {
            return Objects.ValueTypeHashCode(x, y);
        }
    }
}
