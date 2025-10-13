#if VCONTAINER_PRESENT
namespace WallstopStudios.UnityHelpers.Tests.Integrations.VContainer
{
    using System.Collections;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Integrations.VContainer;
    using WallstopStudios.UnityHelpers.Tags;
    using WallstopStudios.UnityHelpers.Tests.Editor.Utils;

    public sealed class VContainerRelationalEntryPointTests : CommonTestBase
    {
        private sealed class Consumer : MonoBehaviour
        {
            [SiblingComponent]
            private SpriteRenderer _spriteRenderer;

            public SpriteRenderer SR => _spriteRenderer;
        }

        [UnityTest]
        public IEnumerator EntryPointAssignsSiblingOnActiveScene()
        {
            CreateTempScene("VContainerTestScene");

            GameObject go = NewGameObject("Root");
            go.AddComponent<SpriteRenderer>();
            Consumer consumer = go.AddComponent<Consumer>();

            // Give Unity a frame to register objects
            yield return null;

            AttributeMetadataCache cache = CreateScriptableObject<AttributeMetadataCache>();
#if UNITY_EDITOR
            AttributeMetadataCache.RelationalTypeMetadata relationalMetadata =
                new AttributeMetadataCache.RelationalTypeMetadata(
                    typeof(Consumer).AssemblyQualifiedName,
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
            yield return null;
#endif

            RelationalComponentAssigner assigner = new RelationalComponentAssigner(cache);
            RelationalComponentEntryPoint entry = new RelationalComponentEntryPoint(
                assigner,
                cache,
                RelationalSceneAssignmentOptions.Default
            );

            entry.Initialize();

            // Allow assignment to complete
            yield return null;

            Assert.IsTrue(consumer != null);
            Assert.IsTrue(
                consumer.SR != null,
                "Relational field should be assigned by entry point"
            );
        }
    }
}
#endif
