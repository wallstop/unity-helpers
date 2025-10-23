namespace WallstopStudios.UnityHelpers.Tests.Tags.Helpers
{
    using System.Collections.Generic;
    using WallstopStudios.UnityHelpers.Tags;

    public sealed class RecordingEffectBehavior : EffectBehavior
    {
        private static readonly HashSet<int> InstanceIds = new();

        public static int ApplyCount { get; private set; }

        public static int TickCount { get; private set; }

        public static int PeriodicTickCount { get; private set; }

        public static int RemoveCount { get; private set; }

        public static int InstanceCount => InstanceIds.Count;

        public static void Reset()
        {
            ApplyCount = 0;
            TickCount = 0;
            PeriodicTickCount = 0;
            RemoveCount = 0;
            InstanceIds.Clear();
        }

        private void OnEnable()
        {
            _ = InstanceIds.Add(GetInstanceID());
        }

        public override void OnApply(EffectBehaviorContext context)
        {
            ++ApplyCount;
        }

        public override void OnTick(EffectBehaviorContext context)
        {
            ++TickCount;
        }

        public override void OnPeriodicTick(
            EffectBehaviorContext context,
            PeriodicEffectTickContext tickContext
        )
        {
            ++PeriodicTickCount;
        }

        public override void OnRemove(EffectBehaviorContext context)
        {
            ++RemoveCount;
        }
    }
}
