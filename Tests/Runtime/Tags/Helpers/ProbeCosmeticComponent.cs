namespace WallstopStudios.UnityHelpers.Tests.Tags.Helpers
{
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Tags;

    internal sealed class ProbeCosmeticComponent : CosmeticEffectComponent
    {
        public bool requiresInstance;
        public bool cleansSelf;
        public readonly List<GameObject> appliedTargets = new();
        public readonly List<GameObject> removedTargets = new();

        public override bool RequiresInstance => requiresInstance;
        public override bool CleansUpSelf => cleansSelf;

        public override void OnApplyEffect(GameObject target)
        {
            base.OnApplyEffect(target);
            appliedTargets.Add(target);
        }

        public override void OnRemoveEffect(GameObject target)
        {
            base.OnRemoveEffect(target);
            removedTargets.Add(target);
        }
    }
}
