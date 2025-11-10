namespace WallstopStudios.UnityHelpers.Editor.WButton
{
#if UNITY_EDITOR
    using System;
    using UnityEngine;

    internal static class WButtonValueUtility
    {
        internal static object CloneValue(object value)
        {
            if (value is Array array)
            {
                return array.Clone();
            }

            AnimationCurve curve = value as AnimationCurve;
            if (curve != null)
            {
                AnimationCurve clone = new(curve.keys)
                {
                    preWrapMode = curve.preWrapMode,
                    postWrapMode = curve.postWrapMode,
                };
                return clone;
            }

            return value;
        }

        internal static bool ValuesEqual(object left, object right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null)
            {
                return false;
            }

            if (left is Array leftArray && right is Array rightArray)
            {
                if (leftArray.Length != rightArray.Length)
                {
                    return false;
                }

                for (int index = 0; index < leftArray.Length; index++)
                {
                    object leftElement = leftArray.GetValue(index);
                    object rightElement = rightArray.GetValue(index);
                    if (!ValuesEqual(leftElement, rightElement))
                    {
                        return false;
                    }
                }

                return true;
            }

            return left.Equals(right);
        }
    }
#endif
}
