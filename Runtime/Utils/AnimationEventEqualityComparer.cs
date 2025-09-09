namespace WallstopStudios.UnityHelpers.Utils
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public sealed class AnimationEventEqualityComparer
        : EqualityComparer<AnimationEvent>,
            IComparer<AnimationEvent>
    {
        public static readonly AnimationEventEqualityComparer Instance = new();

        private AnimationEventEqualityComparer() { }

        // Returns a shallow copy with equatable values propagated
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

        public override int GetHashCode(AnimationEvent instance)
        {
            return HashCode.Combine(
                instance.time,
                instance.functionName,
                instance.intParameter,
                instance.floatParameter,
                instance.stringParameter,
                instance.objectReferenceParameter,
                instance.messageOptions
            );
        }

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
