namespace WallstopStudios.UnityHelpers.Tests.Tags
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Tags;
    using WallstopStudios.UnityHelpers.Tests.Tags.Helpers;

    [TestFixture]
    public sealed class AttributeUtilitiesTests : TagsTestBase
    {
        [SetUp]
        public void SetUp()
        {
            ResetEffectHandleId();
            ClearAttributeUtilitiesCaches();
        }

        [TearDown]
        public void TearDown()
        {
            ClearAttributeUtilitiesCaches();
        }

        [Test]
        public void GetAllAttributeNamesIncludesDerivedComponentFields()
        {
            string[] allNames = AttributeUtilities.GetAllAttributeNames();
            CollectionAssert.Contains(allNames, nameof(TestAttributesComponent.health));
            CollectionAssert.Contains(allNames, nameof(TestAttributesComponent.armor));
        }

        [Test]
        public void GetAttributeFieldsCachesPerType()
        {
            Dictionary<string, FieldInfo> first = AttributeUtilities.GetAttributeFields(
                typeof(TestAttributesComponent)
            );
            Dictionary<string, FieldInfo> second = AttributeUtilities.GetAttributeFields(
                typeof(TestAttributesComponent)
            );
            Assert.IsTrue(ReferenceEquals(first, second));
            Assert.IsTrue(first.ContainsKey(nameof(TestAttributesComponent.health)));
        }

        [Test]
        public void HasTagReturnsFalseForNullTargets()
        {
            Object target = null;
            Assert.IsFalse(target.HasTag("Buff"));
        }

        [UnityTest]
        public IEnumerator HasTagReturnsTrueWhenHandlerContainsTag()
        {
            GameObject entity = CreateTrackedGameObject("Tagged", typeof(TagHandler));
            yield return null;
            TagHandler handler = entity.GetComponent<TagHandler>();
            handler.ApplyTag("Buff");
            Assert.IsTrue(entity.HasTag("Buff"));
            Assert.IsFalse(entity.HasTag("Missing"));
        }

        [UnityTest]
        public IEnumerator HasAnyTagChecksEnumerableAndReadOnlyList()
        {
            GameObject entity = CreateTrackedGameObject("Tagged", typeof(TagHandler));
            yield return null;
            TagHandler handler = entity.GetComponent<TagHandler>();
            handler.ApplyTag("Buff");

            Assert.IsTrue(entity.HasAnyTag(new[] { "Missing", "Buff" }));
            Assert.IsFalse(entity.HasAnyTag(new[] { "Missing" }));

            IReadOnlyList<string> list = new List<string> { "Other", "Buff" };
            Assert.IsTrue(entity.HasAnyTag(list));
        }

        [UnityTest]
        public IEnumerator ApplyEffectAddsAttributesCosmeticsAndTags()
        {
            GameObject entity = CreateTrackedGameObject("Entity", typeof(TestAttributesComponent));
            yield return null;
            TestAttributesComponent attributes = entity.GetComponent<TestAttributesComponent>();
            TagHandler tagHandler = entity.GetComponent<TagHandler>();

            AttributeEffect effect = CreateEffect(
                "Might",
                e =>
                {
                    e.effectTags.Add("Buff");
                    e.modifications.Add(
                        new AttributeModification
                        {
                            attribute = nameof(TestAttributesComponent.health),
                            action = ModificationAction.Addition,
                            value = 15f,
                        }
                    );
                }
            );

            EffectHandle? handle = entity.ApplyEffect(effect);
            Assert.IsTrue(handle.HasValue);
            Assert.AreEqual(115f, attributes.health.CurrentValue);
            Assert.IsTrue(tagHandler.HasTag("Buff"));
            Assert.AreEqual(1, attributes.notifications.Count);
        }

        [UnityTest]
        public IEnumerator ApplyEffectReusesHandleWhenResetDisabled()
        {
            GameObject entity = CreateTrackedGameObject("Entity", typeof(TestAttributesComponent));
            yield return null;
            TestAttributesComponent attributes = entity.GetComponent<TestAttributesComponent>();

            AttributeEffect effect = CreateEffect(
                "Might",
                e =>
                {
                    e.resetDurationOnReapplication = false;
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

            EffectHandle first = entity.ApplyEffect(effect).Value;
            attributes.notifications.Clear();
            EffectHandle second = entity.ApplyEffect(effect).Value;
            Assert.AreEqual(first, second);
            Assert.IsEmpty(attributes.notifications);
        }

        [UnityTest]
        public IEnumerator ApplyEffectsNoAllocPopulatesHandleList()
        {
            GameObject entity = CreateTrackedGameObject("Entity", typeof(TestAttributesComponent));
            yield return null;
            List<AttributeEffect> effects = new()
            {
                CreateEffect(
                    "BuffA",
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
                ),
                CreateEffect(
                    "BuffB",
                    e =>
                    {
                        e.modifications.Add(
                            new AttributeModification
                            {
                                attribute = nameof(TestAttributesComponent.armor),
                                action = ModificationAction.Addition,
                                value = 10f,
                            }
                        );
                    }
                ),
            };

            List<EffectHandle> handles = new();
            entity.ApplyEffectsNoAlloc(effects, handles);
            Assert.AreEqual(2, handles.Count);
        }

        [UnityTest]
        public IEnumerator ApplyEffectsReturnsHandles()
        {
            GameObject entity = CreateTrackedGameObject("Entity", typeof(TestAttributesComponent));
            yield return null;
            List<AttributeEffect> effects = new()
            {
                CreateEffect(
                    "BuffA",
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
                ),
                CreateEffect(
                    "BuffB",
                    e =>
                    {
                        e.modifications.Add(
                            new AttributeModification
                            {
                                attribute = nameof(TestAttributesComponent.armor),
                                action = ModificationAction.Addition,
                                value = 10f,
                            }
                        );
                    }
                ),
            };

            List<EffectHandle> handles = entity.ApplyEffects(effects);
            Assert.AreEqual(2, handles.Count);
        }

        [UnityTest]
        public IEnumerator ApplyEffectsNoAllocEnumerableAppliesAll()
        {
            GameObject entity = CreateTrackedGameObject("Entity", typeof(TestAttributesComponent));
            yield return null;
            IEnumerable<AttributeEffect> effects = new[]
            {
                CreateEffect(
                    "BuffA",
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
                ),
                CreateEffect(
                    "BuffB",
                    e =>
                    {
                        e.modifications.Add(
                            new AttributeModification
                            {
                                attribute = nameof(TestAttributesComponent.armor),
                                action = ModificationAction.Addition,
                                value = 10f,
                            }
                        );
                    }
                ),
            };

            entity.ApplyEffectsNoAlloc(effects);
            TestAttributesComponent attributes = entity.GetComponent<TestAttributesComponent>();
            Assert.AreEqual(105f, attributes.health.CurrentValue);
            Assert.AreEqual(60f, attributes.armor.CurrentValue);
        }

        [UnityTest]
        public IEnumerator RemoveEffectRestoresAttributesAndTags()
        {
            GameObject entity = CreateTrackedGameObject("Entity", typeof(TestAttributesComponent));
            yield return null;
            AttributeEffect effect = CreateEffect(
                "Might",
                e =>
                {
                    e.effectTags.Add("Buff");
                    e.modifications.Add(
                        new AttributeModification
                        {
                            attribute = nameof(TestAttributesComponent.health),
                            action = ModificationAction.Addition,
                            value = 20f,
                        }
                    );
                }
            );

            EffectHandle handle = entity.ApplyEffect(effect).Value;
            entity.RemoveEffect(handle);

            TestAttributesComponent attributes = entity.GetComponent<TestAttributesComponent>();
            Assert.AreEqual(100f, attributes.health.CurrentValue);
            Assert.IsFalse(entity.HasTag("Buff"));
        }

        [UnityTest]
        public IEnumerator RemoveEffectsRemovesAllHandles()
        {
            GameObject entity = CreateTrackedGameObject("Entity", typeof(TestAttributesComponent));
            yield return null;
            List<AttributeEffect> effects = new()
            {
                CreateEffect(
                    "BuffA",
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
                ),
                CreateEffect(
                    "BuffB",
                    e =>
                    {
                        e.modifications.Add(
                            new AttributeModification
                            {
                                attribute = nameof(TestAttributesComponent.armor),
                                action = ModificationAction.Addition,
                                value = 10f,
                            }
                        );
                    }
                ),
            };

            List<EffectHandle> handles = entity.ApplyEffects(effects);
            entity.RemoveEffects(handles);

            TestAttributesComponent attributes = entity.GetComponent<TestAttributesComponent>();
            Assert.AreEqual(100f, attributes.health.CurrentValue);
            Assert.AreEqual(50f, attributes.armor.CurrentValue);
        }

        [UnityTest]
        public IEnumerator RemoveAllEffectsClearsState()
        {
            GameObject entity = CreateTrackedGameObject("Entity", typeof(TestAttributesComponent));
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

            _ = entity.ApplyEffect(effect);
            entity.RemoveAllEffects();

            TestAttributesComponent attributes = entity.GetComponent<TestAttributesComponent>();
            Assert.AreEqual(100f, attributes.health.CurrentValue);
            Assert.IsFalse(entity.HasTag("Buff"));
        }

        [UnityTest]
        public IEnumerator ApplyEffectsNoAllocSkipsNullTarget()
        {
            Object target = null;
            List<AttributeEffect> effects = new() { CreateEffect("Buff") };
            List<EffectHandle> handles = new();

            target.ApplyEffectsNoAlloc(effects, handles);
            Assert.IsEmpty(handles);
            yield break;
        }

        [UnityTest]
        public IEnumerator ApplyEffectReturnsNullWhenTargetNull()
        {
            Object target = null;
            AttributeEffect effect = CreateEffect("Buff");

            EffectHandle? handle = target.ApplyEffect(effect);
            Assert.IsFalse(handle.HasValue);
            yield break;
        }

        [UnityTest]
        public IEnumerator RemoveEffectsWithEmptyListNoops()
        {
            GameObject entity = CreateTrackedGameObject("Entity", typeof(TestAttributesComponent));
            yield return null;
            entity.RemoveEffects(new List<EffectHandle>());
            Assert.Pass();
        }
    }
}
