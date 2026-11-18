using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 小数の周期をフレームに割り振る。k 回目から k+1 回目までの間隔は round((k+1)p) - round(kp) なので、22.5 なら 22, 23, 23, 22, … と並ぶ
    public static class FrameSchedule
    {
        public static int Step(float period, int index) =>
            Mathf.Max(1, Mathf.RoundToInt((index + 1) * period) - Mathf.RoundToInt(index * period));
    }
}
