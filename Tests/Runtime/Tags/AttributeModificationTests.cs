namespace WallstopStudios.UnityHelpers.Tests.Tags
{
    using System.Text.Json;
    using NUnit.Framework;
    using WallstopStudios.UnityHelpers.Core.Serialization;
    using WallstopStudios.UnityHelpers.Tags;
    using WallstopStudios.UnityHelpers.Tests.TestUtils;

    public abstract class AttributeTagsTestBase : CommonTestBase
    {
        protected static void ResetEffectHandleId(long value = 0)
        {
            EffectHandle.Id = value;
        }
    }

    [TestFixture]
    public sealed class AttributeModificationTests
    {
        [Test]
        public void EqualityOperatorsRespectFields()
        {
            AttributeModification baseline = new()
            {
                attribute = "health",
                action = ModificationAction.Multiplication,
                value = 1.5f,
            };

            AttributeModification clone = baseline;
            Assert.IsTrue(baseline == clone);
            Assert.IsFalse(baseline != clone);
            Assert.AreEqual(baseline, clone);
            Assert.AreEqual(baseline.GetHashCode(), clone.GetHashCode());

            AttributeModification differentAttribute = baseline;
            differentAttribute.attribute = "armor";
            Assert.IsFalse(baseline == differentAttribute);
            Assert.IsTrue(baseline != differentAttribute);

            AttributeModification differentAction = baseline;
            differentAction.action = ModificationAction.Addition;
            Assert.IsFalse(baseline.Equals(differentAction));

            AttributeModification differentValue = baseline;
            differentValue.value = 2f;
            Assert.IsFalse(baseline.Equals(differentValue));
        }

        [Test]
        public void ToStringSerializesAllFields()
        {
            AttributeModification modification = new()
            {
                attribute = "health",
                action = ModificationAction.Override,
                value = 42.5f,
            };

            using JsonDocument document = JsonDocument.Parse(modification.ToString());
            JsonElement root = document.RootElement;
            Assert.AreEqual("health", root.GetProperty("attribute").GetString());
            Assert.AreEqual("Override", root.GetProperty("action").GetString());
            Assert.AreEqual(42.5f, root.GetProperty("value").GetSingle());
        }

        [Test]
        public void SystemTextJsonRoundtripPreservesFields()
        {
            AttributeModification modification = new(
                "armor",
                ModificationAction.Multiplication,
                1.25f
            );
            string json = Serializer.JsonStringify(modification);
            AttributeModification clone = Serializer.JsonDeserialize<AttributeModification>(json);

            Assert.AreEqual(modification, clone);
        }

        [Test]
        public void ProtoBufRoundtripPreservesFields()
        {
            AttributeModification modification = new("speed", ModificationAction.Addition, -3f);
            byte[] payload = Serializer.ProtoSerialize(modification);
            AttributeModification clone = Serializer.ProtoDeserialize<AttributeModification>(
                payload
            );
            Assert.AreEqual(modification, clone);
        }
    }
}
