using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.BehaviorTree;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    public struct ActorSpawn
    {
        public GameObject Prefab;
        public Vector3 Position;
        public Quaternion Rotation;
        // 出した敵に付けるツリー。未指定ならツリーを付けない
        public BehaviorTreeAsset Behavior;
        public IReadOnlyList<TreeParameter> Overrides;
        // 「敵の全滅」の判定対象にするか
        public bool Tracked;
        // ビーム/警告線の設定。0 以下は Prefab の値
        public float BeamRange;
        public float BeamDuration;
        public float BeamWidth;
    }
}
