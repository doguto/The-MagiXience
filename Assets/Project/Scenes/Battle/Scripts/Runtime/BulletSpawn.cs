using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.BehaviorTree;
using Project.Scenes.Battle.Scripts.Presenter.Entity;
using Project.Scenes.Battle.Scripts.Runtime.Motion;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    public struct BulletSpawn
    {
        public BulletEntityPresenter Prefab;
        public int Damage;
        public Vector3 Position;
        public Vector2 Direction;
        // Prefab 本来の回転に対するオフセット
        public Quaternion Rotation;
        // 飛距離の上限 (発射方向への進捗)。0 以下で無制限
        public float Range;
        // 消えるまでのフレーム数。0 以下で無制限
        public int LifetimeFrames;
        // 出現してから動き出すまでのフレーム数
        public int StartDelayFrames;
        public IReadOnlyList<MotionStep> Motion;
        // 弾を動かすツリー。未指定なら Motion を実行するだけ
        public BulletTreeAsset Tree;
        public IReadOnlyList<TreeParameter> Overrides;
        // 撃った敵・弾 (合図の宛先「自分が出したもの」の判定に使う)
        public IAgent Owner;
        public string Group;
        public float AttackTime;
        public bool AffectedByForce;
        // 自機のチャージ弾。敵の「チャージ攻撃のみ有効」の判定に使う
        public bool IsPlayerChargeBullet;
    }
}
