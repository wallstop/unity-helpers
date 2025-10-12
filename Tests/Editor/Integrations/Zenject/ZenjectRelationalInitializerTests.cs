#if ZENJECT_PRESENT
namespace WallstopStudios.UnityHelpers.Tests.Integrations.Zenject
{
    using System.Collections;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Integrations.Zenject;
    using WallstopStudios.UnityHelpers.Tags;

    public sealed class ZenjectRelationalInitializerTests
    {
        private sealed class Consumer : MonoBehaviour
        {
            [SiblingComponent]
            private SpriteRenderer _spriteRenderer;

            public SpriteRenderer SR => _spriteRenderer;
        }

        [UnityTest]
        public IEnumerator InitializerAssignsSiblingOnActiveScene()
        {
            Scene scene = SceneManager.CreateScene("ZenjectTestScene");
            SceneManager.SetActiveScene(scene);

            GameObject go = new GameObject("Root");
            go.AddComponent<SpriteRenderer>();
            Consumer consumer = go.AddComponent<Consumer>();

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
            RelationalComponentSceneInitializer initializer =
                new RelationalComponentSceneInitializer(
                    assigner,
                    cache,
                    RelationalSceneAssignmentOptions.Default
                );

            initializer.Initialize();

            yield return null;

            Assert.NotNull(consumer);
            Assert.NotNull(consumer.SR, "Relational field should be assigned by initializer");
        }
    }
}
#endif
