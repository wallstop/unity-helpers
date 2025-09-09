namespace WallstopStudios.UnityHelpers.Core.Math
{
    using System;
    using System.Runtime.Serialization;

    [DataContract]
    [Serializable]
    public readonly struct Parabola
    {
        public readonly float Length;

        public readonly float A;
        public readonly float B;

        /// <summary>
        /// Creates a Parabola that reaches a max height and has a specified length.
        /// </summary>
        /// <param name="max">Max height of parabola.</param>
        /// <param name="length">Length of parabola (between x intercepts).</param>
        public Parabola(float max, float length)
        {
            if (length <= 0)
            {
                throw new ArgumentException(
                    $"Expected a length greater than 0, but found: {length:0.00}."
                );
            }

            Length = length;

            A = -4 * max / (length * length);
            B = -A * length;
        }

        public bool ValueAt(float x, out float y)
        {
            if (x < 0 || Length < x)
            {
                y = float.NaN;
                return false;
            }

            y = A * (x * x) + B * x;
            return true;
        }
    }
}
