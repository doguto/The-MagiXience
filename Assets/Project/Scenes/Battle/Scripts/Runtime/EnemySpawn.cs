using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.BehaviorTree;
using Project.Scenes.Battle.Scripts.Runtime.Motion;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    public struct EnemySpawn
    {
        public GameObject Prefab;
        public Vector3 Position;
        public Quaternion Rotation;
        // 出した敵に付けるツリー。未指定ならツリーを付けない
        public EnemyTreeAsset Behavior;
        public IReadOnlyList<MotionStep> Motion;
        // 出した敵・弾 (合図の宛先「自分が出したもの」の判定に使う)
        public IAgent Owner;
        public string Group;
        public IReadOnlyList<TreeParameter> Overrides;
        // 「敵の全滅」の判定対象にするか
        public bool Tracked;
        // ビーム/警告線の設定。0 以下は Prefab の値
        public float BeamRange;
        public float BeamDuration;
        public float BeamWidth;
    }
}
