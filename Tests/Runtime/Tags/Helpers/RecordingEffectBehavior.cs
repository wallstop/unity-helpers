namespace WallstopStudios.UnityHelpers.Tests.Tags.Helpers
{
    using WallstopStudios.UnityHelpers.Tags;

    public sealed class RecordingEffectBehavior : EffectBehavior
    {
        public static int ApplyCount { get; private set; }
        public static int TickCount { get; private set; }
        public static int PeriodicTickCount { get; private set; }
        public static int RemoveCount { get; private set; }

        public static void Reset()
        {
            ApplyCount = 0;
            TickCount = 0;
            PeriodicTickCount = 0;
            RemoveCount = 0;
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
