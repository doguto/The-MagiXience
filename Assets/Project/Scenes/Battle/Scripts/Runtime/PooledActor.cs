using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    // ActorPool が生成時に付ける目印。戻し先のプールと元の Prefab を覚えておく
    [DisallowMultipleComponent]
    public sealed class PooledActor : MonoBehaviour
    {
        public GameObject Prefab { get; set; }
        public ActorPool Pool { get; set; }
        public bool HasBeenActive { get; set; }
    }
}
