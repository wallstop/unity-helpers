namespace WallstopStudios.UnityHelpers.Tests.Attributes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [TestFixture]
    public sealed class AnimationEventHelpersTests
    {
        [Test]
        public void TypesToMethodsIncludesDeclaringTypeAndFiltersEmptyEntries()
        {
            IReadOnlyDictionary<Type, IReadOnlyList<MethodInfo>> mapping =
                AnimationEventHelpers.TypesToMethods;

            Assert.IsTrue(
                mapping.TryGetValue(
                    typeof(AnimationEventSource),
                    out IReadOnlyList<MethodInfo> methods
                ),
                "Expected AnimationEventSource to be registered."
            );
            CollectionAssert.Contains(
                methods,
                typeof(AnimationEventSource).GetMethod(
                    nameof(AnimationEventSource.SimpleEvent),
                    BindingFlags.Instance | BindingFlags.NonPublic
                )
            );

            Assert.IsFalse(mapping.ContainsKey(typeof(AnimationEventPlainBehaviour)));
        }

        [Test]
        public void IgnoreDerivedPreventsInheritedHandlersFromRegistering()
        {
            IReadOnlyDictionary<Type, IReadOnlyList<MethodInfo>> mapping =
                AnimationEventHelpers.TypesToMethods;

            bool hasDerivedIgnore = mapping.TryGetValue(
                typeof(AnimationEventDerivedIgnore),
                out IReadOnlyList<MethodInfo> ignoreMethods
            );
            if (hasDerivedIgnore)
            {
                System.Console.WriteLine(
                    $"[DEBUG_LOG] AnimationEventDerivedIgnore has {ignoreMethods.Count} methods:"
                );
                foreach (var method in ignoreMethods)
                {
                    System.Console.WriteLine(
                        $"[DEBUG_LOG]   - {method.Name} (DeclaringType: {method.DeclaringType.Name})"
                    );
                }
            }

            Assert.IsFalse(
                hasDerivedIgnore,
                "Derived type should not register when base handler ignores derived."
            );

            Assert.IsTrue(
                mapping.TryGetValue(
                    typeof(AnimationEventDerivedAllowed),
                    out IReadOnlyList<MethodInfo> methods
                ),
                "Derived type should register when it declares its own handlers."
            );
            CollectionAssert.Contains(
                methods,
                typeof(AnimationEventDerivedAllowed).GetMethod(
                    nameof(AnimationEventDerivedAllowed.DerivedOnly),
                    BindingFlags.Instance | BindingFlags.NonPublic
                )
            );
            CollectionAssert.Contains(
                methods,
                typeof(AnimationEventSource).GetMethod(
                    nameof(AnimationEventSource.AllowDerived),
                    BindingFlags.Instance | BindingFlags.NonPublic
                )
            );
        }

        [Test]
        public void GetPossibleAnimatorEventsReturnsOnlySupportedSignaturesFromDeclaringType()
        {
            List<MethodInfo> methods = typeof(AnimationEventSignatureHost)
                .GetPossibleAnimatorEventsForType()
                .Where(method => method.DeclaringType == typeof(AnimationEventSignatureHost))
                .ToList();

            string[] expected =
            {
                nameof(AnimationEventSignatureHost.NoArgs),
                nameof(AnimationEventSignatureHost.WithEnum),
                nameof(AnimationEventSignatureHost.WithFloat),
                nameof(AnimationEventSignatureHost.WithInt),
                nameof(AnimationEventSignatureHost.WithString),
                nameof(AnimationEventSignatureHost.WithUnityObject),
            };

            CollectionAssert.AreEquivalent(expected, methods.Select(method => method.Name));
        }
    }

    internal enum AnimationEventSignal
    {
        Ready,
        Done,
    }

    internal sealed class AnimationEventPlainBehaviour : MonoBehaviour { }

    internal class AnimationEventSource : MonoBehaviour
    {
        [AnimationEvent]
        protected internal void SimpleEvent() { }

        [AnimationEvent(ignoreDerived = false)]
        protected internal void AllowDerived() { }

        [AnimationEvent]
        private int InvalidReturn() => 0;

        [AnimationEvent]
        private void InvalidParameter(Vector3 _) { }
    }

    internal sealed class AnimationEventDerivedIgnore : AnimationEventSource { }

    internal sealed class AnimationEventDerivedAllowed : AnimationEventSource
    {
        [AnimationEvent(ignoreDerived = false)]
        internal void DerivedOnly() { }
    }

    internal sealed class AnimationEventSignatureHost : MonoBehaviour
    {
        [AnimationEvent]
        public void NoArgs() { }

        [AnimationEvent]
        public void WithInt(int value) { }

        [AnimationEvent]
        public void WithEnum(AnimationEventSignal signal) { }

        [AnimationEvent]
        public void WithFloat(float value) { }

        [AnimationEvent]
        public void WithString(string text) { }

        [AnimationEvent]
        public void WithUnityObject(UnityEngine.Object target) { }

        [AnimationEvent]
        public void TwoParameters(int value, string text) { }

        [AnimationEvent]
        public int NonVoidReturn() => 0;
    }
}
