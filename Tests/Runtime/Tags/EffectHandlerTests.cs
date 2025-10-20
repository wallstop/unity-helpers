namespace WallstopStudios.UnityHelpers.Tests.Tags
{
    using System.Collections;
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Tags;
    using WallstopStudios.UnityHelpers.Tests.Tags.Helpers;

    [TestFixture]
    public sealed class EffectHandlerTests : TagsTestBase
    {
        [SetUp]
        public void SetUp()
        {
            ResetEffectHandleId();
            RecordingCosmeticComponent.ResetCounters();
        }

        [UnityTest]
        public IEnumerator ApplyEffectWithDurationInvokesEventsAndAppliesChanges()
        {
            (
                GameObject entity,
                EffectHandler handler,
                TestAttributesComponent attributes,
                TagHandler tags
            ) = CreateEntity();
            yield return null;

            AttributeEffect effect = CreateEffect(
                "Buff",
                e =>
                {
                    e.effectTags.Add("Buff");
                    e.modifications.Add(
                        new AttributeModification
                        {
                            attribute = nameof(TestAttributesComponent.health),
                            action = ModificationAction.Addition,
                            value = 5f,
                        }
                    );
                }
            );

            int appliedCount = 0;
            handler.OnEffectApplied += _ => ++appliedCount;

            EffectHandle handle = entity.ApplyEffect(effect).Value;
            Assert.AreEqual(1, appliedCount);
            Assert.AreEqual(105f, attributes.health.CurrentValue);
            Assert.IsTrue(tags.HasTag("Buff"));

            handler.RemoveEffect(handle);
            Assert.IsFalse(tags.HasTag("Buff"));
            Assert.AreEqual(100f, attributes.health.CurrentValue);
        }

        [UnityTest]
        public IEnumerator ApplyEffectWithInstantDurationAppliesImmediately()
        {
            (
                GameObject entity,
                EffectHandler handler,
                TestAttributesComponent attributes,
                TagHandler tags
            ) = CreateEntity();
            yield return null;

            AttributeEffect effect = CreateEffect(
                "Instant",
                e =>
                {
                    e.durationType = ModifierDurationType.Instant;
                    e.effectTags.Add("Flash");
                    e.modifications.Add(
                        new AttributeModification
                        {
                            attribute = nameof(TestAttributesComponent.health),
                            action = ModificationAction.Addition,
                            value = 10f,
                        }
                    );
                }
            );

            EffectHandle? handle = entity.ApplyEffect(effect);
            Assert.IsFalse(handle.HasValue);
            Assert.AreEqual(110f, attributes.health.CurrentValue);
            Assert.IsTrue(tags.HasTag("Flash"));
        }

        [UnityTest]
        public IEnumerator RemoveAllEffectsClearsAppliedHandles()
        {
            (
                GameObject entity,
                EffectHandler handler,
                TestAttributesComponent attributes,
                TagHandler tags
            ) = CreateEntity();
            AttributeEffect effectA = CreateEffect(
                "BuffA",
                e =>
                {
                    e.effectTags.Add("BuffA");
                    e.modifications.Add(
                        new AttributeModification
                        {
                            attribute = nameof(TestAttributesComponent.health),
                            action = ModificationAction.Addition,
                            value = 5f,
                        }
                    );
                }
            );
            AttributeEffect effectB = CreateEffect(
                "BuffB",
                e =>
                {
                    e.effectTags.Add("BuffB");
                    e.modifications.Add(
                        new AttributeModification
                        {
                            attribute = nameof(TestAttributesComponent.armor),
                            action = ModificationAction.Addition,
                            value = 10f,
                        }
                    );
                }
            );

            _ = entity.ApplyEffect(effectA);
            _ = entity.ApplyEffect(effectB);

            handler.RemoveAllEffects();
            Assert.AreEqual(100f, attributes.health.CurrentValue);
            Assert.AreEqual(50f, attributes.armor.CurrentValue);
            Assert.IsFalse(tags.HasTag("BuffA"));
            Assert.IsFalse(tags.HasTag("BuffB"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator DurationEffectExpiresAutomatically()
        {
            (
                GameObject entity,
                EffectHandler handler,
                TestAttributesComponent attributes,
                TagHandler tags
            ) = CreateEntity();
            AttributeEffect effect = CreateEffect(
                "Temporary",
                e =>
                {
                    e.duration = 0f;
                    e.effectTags.Add("Temp");
                    e.modifications.Add(
                        new AttributeModification
                        {
                            attribute = nameof(TestAttributesComponent.health),
                            action = ModificationAction.Addition,
                            value = 5f,
                        }
                    );
                }
            );

            int removedCount = 0;
            handler.OnEffectRemoved += _ => ++removedCount;

            _ = entity.ApplyEffect(effect);
            Assert.IsTrue(tags.HasTag("Temp"));

            yield return null;
            yield return null;

            Assert.IsFalse(tags.HasTag("Temp"));
            Assert.AreEqual(100f, attributes.health.CurrentValue);
            Assert.AreEqual(1, removedCount);
        }

        [UnityTest]
        public IEnumerator ApplyEffectAppliesNonInstancedCosmetics()
        {
            (
                GameObject entity,
                EffectHandler handler,
                TestAttributesComponent attributes,
                TagHandler tags
            ) = CreateEntity();
            CosmeticEffectData cosmetic = CreateCosmeticTemplate("Glow");
            AttributeEffect effect = CreateEffect(
                "Cosmetic",
                e =>
                {
                    e.cosmeticEffects.Add(cosmetic);
                }
            );

            EffectHandle handle = entity.ApplyEffect(effect).Value;
            Assert.AreEqual(1, RecordingCosmeticComponent.AppliedCount);

            handler.RemoveEffect(handle);
            Assert.AreEqual(1, RecordingCosmeticComponent.RemovedCount);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ApplyEffectInstantiatesAndDestroysCosmeticInstances()
        {
            (
                GameObject entity,
                EffectHandler handler,
                TestAttributesComponent attributes,
                TagHandler tags
            ) = CreateEntity();
            CosmeticEffectData template = CreateCosmeticTemplate("Aura", requiresInstance: true);
            AttributeEffect effect = CreateEffect(
                "Aura",
                e =>
                {
                    e.cosmeticEffects.Add(template);
                }
            );

            int initialChildCount = entity.transform.childCount;
            EffectHandle handle = entity.ApplyEffect(effect).Value;
            Assert.Greater(entity.transform.childCount, initialChildCount);
            Assert.AreEqual(1, RecordingCosmeticComponent.AppliedCount);

            handler.RemoveEffect(handle);
            yield return null;

            Assert.LessOrEqual(entity.transform.childCount, initialChildCount);
            Assert.AreEqual(1, RecordingCosmeticComponent.RemovedCount);
        }

        [UnityTest]
        public IEnumerator IsEffectActiveReflectsState()
        {
            (
                GameObject entity,
                EffectHandler handler,
                TestAttributesComponent attributes,
                TagHandler tags
            ) = CreateEntity();
            yield return null;

            AttributeEffect effect = CreateEffect("Buff");
            Assert.IsFalse(handler.IsEffectActive(effect));

            EffectHandle handle = handler.ApplyEffect(effect).Value;
            Assert.IsTrue(handler.IsEffectActive(effect));

            handler.RemoveEffect(handle);
            Assert.IsFalse(handler.IsEffectActive(effect));
        }

        [UnityTest]
        public IEnumerator GetEffectStackCountSupportsMultipleHandles()
        {
            (
                GameObject entity,
                EffectHandler handler,
                TestAttributesComponent attributes,
                TagHandler tags
            ) = CreateEntity();
            yield return null;

            AttributeEffect effect = CreateEffect(
                "Stacking",
                e =>
                {
                    e.durationType = ModifierDurationType.Infinite;
                }
            );

            EffectHandle first = handler.ApplyEffect(effect).Value;
            EffectHandle second = handler.ApplyEffect(effect).Value;

            Assert.AreEqual(2, handler.GetEffectStackCount(effect));

            handler.RemoveEffect(first);
            Assert.AreEqual(1, handler.GetEffectStackCount(effect));

            handler.RemoveEffect(second);
            Assert.AreEqual(0, handler.GetEffectStackCount(effect));
        }

        [UnityTest]
        public IEnumerator GetActiveEffectsPopulatesBuffer()
        {
            (
                GameObject entity,
                EffectHandler handler,
                TestAttributesComponent attributes,
                TagHandler tags
            ) = CreateEntity();
            yield return null;

            AttributeEffect effect = CreateEffect(
                "Active",
                e =>
                {
                    e.durationType = ModifierDurationType.Infinite;
                }
            );

            EffectHandle first = handler.ApplyEffect(effect).Value;
            EffectHandle second = handler.ApplyEffect(effect).Value;

            List<EffectHandle> buffer = new();
            handler.GetActiveEffects(buffer);
            CollectionAssert.AreEquivalent(new[] { first, second }, buffer);

            handler.RemoveEffect(first);
            buffer.Clear();
            handler.GetActiveEffects(buffer);
            CollectionAssert.AreEqual(new[] { second }, buffer);

            handler.RemoveEffect(second);
        }

        [UnityTest]
        public IEnumerator TryGetRemainingDurationReportsTime()
        {
            (
                GameObject entity,
                EffectHandler handler,
                TestAttributesComponent attributes,
                TagHandler tags
            ) = CreateEntity();
            yield return null;

            AttributeEffect effect = CreateEffect(
                "Timed",
                e =>
                {
                    e.duration = 0.5f;
                }
            );

            EffectHandle handle = handler.ApplyEffect(effect).Value;
            Assert.IsTrue(handler.TryGetRemainingDuration(handle, out float remaining));
            Assert.Greater(remaining, 0f);
            Assert.LessOrEqual(remaining, effect.duration);

            handler.RemoveEffect(handle);
            Assert.IsFalse(handler.TryGetRemainingDuration(handle, out float afterRemoval));
            Assert.AreEqual(0f, afterRemoval);
        }

        [UnityTest]
        public IEnumerator EnsureHandleRefreshesDurationWhenRequested()
        {
            (
                GameObject entity,
                EffectHandler handler,
                TestAttributesComponent attributes,
                TagHandler tags
            ) = CreateEntity();
            yield return null;

            AttributeEffect effect = CreateEffect(
                "Refreshable",
                e =>
                {
                    e.duration = 0.2f;
                    e.resetDurationOnReapplication = true;
                }
            );

            EffectHandle handle = handler.ApplyEffect(effect).Value;
            Assert.IsTrue(handler.TryGetRemainingDuration(handle, out float initialRemaining));
            yield return null;
            Assert.IsTrue(handler.TryGetRemainingDuration(handle, out float beforeRefresh));
            Assert.Less(beforeRefresh, initialRemaining);

            EffectHandle? ensured = handler.EnsureHandle(effect);
            Assert.IsTrue(ensured.HasValue);
            Assert.AreEqual(handle, ensured.Value);
            Assert.IsTrue(handler.TryGetRemainingDuration(handle, out float afterRefresh));
            Assert.Greater(afterRefresh, beforeRefresh);

            yield return null;
            Assert.IsTrue(handler.TryGetRemainingDuration(handle, out float beforeNoRefresh));
            EffectHandle? ensuredNoRefresh = handler.EnsureHandle(effect, refreshDuration: false);
            Assert.IsTrue(ensuredNoRefresh.HasValue);
            Assert.AreEqual(handle, ensuredNoRefresh.Value);
            Assert.IsTrue(handler.TryGetRemainingDuration(handle, out float afterNoRefresh));
            Assert.LessOrEqual(afterNoRefresh, beforeNoRefresh);

            handler.RemoveEffect(handle);
        }

        [UnityTest]
        public IEnumerator RefreshEffectHonorsReapplicationPolicy()
        {
            (
                GameObject entity,
                EffectHandler handler,
                TestAttributesComponent attributes,
                TagHandler tags
            ) = CreateEntity();
            yield return null;

            AttributeEffect effect = CreateEffect(
                "Policy",
                e =>
                {
                    e.duration = 0.3f;
                    e.resetDurationOnReapplication = false;
                }
            );

            EffectHandle handle = handler.ApplyEffect(effect).Value;
            yield return null;
            Assert.IsTrue(handler.TryGetRemainingDuration(handle, out float beforeRefresh));

            Assert.IsFalse(handler.RefreshEffect(handle));
            Assert.IsTrue(handler.RefreshEffect(handle, ignoreReapplicationPolicy: true));
            Assert.IsTrue(handler.TryGetRemainingDuration(handle, out float afterRefresh));
            Assert.Greater(afterRefresh, beforeRefresh);

            handler.RemoveEffect(handle);
        }

        private (
            GameObject entity,
            EffectHandler handler,
            TestAttributesComponent attributes,
            TagHandler tags
        ) CreateEntity()
        {
            GameObject entity = CreateTrackedGameObject("Entity", typeof(TestAttributesComponent));
            return (
                entity,
                entity.GetComponent<EffectHandler>(),
                entity.GetComponent<TestAttributesComponent>(),
                entity.GetComponent<TagHandler>()
            );
        }

        private CosmeticEffectData CreateCosmeticTemplate(
            string name,
            bool requiresInstance = false
        )
        {
            GameObject template = CreateTrackedGameObject(name, typeof(CosmeticEffectData));
            RecordingCosmeticComponent component =
                template.AddComponent<RecordingCosmeticComponent>();
            component.requireInstance = requiresInstance;
            component.cleansSelf = false;
            return template.GetComponent<CosmeticEffectData>();
        }
    }
}
