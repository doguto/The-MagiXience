using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 予測線 1 本分。Start から Direction へ Length の長さ (0 なら予測線 Prefab の既定の長さ)
    public struct BeamLine
    {
        public Vector2 Start;
        public Vector2 Direction;
        public float Length;
    }
}
