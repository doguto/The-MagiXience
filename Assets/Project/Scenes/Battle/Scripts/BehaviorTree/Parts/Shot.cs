using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    public struct Shot
    {
        public Vector2 Direction;
        public Vector2 Offset;
        public int SpawnDelayFrames;
        public int StartDelayFrames;
    }
}
