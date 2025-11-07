#if UNITY_EDITOR
namespace WallstopStudios.UnityHelpers.Tests.Core.Attributes
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Tags;
    using WallstopStudios.UnityHelpers.Tests.Utils;

    public sealed class RelationalComponentAssignerTests : CommonTestBase
    {
        private sealed class RelationalConsumer : MonoBehaviour
        {
            [SiblingComponent]
            private SpriteRenderer _spriteRenderer;

            public SpriteRenderer SR => _spriteRenderer;
        }

        private sealed class NonRelational : MonoBehaviour
        {
            public int x;
        }

        [Test]
        public void HasRelationalAssignmentsRespectsMetadata()
        {
            AttributeMetadataCache cache = CreateScriptableObject<AttributeMetadataCache>();

            AttributeMetadataCache.RelationalTypeMetadata relationalMetadata = new(
                typeof(RelationalConsumer).AssemblyQualifiedName,
                new[]
                {
                    new AttributeMetadataCache.RelationalFieldMetadata(
                        "_spriteRenderer",
                        AttributeMetadataCache.RelationalAttributeKind.Sibling,
                        AttributeMetadataCache.FieldKind.Single,
                        typeof(SpriteRenderer).AssemblyQualifiedName,
                        false
                    ),
                }
            );

            cache.SetMetadata(
                System.Array.Empty<string>(),
                System.Array.Empty<AttributeMetadataCache.TypeFieldMetadata>(),
                new[] { relationalMetadata }
            );
            cache.ForceRebuildForTests();

            RelationalComponentAssigner assigner = new(cache);

            Assert.IsTrue(
                assigner.HasRelationalAssignments(typeof(RelationalConsumer)),
                "Expected HasRelationalAssignments to be true for type with relational metadata"
            );
            Assert.IsFalse(
                assigner.HasRelationalAssignments(typeof(NonRelational)),
                "Expected HasRelationalAssignments to be false for type without relational metadata"
            );
        }

        [Test]
        public void AssignIEnumerableAssignsOnlyRelationalTypesAndSkipsNulls()
        {
            AttributeMetadataCache cache = CreateScriptableObject<AttributeMetadataCache>();

            AttributeMetadataCache.RelationalTypeMetadata relationalMetadata = new(
                typeof(RelationalConsumer).AssemblyQualifiedName,
                new[]
                {
                    new AttributeMetadataCache.RelationalFieldMetadata(
                        "_spriteRenderer",
                        AttributeMetadataCache.RelationalAttributeKind.Sibling,
                        AttributeMetadataCache.FieldKind.Single,
                        typeof(SpriteRenderer).AssemblyQualifiedName,
                        false
                    ),
                }
            );

            cache.SetMetadata(
                System.Array.Empty<string>(),
                System.Array.Empty<AttributeMetadataCache.TypeFieldMetadata>(),
                new[] { relationalMetadata }
            );
            cache.ForceRebuildForTests();

            RelationalComponentAssigner assigner = new(cache);

            GameObject go1 = NewGameObject("Relational");
            SpriteRenderer sr1 = go1.AddComponent<SpriteRenderer>();
            RelationalConsumer consumer = go1.AddComponent<RelationalConsumer>();

            GameObject go2 = NewGameObject("NonRelational");
            NonRelational non = go2.AddComponent<NonRelational>();

            Assert.IsTrue(consumer.SR == null, "Precondition: relational field should start null");

            List<Component> items = new() { consumer, null, non.transform };
            assigner.Assign(items);

            Assert.IsTrue(
                consumer.SR != null,
                "Relational field should be assigned by Assign(IEnumerable<Component>)"
            );

            // Sanity: ensure non-relational type was not modified by the assigner
            Assert.AreEqual(0, non.x);

            // Cleanup handled by UnityTestBase
        }
    }
}
#endif
