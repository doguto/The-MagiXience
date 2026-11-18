#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // エディタのグラフ表示用。ノード定義の guid ごとに最後に Tick された時刻を記録する。
    public static class BehaviorTreeDebug
    {
        static readonly Dictionary<string, float> lastTicked = new();

        public static void MarkTicked(string guid) => lastTicked[guid] = Time.realtimeSinceStartup;

        public static bool WasTickedRecently(string guid, float withinSeconds = 0.15f) =>
            lastTicked.TryGetValue(guid, out var t) && Time.realtimeSinceStartup - t <= withinSeconds;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay() => lastTicked.Clear();
    }
}
#endif
