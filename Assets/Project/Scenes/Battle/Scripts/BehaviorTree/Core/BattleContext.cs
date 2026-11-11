using System;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    public sealed class BattleContext
    {
        public const int FramesPerSecond = 60;

        public BattleContext(int seed, Func<float> bgmTimeProvider = null)
        {
            Random = new Random(seed);
            this.bgmTimeProvider = bgmTimeProvider;
        }

        readonly Func<float> bgmTimeProvider;

        public int Frame { get; private set; }
        public Random Random { get; }
        public float BgmTime => bgmTimeProvider?.Invoke() ?? 0f;

        internal void AdvanceFrame() => Frame++;

        public static int SecondsToFrames(float seconds) => (int)Math.Round(seconds * FramesPerSecond);
    }
}
