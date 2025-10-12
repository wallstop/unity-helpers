namespace WallstopStudios.UnityHelpers.Tests.Tags
{
    using System.Collections;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Tags;
    using WallstopStudios.UnityHelpers.Tests.Tags.Helpers;

    [TestFixture]
    public sealed class CosmeticEffectDataTests : TagsTestBase
    {
        [UnityTest]
        public IEnumerator RequiresInstancingTrueWhenAnyComponentRequestsIt()
        {
            GameObject cosmetic = CreateTrackedGameObject("Cosmetic", typeof(CosmeticEffectData));
            yield return null;
            ProbeCosmeticComponent component = cosmetic.AddComponent<ProbeCosmeticComponent>();
            component.requiresInstance = true;

            CosmeticEffectData data = cosmetic.GetComponent<CosmeticEffectData>();
            Assert.IsTrue(data.RequiresInstancing);
        }

        [UnityTest]
        public IEnumerator EqualsReturnsTrueWhenNamesAndComponentsMatch()
        {
            GameObject first = CreateTrackedGameObject("CosmeticA", typeof(CosmeticEffectData));
            yield return null;
            _ = first.AddComponent<ProbeCosmeticComponent>();

            GameObject second = CreateTrackedGameObject("CosmeticB", typeof(CosmeticEffectData));
            yield return null;
            second.name = first.name;
            _ = second.AddComponent<ProbeCosmeticComponent>();

            CosmeticEffectData firstData = first.GetComponent<CosmeticEffectData>();
            CosmeticEffectData secondData = second.GetComponent<CosmeticEffectData>();
            Assert.IsTrue(firstData.Equals(secondData));
            Assert.AreEqual(firstData.GetHashCode(), secondData.GetHashCode());
        }

        [UnityTest]
        public IEnumerator EqualsReturnsFalseWhenComponentSetsDiffer()
        {
            GameObject first = CreateTrackedGameObject("Cosmetic", typeof(CosmeticEffectData));
            yield return null;
            _ = first.AddComponent<ProbeCosmeticComponent>();

            GameObject second = CreateTrackedGameObject("Cosmetic", typeof(CosmeticEffectData));
            yield return null;
            _ = second.AddComponent<ProbeCosmeticComponent>();
            _ = second.AddComponent<SecondaryProbeCosmeticComponent>();

            CosmeticEffectData firstData = first.GetComponent<CosmeticEffectData>();
            CosmeticEffectData secondData = second.GetComponent<CosmeticEffectData>();
            Assert.IsFalse(firstData.Equals(secondData));
        }
    }
}
