namespace UnityHelpers.Utils
{
    using Core.Extension;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.Serialization;
    using UnityEngine;

    /// <summary>
    ///     Maps & manages an enum parameter to bool Animator parameters.
    /// </summary>
    /// <typeparam name="T">Specific Enum being managed.</typeparam>
    [DataContract]
    public sealed class AnimatorEnumStateMachine<T>
        where T : struct, IConvertible, IComparable,
        IFormattable // This is as close as we can get to saying where T : Enum (https://stackoverflow.com/questions/79126/create-generic-method-constraining-t-to-an-enum)
    {
        private static readonly T[] Values = Enum.GetValues(typeof(T)).OfType<T>().ToArray();

        [IgnoreDataMember] private readonly HashSet<string> _availableBools = new HashSet<string>();

        [IgnoreDataMember] public readonly Animator Animator;

        [IgnoreDataMember] private T _value;

        [DataMember]
        public T Value
        {
            get { return _value; }
            set
            {
                foreach (T possibleValue in Values)
                {
                    string valueName = possibleValue.ToString(CultureInfo.InvariantCulture);
                    if (_availableBools.Contains(valueName))
                    {
                        Animator.SetBool(valueName, Equals(value, possibleValue));
                    }
                }

                _value = value;
            }
        }

        [DataMember]
        private string Type => typeof(T).Name;

        public AnimatorEnumStateMachine(Animator animator, T defaultValue = default(T))
        {
            if (animator == null)
            {
                throw new ArgumentNullException(nameof(animator));
            }

            Animator = animator;

            foreach (AnimatorControllerParameter parameter in Animator.parameters)
            {
                switch (parameter.type)
                {
                    case AnimatorControllerParameterType.Bool:
                    case AnimatorControllerParameterType.Trigger:
                        _availableBools.Add(parameter.name);
                        continue;
                    default:
                        continue;
                }
            }

            _value = defaultValue;
        }

        public override string ToString()
        {
            return this.ToJson();
        }
    }
}
