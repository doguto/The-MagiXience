using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 旧 Control トラック
    [Serializable, NodeInfo("子の実行中だけ Prefab を出しておく (障害物・演出など)", "Decorator")]
    public sealed class WithPrefab : DecoratorNode
    {
        [SerializeField] GameObject prefab;
        [SerializeField] Vector3 position;

        [NonSerialized] GameObject instance;

        public override string Summary => prefab ? prefab.name : "(未設定)";

        protected override void OnEnter(BattleContext ctx)
        {
            if (prefab != null) instance = UnityEngine.Object.Instantiate(prefab, position, prefab.transform.rotation);
        }

        protected override NodeStatus OnTick(BattleContext ctx) => Child.Tick(ctx);
        protected override void OnAbort(BattleContext ctx) => Child.Abort(ctx);

        protected override void OnExit(BattleContext ctx)
        {
            if (instance != null) UnityEngine.Object.Destroy(instance);
            instance = null;
        }
    }
}
