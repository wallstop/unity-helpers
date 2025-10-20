namespace WallstopStudios.UnityHelpers.Tests.Tags
{
    using System.Collections;
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Tags;
    using WallstopStudios.UnityHelpers.Tests.Tags.Helpers;

    internal sealed class ProbeCosmeticComponent : CosmeticEffectComponent
    {
        public bool requiresInstance;
        public bool cleansSelf;
        public readonly List<GameObject> appliedTargets = new();
        public readonly List<GameObject> removedTargets = new();

        public override bool RequiresInstance => requiresInstance;
        public override bool CleansUpSelf => cleansSelf;

        public override void OnApplyEffect(GameObject target)
        {
            base.OnApplyEffect(target);
            appliedTargets.Add(target);
        }

        public override void OnRemoveEffect(GameObject target)
        {
            base.OnRemoveEffect(target);
            removedTargets.Add(target);
        }
    }

    internal sealed class SecondaryProbeCosmeticComponent : CosmeticEffectComponent { }

    internal sealed class SpyCosmeticComponent : CosmeticEffectComponent
    {
        public static int RemoveInvocationCount { get; private set; }

        public static void Reset()
        {
            RemoveInvocationCount = 0;
        }

        public int AppliedCount => _appliedTargets.Count;

        public override void OnApplyEffect(GameObject target)
        {
            base.OnApplyEffect(target);
        }

        public override void OnRemoveEffect(GameObject target)
        {
            base.OnRemoveEffect(target);
            ++RemoveInvocationCount;
        }
    }

    [TestFixture]
    public sealed class CosmeticEffectComponentTests : TagsTestBase
    {
        [Test]
        public void OnApplyAndRemoveEffectMaintainTargetList()
        {
            GameObject cosmetic = CreateTrackedGameObject("Cosmetic", typeof(CosmeticEffectData));
            SpyCosmeticComponent.Reset();
            SpyCosmeticComponent component = cosmetic.AddComponent<SpyCosmeticComponent>();
            GameObject target = CreateTrackedGameObject("Target");

            component.OnApplyEffect(target);
            Assert.AreEqual(1, component.AppliedCount);

            component.OnRemoveEffect(target);
            Assert.AreEqual(0, component.AppliedCount);
        }

        [Test]
        public void OnDestroyInvokesRemoveEffectForTrackedTargets()
        {
            GameObject cosmetic = CreateTrackedGameObject("Cosmetic", typeof(CosmeticEffectData));
            SpyCosmeticComponent.Reset();
            SpyCosmeticComponent component = cosmetic.AddComponent<SpyCosmeticComponent>();
            GameObject target = CreateTrackedGameObject("Target");

            component.OnApplyEffect(target);
            Object.DestroyImmediate(cosmetic);
            Assert.AreEqual(1, SpyCosmeticComponent.RemoveInvocationCount);
        }
    }

    [TestFixture]
    public sealed class CollisionSensesTests : TagsTestBase
    {
        [UnityTest]
        public IEnumerator ApplyingDisableTagDisablesChildColliders()
        {
            GameObject entity = CreateTrackedGameObject(
                "Entity",
                typeof(TagHandler),
                typeof(CollisionSenses)
            );
            GameObject child = CreateTrackedGameObject("Collider", typeof(BoxCollider2D));
            child.transform.SetParent(entity.transform);
            BoxCollider2D collider = child.GetComponent<BoxCollider2D>();
            collider.enabled = true;

            yield return null;

            TagHandler handler = entity.GetComponent<TagHandler>();
            handler.ApplyTag(CollisionSenses.CollisionDisabledTag);
            yield return null;
            Assert.IsFalse(collider.enabled);

            handler.RemoveTag(CollisionSenses.CollisionDisabledTag);
            yield return null;
            Assert.IsTrue(collider.enabled);
        }

        [UnityTest]
        public IEnumerator DisabledCollidersAreIgnored()
        {
            GameObject entity = CreateTrackedGameObject(
                "Entity",
                typeof(TagHandler),
                typeof(CollisionSenses)
            );

            GameObject activeChild = CreateTrackedGameObject("Active", typeof(BoxCollider2D));
            activeChild.transform.SetParent(entity.transform);
            BoxCollider2D active = activeChild.GetComponent<BoxCollider2D>();
            active.enabled = true;

            GameObject disabledChild = CreateTrackedGameObject("Disabled", typeof(BoxCollider2D));
            disabledChild.transform.SetParent(entity.transform);
            BoxCollider2D disabled = disabledChild.GetComponent<BoxCollider2D>();
            disabled.enabled = false;

            yield return null;

            entity.GetComponent<TagHandler>().ApplyTag(CollisionSenses.CollisionDisabledTag);
            yield return null;

            Assert.IsFalse(active.enabled);
            Assert.IsFalse(disabled.enabled);
        }
    }
}
