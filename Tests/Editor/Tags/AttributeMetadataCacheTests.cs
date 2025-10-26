namespace WallstopStudios.UnityHelpers.Tests.Editor.Tags
{
    using System;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEngine;
    using UnityHelpers.Tags;

    [TestFixture]
    public sealed class AttributeMetadataCacheTests
    {
        [Test]
        public void SetMetadataSortsSerializedContent()
        {
            AttributeMetadataCache cache =
                ScriptableObject.CreateInstance<AttributeMetadataCache>();

            try
            {
                string[] attributeNames = new string[] { "Gamma", "Alpha", "Beta" };

                string alphaAttributesTypeName =
                    typeof(AlphaAttributesComponent).AssemblyQualifiedName ?? string.Empty;
                string bravoAttributesTypeName =
                    typeof(BravoAttributesComponent).AssemblyQualifiedName ?? string.Empty;

                AttributeMetadataCache.TypeFieldMetadata alphaTypeMetadata = new(
                    alphaAttributesTypeName,
                    new string[] { "gammaField", "betaField" }
                );

                AttributeMetadataCache.TypeFieldMetadata bravoTypeMetadata = new(
                    bravoAttributesTypeName,
                    new string[] { "zetaField", "alphaField" }
                );

                string alphaRelationalTypeName =
                    typeof(AlphaRelationalComponent).AssemblyQualifiedName ?? string.Empty;
                string bravoRelationalTypeName =
                    typeof(BravoRelationalComponent).AssemblyQualifiedName ?? string.Empty;

                AttributeMetadataCache.RelationalFieldMetadata[] alphaRelationalFields =
                    new AttributeMetadataCache.RelationalFieldMetadata[]
                    {
                        new(
                            "betaRelation",
                            AttributeMetadataCache.RelationalAttributeKind.Parent,
                            AttributeMetadataCache.FieldKind.HashSet,
                            typeof(Light).AssemblyQualifiedName ?? string.Empty,
                            true
                        ),
                        new(
                            "alphaRelation",
                            AttributeMetadataCache.RelationalAttributeKind.Child,
                            AttributeMetadataCache.FieldKind.Single,
                            typeof(Transform).AssemblyQualifiedName ?? string.Empty,
                            false
                        ),
                    };

                AttributeMetadataCache.RelationalFieldMetadata[] bravoRelationalFields =
                    new AttributeMetadataCache.RelationalFieldMetadata[]
                    {
                        new(
                            "zetaRelation",
                            AttributeMetadataCache.RelationalAttributeKind.Sibling,
                            AttributeMetadataCache.FieldKind.List,
                            typeof(Camera).AssemblyQualifiedName ?? string.Empty,
                            true
                        ),
                        new(
                            "alphaRelation",
                            AttributeMetadataCache.RelationalAttributeKind.Child,
                            AttributeMetadataCache.FieldKind.Single,
                            typeof(Transform).AssemblyQualifiedName ?? string.Empty,
                            false
                        ),
                    };

                AttributeMetadataCache.RelationalTypeMetadata[] relationalMetadata =
                    new AttributeMetadataCache.RelationalTypeMetadata[]
                    {
                        new(
                            bravoRelationalTypeName,
                            new AttributeMetadataCache.RelationalFieldMetadata[]
                            {
                                bravoRelationalFields[0],
                                null,
                                bravoRelationalFields[1],
                            }
                        ),
                        null,
                        new(alphaRelationalTypeName, alphaRelationalFields),
                    };

                AttributeMetadataCache.TypeFieldMetadata[] typeMetadata =
                    new AttributeMetadataCache.TypeFieldMetadata[]
                    {
                        bravoTypeMetadata,
                        null,
                        alphaTypeMetadata,
                    };

                cache.SetMetadata(attributeNames, typeMetadata, relationalMetadata);

                string[] storedAttributeNames = GetPrivateField<string[]>(
                    cache,
                    "_allAttributeNames"
                );
                Assert.That(
                    storedAttributeNames,
                    Is.EqualTo(new string[] { "Alpha", "Beta", "Gamma" })
                );

                AttributeMetadataCache.TypeFieldMetadata[] storedTypeMetadata =
                    GetPrivateField<AttributeMetadataCache.TypeFieldMetadata[]>(
                        cache,
                        "_typeMetadata"
                    );
                Assert.That(storedTypeMetadata.Length, Is.EqualTo(2));
                Assert.That(storedTypeMetadata[0].typeName, Is.EqualTo(alphaAttributesTypeName));
                Assert.That(
                    storedTypeMetadata[0].fieldNames,
                    Is.EqualTo(new string[] { "betaField", "gammaField" })
                );
                Assert.That(storedTypeMetadata[1].typeName, Is.EqualTo(bravoAttributesTypeName));
                Assert.That(
                    storedTypeMetadata[1].fieldNames,
                    Is.EqualTo(new string[] { "alphaField", "zetaField" })
                );

                AttributeMetadataCache.RelationalTypeMetadata[] storedRelationalMetadata =
                    GetPrivateField<AttributeMetadataCache.RelationalTypeMetadata[]>(
                        cache,
                        "_relationalTypeMetadata"
                    );
                Assert.That(storedRelationalMetadata.Length, Is.EqualTo(2));
                Assert.That(
                    storedRelationalMetadata[0].typeName,
                    Is.EqualTo(alphaRelationalTypeName)
                );
                Assert.That(storedRelationalMetadata[0].fields.Length, Is.EqualTo(2));
                Assert.That(
                    storedRelationalMetadata[0].fields[0].fieldName,
                    Is.EqualTo("alphaRelation")
                );
                Assert.That(
                    storedRelationalMetadata[0].fields[1].fieldName,
                    Is.EqualTo("betaRelation")
                );
                Assert.That(
                    storedRelationalMetadata[1].typeName,
                    Is.EqualTo(bravoRelationalTypeName)
                );
                Assert.That(storedRelationalMetadata[1].fields.Length, Is.EqualTo(2));
                Assert.That(
                    storedRelationalMetadata[1].fields[0].fieldName,
                    Is.EqualTo("alphaRelation")
                );
                Assert.That(
                    storedRelationalMetadata[1].fields[1].fieldName,
                    Is.EqualTo("zetaRelation")
                );
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cache);
            }
        }

        private static T GetPrivateField<T>(AttributeMetadataCache cache, string fieldName)
        {
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo field = typeof(AttributeMetadataCache).GetField(fieldName, bindingFlags);
            if (field == null)
            {
                throw new InvalidOperationException($"Field '{fieldName}' was not found.");
            }

            object value = field.GetValue(cache);
            return value is T castValue ? castValue : default;
        }

        private sealed class AlphaAttributesComponent : AttributesComponent { }

        private sealed class BravoAttributesComponent : AttributesComponent { }

        private sealed class AlphaRelationalComponent : MonoBehaviour { }

        private sealed class BravoRelationalComponent : MonoBehaviour { }
    }
}
