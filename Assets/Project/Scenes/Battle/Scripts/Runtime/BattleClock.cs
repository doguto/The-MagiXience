using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    // 描画フレームとは独立に、固定 60fps で BattleWorld を進める。Time.timeScale = 0 (ポーズ) の間は止まる。
    public sealed class BattleClock : MonoBehaviour
    {
        // 処理落ち時に 1 描画フレームで追いつく上限。超えた分は切り捨てる (スロー扱い)
        const int MaxStepsPerFrame = 4;

        float accumulator;

        public BattleWorld World { get; private set; }

        public void Begin(BattleWorld world)
        {
            World = world;
            accumulator = 0f;
        }

        public void End()
        {
            World?.Clear();
            World = null;
        }

        void Update()
        {
            if (World == null) return;

            accumulator += Time.deltaTime;
            var steps = 0;
            while (accumulator >= BattleWorld.DeltaTime && steps < MaxStepsPerFrame)
            {
                accumulator -= BattleWorld.DeltaTime;
                World.Step();
                steps++;
            }
            if (steps == MaxStepsPerFrame) accumulator = Mathf.Min(accumulator, BattleWorld.DeltaTime);
        }
    }
}
