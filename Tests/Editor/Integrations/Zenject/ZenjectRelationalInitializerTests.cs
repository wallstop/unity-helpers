#if ZENJECT_PRESENT
namespace WallstopStudios.UnityHelpers.Tests.Integrations.Zenject
{
    using System.Collections;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Integrations.Zenject;
    using WallstopStudios.UnityHelpers.Tags;
    using WallstopStudios.UnityHelpers.Tests.Editor.Utils;

    public sealed class ZenjectRelationalInitializerTests : CommonTestBase
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
            CreateTempScene("ZenjectTestScene");

            GameObject go = NewGameObject("Root");
            go.AddComponent<SpriteRenderer>();
            Consumer consumer = go.AddComponent<Consumer>();

            yield return null;

            AttributeMetadataCache cache = CreateScriptableObject<AttributeMetadataCache>();
#if UNITY_EDITOR
            AttributeMetadataCache.RelationalTypeMetadata relationalMetadata = new(
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

            RelationalComponentAssigner assigner = new(cache);
            RelationalComponentSceneInitializer initializer = new(
                assigner,
                cache,
                RelationalSceneAssignmentOptions.Default
            );

            initializer.Initialize();

            yield return null;

            Assert.IsTrue(consumer != null);
            Assert.IsTrue(
                consumer.SR != null,
                "Relational field should be assigned by initializer"
            );
        }
    }
}
#endif
