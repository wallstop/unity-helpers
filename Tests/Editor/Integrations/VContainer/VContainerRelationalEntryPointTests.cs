#if VCONTAINER_PRESENT
namespace WallstopStudios.UnityHelpers.Tests.Integrations.VContainer
{
    using System.Collections;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Integrations.VContainer;
    using WallstopStudios.UnityHelpers.Tags;

    public sealed class VContainerRelationalEntryPointTests
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
            Scene scene = SceneManager.CreateScene("VContainerTestScene");
            SceneManager.SetActiveScene(scene);

            GameObject go = new GameObject("Root");
            go.AddComponent<SpriteRenderer>();
            Consumer consumer = go.AddComponent<Consumer>();

            // Give Unity a frame to register objects
            yield return null;

            AttributeMetadataCache cache =
                ScriptableObject.CreateInstance<AttributeMetadataCache>();
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

            Assert.NotNull(consumer);
            Assert.NotNull(consumer.SR, "Relational field should be assigned by entry point");
        }
    }
}
#endif
