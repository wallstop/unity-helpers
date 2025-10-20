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
    public sealed class TagHandlerTests : TagsTestBase
    {
        [SetUp]
        public void SetUp()
        {
            ResetEffectHandleId();
        }

        [UnityTest]
        public IEnumerator ApplyAndRemoveTagRaisesEvents()
        {
            GameObject entity = CreateTrackedGameObject("Tags", typeof(TagHandler));
            TagHandler handler = entity.GetComponent<TagHandler>();

            List<string> added = new();
            List<(string tag, uint count)> changed = new();
            List<string> removed = new();

            handler.OnTagAdded += tag => added.Add(tag);
            handler.OnTagCountChanged += (tag, count) => changed.Add((tag, count));
            handler.OnTagRemoved += tag => removed.Add(tag);

            handler.ApplyTag("Buff");
            handler.ApplyTag("Buff");
            handler.ApplyTag("Buff");
            handler.RemoveTag("Buff");

            Assert.AreEqual(new[] { "Buff" }, added);
            Assert.AreEqual(2, changed.Count);
            Assert.AreEqual(("Buff", 2U), changed[0]);
            Assert.AreEqual(("Buff", 3U), changed[1]);
            Assert.AreEqual(new[] { "Buff" }, removed);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RemoveTagAllInstancesClearsWithEvents()
        {
            GameObject entity = CreateTrackedGameObject("Tags", typeof(TagHandler));
            TagHandler handler = entity.GetComponent<TagHandler>();

            int removedCount = 0;
            handler.OnTagRemoved += _ => ++removedCount;

            handler.ApplyTag("Buff");
            handler.RemoveTag("Buff");

            Assert.IsFalse(handler.HasTag("Buff"));
            Assert.AreEqual(1, removedCount);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ForceApplyTagsIgnoresDuplicateHandles()
        {
            GameObject entity = CreateTrackedGameObject("Tags", typeof(TagHandler));
            TagHandler handler = entity.GetComponent<TagHandler>();

            AttributeEffect effect = CreateEffect(
                "Buff",
                e =>
                {
                    e.effectTags.Add("Buff");
                }
            );
            EffectHandle handle = EffectHandle.CreateInstance(effect);

            int addedCount = 0;
            handler.OnTagAdded += _ => ++addedCount;
            handler.ForceApplyTags(handle);
            handler.ForceApplyTags(handle);

            Assert.AreEqual(1, addedCount);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ForceRemoveTagsReturnsExpectedResult()
        {
            GameObject entity = CreateTrackedGameObject("Tags", typeof(TagHandler));
            TagHandler handler = entity.GetComponent<TagHandler>();

            AttributeEffect effect = CreateEffect(
                "Buff",
                e =>
                {
                    e.effectTags.Add("Buff");
                }
            );
            EffectHandle handle = EffectHandle.CreateInstance(effect);

            handler.ForceApplyTags(handle);
            Assert.IsTrue(handler.ForceRemoveTags(handle));
            Assert.IsFalse(handler.ForceRemoveTags(handle));
            yield return null;
        }

        [UnityTest]
        public IEnumerator HasAllTagsAndHasNoneTagSupport()
        {
            GameObject entity = CreateTrackedGameObject("Tags", typeof(TagHandler));
            TagHandler handler = entity.GetComponent<TagHandler>();

            handler.ApplyTag("Buff");
            handler.ApplyTag("Shield");

            Assert.IsTrue(handler.HasAllTags(new[] { "Buff" }));
            List<string> required = new() { "Buff", "Shield" };
            Assert.IsTrue(handler.HasAllTags(required));
            Assert.IsFalse(handler.HasAllTags(new[] { "Buff", "Missing" }));

            List<string> none = new() { "Missing", "Other" };
            Assert.IsTrue(handler.HasNoneOfTags(none));
            none[1] = "Buff";
            Assert.IsFalse(handler.HasNoneOfTags(none));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TryGetTagCountAndActiveTagsPopulateBuffer()
        {
            GameObject entity = CreateTrackedGameObject("Tags", typeof(TagHandler));
            TagHandler handler = entity.GetComponent<TagHandler>();

            handler.ApplyTag("Buff");
            handler.ApplyTag("Buff");
            handler.ApplyTag("Shield");

            Assert.IsTrue(handler.TryGetTagCount("Buff", out int buffCount));
            Assert.AreEqual(2, buffCount);
            Assert.IsFalse(handler.TryGetTagCount("Missing", out int missingCount));
            Assert.AreEqual(0, missingCount);

            List<string> active = new();
            handler.GetActiveTags(active);
            CollectionAssert.AreEquivalent(new[] { "Buff", "Shield" }, active);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GetHandlesWithTagTracksHandleLifecycle()
        {
            GameObject entity = CreateTrackedGameObject("Entity", typeof(TestAttributesComponent));
            TagHandler handler = entity.GetComponent<TagHandler>();
            EffectHandler effectHandler = entity.GetComponent<EffectHandler>();

            AttributeEffect effect = CreateEffect(
                "Buff",
                e =>
                {
                    e.effectTags.Add("Buff");
                }
            );
            EffectHandle handle = effectHandler.ApplyEffect(effect).Value;

            List<EffectHandle> handles = new();
            Assert.AreNotEqual(0, handler.GetHandlesWithTag("Buff", handles).Count);
            Assert.AreEqual(1, handles.Count);
            Assert.AreEqual(handle, handles[0]);

            handles.Clear();
            handler.RemoveTag("Buff");
            Assert.AreEqual(0, handler.GetHandlesWithTag("Buff", handles).Count);

            handles.Clear();
            effectHandler.RemoveEffect(handle);
            Assert.AreEqual(0, handler.GetHandlesWithTag("Buff", handles).Count);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GetActiveTagsReturnsNewBufferWhenNeeded()
        {
            GameObject entity = CreateTrackedGameObject("Tags", typeof(TagHandler));
            TagHandler handler = entity.GetComponent<TagHandler>();

            handler.ApplyTag("Buff");
            handler.ApplyTag("Shield");

            List<string> firstCall = handler.GetActiveTags();
            Assert.IsNotNull(firstCall);
            CollectionAssert.AreEquivalent(new[] { "Buff", "Shield" }, firstCall);

            List<string> reusable = new() { "Sentinel" };
            List<string> secondCall = handler.GetActiveTags(reusable);
            Assert.AreSame(reusable, secondCall);
            CollectionAssert.AreEquivalent(new[] { "Buff", "Shield" }, secondCall);
            Assert.IsFalse(secondCall.Contains("Sentinel"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator RemoveTagReturnsContributingHandles()
        {
            GameObject entity = CreateTrackedGameObject("Entity", typeof(TestAttributesComponent));
            yield return null;
            TagHandler handler = entity.GetComponent<TagHandler>();
            EffectHandler effectHandler = entity.GetComponent<EffectHandler>();

            AttributeEffect effect = CreateEffect(
                "Buff",
                e =>
                {
                    e.effectTags.Add("Buff");
                }
            );
            EffectHandle handle = effectHandler.ApplyEffect(effect).Value;

            List<EffectHandle> removed = handler.RemoveTag("Buff");
            Assert.AreEqual(1, removed.Count);
            Assert.AreEqual(handle, removed[0]);
            Assert.IsFalse(handler.HasTag("Buff"));
            Assert.AreEqual(0, handler.GetHandlesWithTag("Buff").Count);

            List<EffectHandle> empty = handler.RemoveTag("Missing");
            Assert.IsNotNull(empty);
            Assert.AreEqual(0, empty.Count);
            yield return null;
        }
    }
}
