using System;
using Project.Scenes.Battle.Scripts.Runtime;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // ツリー 1 本ぶんの実行文脈。World は全ツリーで共有し、Self はそのツリーを持つ敵/ボス (ステージ進行なら null)。
    public sealed class BattleContext
    {
        public const int FramesPerSecond = BattleWorld.FramesPerSecond;

        public BattleContext(BattleWorld world, BattleActor self = null)
        {
            World = world ?? throw new ArgumentNullException(nameof(world));
            Self = self;
        }

        public BattleContext(int seed, Func<float> bgmTimeProvider = null) : this(new BattleWorld(seed, bgmTimeProvider))
        {
        }

        public BattleWorld World { get; }
        public BattleActor Self { get; }
        public int Frame => World.Frame;
        public Random Random => World.Random;
        public float BgmTime => World.BgmTime;
        public BattleServices Services => World.Services;
        public StageContext Stage => World.Stage;

        internal void AdvanceFrame() => World.AdvanceFrame();

        public static int SecondsToFrames(float seconds) => (int)Math.Round(seconds * FramesPerSecond);
    }
}
