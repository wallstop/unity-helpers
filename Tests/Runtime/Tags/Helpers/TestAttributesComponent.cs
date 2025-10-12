namespace WallstopStudios.UnityHelpers.Tests.Tags.Helpers
{
    using System.Collections.Generic;
    using WallstopStudios.UnityHelpers.Tags;

    internal sealed class TestAttributesComponent : AttributesComponent
    {
        public Attribute health = new(100f);
        public Attribute armor = new(50f);
        public readonly List<(string attribute, float previous, float current)> notifications =
            new();

        protected override void Awake()
        {
            base.Awake();
            OnAttributeModified += (attribute, previous, current) =>
            {
                notifications.Add((attribute, previous, current));
            };
        }

        public void ResetAttributes(float healthValue = 100f, float armorValue = 50f)
        {
            health = new Attribute(healthValue);
            armor = new Attribute(armorValue);
        }
    }
}
