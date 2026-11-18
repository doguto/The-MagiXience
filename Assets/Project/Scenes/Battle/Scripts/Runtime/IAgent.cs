using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.BehaviorTree;
using Project.Scenes.Battle.Scripts.Runtime.Motion;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    // ツリーの Self になるもの (敵・弾)
    public interface IAgent
    {
        BattleWorld World { get; }
        IMotionBody Body { get; }
        // 出現させた側から渡された移動。MoveSelf が実行する
        IReadOnlyList<MotionStep> Motion { get; }
        // 発射方向。移動によって使う/使わないが異なる。敵には無い (ゼロ)
        Vector2 Direction { get; }
        // 発射時刻で変わる移動 (頂点の揺らぎなど) に渡す時刻。敵は動き出してからの秒数、弾は撃った時点の撃ち手の時刻
        float AttackTime { get; }
        bool IsAlive { get; }
        // 出すたびに変わる番号。使い回された敵・弾を、前に出したものと区別する
        int Serial { get; }
        // 自分を出した敵・弾 (ステージが出したなら null)。合図の宛先「自分が出したもの」の判定に使う
        IAgent Owner { get; }
        int OwnerSerial { get; }
        string Group { get; }
        SignalInbox Signals { get; }
        void Despawn();
        void SetDespawnBehavior(BehaviorTreeAsset tree, DespawnReason reasons);
    }
}
