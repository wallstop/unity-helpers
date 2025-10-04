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
            handler.RemoveTag("Buff", allInstances: false);
            handler.RemoveTag("Buff", allInstances: false);

            Assert.AreEqual(new[] { "Buff" }, added);
            Assert.AreEqual(("Buff", 2U), changed[0]);
            Assert.AreEqual(("Buff", 1U), changed[1]);
            Assert.AreEqual(new[] { "Buff" }, removed);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RemoveTagAllInstancesClearsWithoutEvents()
        {
            GameObject entity = CreateTrackedGameObject("Tags", typeof(TagHandler));
            TagHandler handler = entity.GetComponent<TagHandler>();

            int removedCount = 0;
            handler.OnTagRemoved += _ => ++removedCount;

            handler.ApplyTag("Buff");
            handler.RemoveTag("Buff", allInstances: true);

            Assert.IsFalse(handler.HasTag("Buff"));
            Assert.AreEqual(0, removedCount);
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
    }
}
