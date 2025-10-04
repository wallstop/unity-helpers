namespace WallstopStudios.UnityHelpers.Tests.Tags.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Tags;

    public abstract class TagsTestBase : AttributeTagsTestBase
    {
        protected GameObject CreateTrackedGameObject(string name, params Type[] componentTypes)
        {
            GameObject gameObject = Track(new GameObject(name));
            if (componentTypes is not { Length: > 0 })
            {
                return gameObject;
            }

            foreach (Type componentType in componentTypes)
            {
                if (gameObject.GetComponent(componentType) != null)
                {
                    continue;
                }

                _ = gameObject.AddComponent(componentType);
            }

            return gameObject;
        }

        protected AttributeEffect CreateEffect(
            string name,
            Action<AttributeEffect> configure = null
        )
        {
            AttributeEffect effect = Track(ScriptableObject.CreateInstance<AttributeEffect>());
            effect.name = name;
            effect.durationType = ModifierDurationType.Duration;
            effect.duration = 1f;
            configure?.Invoke(effect);
            return effect;
        }

        protected static void ClearAttributeUtilitiesCaches()
        {
            AttributeUtilities.AllAttributeNames = null;
            AttributeUtilities.AttributeFields.Clear();
        }
    }
}
