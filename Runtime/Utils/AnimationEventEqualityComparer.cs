namespace WallstopStudios.UnityHelpers.Utils
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;

    /// <summary>
    /// Provides value and ordering comparisons for <see cref="AnimationEvent"/> instances and
    /// offers helpers for duplicating them without mutating the original event.
    /// </summary>
    /// <remarks>
    /// Unity does not expose an <see cref="EqualityComparer{T}"/> implementation for
    /// <see cref="AnimationEvent"/>. This comparer inspects the fields Unity uses for dispatching
    /// events (time, function name, parameters, and message options) and treats two events as equal
    /// only when all of those values match.
    /// </remarks>
    public sealed class AnimationEventEqualityComparer
        : EqualityComparer<AnimationEvent>,
            IComparer<AnimationEvent>
    {
        /// <summary>
        /// Gets a globally shared comparer instance to avoid unnecessary allocations.
        /// </summary>
        public static readonly AnimationEventEqualityComparer Instance = new();

        private AnimationEventEqualityComparer() { }

        /// <summary>
        /// Creates a shallow copy of the supplied <paramref name="instance"/> by cloning its
        /// equatable values.
        /// </summary>
        /// <param name="instance">Event instance to duplicate, or <c>null</c> to skip copying.</param>
        /// <returns>A new <see cref="AnimationEvent"/> carrying the same values, or <c>null</c>.</returns>
        public AnimationEvent Copy(AnimationEvent instance)
        {
            if (instance == null)
            {
                return null;
            }

            AnimationEvent copy = new();
            CopyInto(copy, instance);
            return copy;
        }

        /// <summary>
        /// Copies all equatable values from <paramref name="parameters"/> into
        /// <paramref name="into"/> without creating a new <see cref="AnimationEvent"/>.
        /// </summary>
        /// <param name="into">Destination instance that receives the values.</param>
        /// <param name="parameters">Source instance that provides the values.</param>
        public void CopyInto(AnimationEvent into, AnimationEvent parameters)
        {
            if (into == null || parameters == null)
            {
                return;
            }

            into.time = parameters.time;
            into.functionName = parameters.functionName;
            into.intParameter = parameters.intParameter;
            into.floatParameter = parameters.floatParameter;
            into.stringParameter = parameters.stringParameter;
            into.objectReferenceParameter = parameters.objectReferenceParameter;
            into.messageOptions = parameters.messageOptions;
        }

        /// <inheritdoc />
        public override bool Equals(AnimationEvent lhs, AnimationEvent rhs)
        {
            if (ReferenceEquals(lhs, rhs))
            {
                return true;
            }

            if (lhs == null || rhs == null)
            {
                return false;
            }

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (lhs.time != rhs.time)
            {
                return false;
            }

            if (lhs.functionName != rhs.functionName)
            {
                return false;
            }

            if (lhs.intParameter != rhs.intParameter)
            {
                return false;
            }

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (lhs.floatParameter != rhs.floatParameter)
            {
                return false;
            }

            if (lhs.stringParameter != rhs.stringParameter)
            {
                return false;
            }

            if (lhs.objectReferenceParameter != rhs.objectReferenceParameter)
            {
                return false;
            }

            if (lhs.messageOptions != rhs.messageOptions)
            {
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public override int GetHashCode(AnimationEvent instance)
        {
            return Objects.HashCode(
                instance.time,
                instance.functionName,
                instance.intParameter,
                instance.floatParameter,
                instance.stringParameter,
                instance.objectReferenceParameter,
                instance.messageOptions
            );
        }

        /// <summary>
        /// Orders two <see cref="AnimationEvent"/> instances using their firing time followed by the
        /// textual and numeric parameters.
        /// </summary>
        /// <param name="lhs">First event to compare.</param>
        /// <param name="rhs">Second event to compare.</param>
        /// <returns>
        /// An integer less than zero when <paramref name="lhs"/> should come before
        /// <paramref name="rhs"/>, zero when they are equivalent, or greater than zero otherwise.
        /// </returns>
        public int Compare(AnimationEvent lhs, AnimationEvent rhs)
        {
            if (ReferenceEquals(lhs, rhs))
            {
                return 0;
            }

            if (ReferenceEquals(null, rhs))
            {
                return 1;
            }

            if (ReferenceEquals(null, lhs))
            {
                return -1;
            }

            int timeComparison = lhs.time.CompareTo(rhs.time);
            if (timeComparison != 0)
            {
                return timeComparison;
            }

            int functionNameComparison = string.Compare(
                lhs.functionName,
                rhs.functionName,
                StringComparison.Ordinal
            );
            if (functionNameComparison != 0)
            {
                return functionNameComparison;
            }

            int intParameterComparison = lhs.intParameter.CompareTo(rhs.intParameter);
            if (intParameterComparison != 0)
            {
                return intParameterComparison;
            }

            int stringParameterComparison = string.Compare(
                lhs.stringParameter,
                rhs.stringParameter,
                StringComparison.Ordinal
            );
            if (stringParameterComparison != 0)
            {
                return stringParameterComparison;
            }

            return lhs.floatParameter.CompareTo(rhs.floatParameter);
        }
    }
}
