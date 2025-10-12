namespace WallstopStudios.UnityHelpers.Tests.Tags
{
    using System.Collections;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Tags;
    using WallstopStudios.UnityHelpers.Tests.Tags.Helpers;

    [TestFixture]
    public sealed class AttributesComponentTests : TagsTestBase
    {
        [SetUp]
        public void SetUp()
        {
            ResetEffectHandleId();
        }

        [UnityTest]
        public IEnumerator ApplyAttributeModificationsWithoutHandleUpdatesBaseValue()
        {
            GameObject entity = CreateTrackedGameObject(
                "Attributes",
                typeof(TestAttributesComponent)
            );
            TestAttributesComponent component = entity.GetComponent<TestAttributesComponent>();

            AttributeModification modification = new()
            {
                attribute = nameof(TestAttributesComponent.health),
                action = ModificationAction.Addition,
                value = 10f,
            };

            component.ApplyAttributeModifications(new[] { modification }, null);
            Assert.AreEqual(110f, component.health.CurrentValue);
            Assert.AreEqual(1, component.notifications.Count);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ForceApplyAttributeModificationsAppliesOncePerHandle()
        {
            GameObject entity = CreateTrackedGameObject(
                "Attributes",
                typeof(TestAttributesComponent)
            );
            TestAttributesComponent component = entity.GetComponent<TestAttributesComponent>();

            AttributeEffect effect = CreateEffect(
                "Buff",
                e =>
                {
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
            EffectHandle handle = EffectHandle.CreateInstance(effect);

            component.ForceApplyAttributeModifications(handle);
            Assert.AreEqual(105f, component.health.CurrentValue);
            Assert.AreEqual(1, component.notifications.Count);

            component.notifications.Clear();
            component.ForceApplyAttributeModifications(handle);
            Assert.IsEmpty(component.notifications);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ForceRemoveAttributeModificationsRestoresValue()
        {
            GameObject entity = CreateTrackedGameObject(
                "Attributes",
                typeof(TestAttributesComponent)
            );
            TestAttributesComponent component = entity.GetComponent<TestAttributesComponent>();

            AttributeEffect effect = CreateEffect(
                "Buff",
                e =>
                {
                    e.modifications.Add(
                        new AttributeModification
                        {
                            attribute = nameof(TestAttributesComponent.health),
                            action = ModificationAction.Multiplication,
                            value = 2f,
                        }
                    );
                }
            );
            EffectHandle handle = EffectHandle.CreateInstance(effect);

            component.ForceApplyAttributeModifications(handle);
            component.notifications.Clear();
            component.ForceRemoveAttributeModifications(handle);

            Assert.AreEqual(100f, component.health.CurrentValue);
            Assert.AreEqual(1, component.notifications.Count);
            Assert.AreEqual(
                nameof(TestAttributesComponent.health),
                component.notifications[0].attribute
            );
            yield return null;
        }

        [UnityTest]
        public IEnumerator ApplyAttributeModificationsWithHandleDelegatesToForce()
        {
            GameObject entity = CreateTrackedGameObject(
                "Attributes",
                typeof(TestAttributesComponent)
            );
            TestAttributesComponent component = entity.GetComponent<TestAttributesComponent>();

            AttributeEffect effect = CreateEffect(
                "Buff",
                e =>
                {
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
            EffectHandle handle = EffectHandle.CreateInstance(effect);

            component.ApplyAttributeModifications(effect.modifications, handle);
            Assert.AreEqual(105f, component.health.CurrentValue);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ForceApplyAttributeModificationsSkipsUnknownAttributes()
        {
            GameObject entity = CreateTrackedGameObject(
                "Attributes",
                typeof(TestAttributesComponent)
            );
            TestAttributesComponent component = entity.GetComponent<TestAttributesComponent>();

            AttributeEffect effect = CreateEffect(
                "Buff",
                e =>
                {
                    e.modifications.Add(
                        new AttributeModification
                        {
                            attribute = "missing",
                            action = ModificationAction.Addition,
                            value = 5f,
                        }
                    );
                }
            );
            EffectHandle handle = EffectHandle.CreateInstance(effect);

            component.ForceApplyAttributeModifications(handle);
            Assert.IsEmpty(component.notifications);
            yield return null;
        }
    }
}
