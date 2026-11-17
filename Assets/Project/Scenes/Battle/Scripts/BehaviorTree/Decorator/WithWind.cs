using System;
using System.Linq;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Runtime;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // バトル開始時は無風
    [Serializable, NodeInfo("子の実行中だけ風を吹かせる。終わると直前の風に戻る", "Decorator")]
    public sealed class WithWind : DecoratorNode
    {
        [SerializeField] BattleWindSettings wind = new() { enabled = true };

        [NonSerialized] WindService service;
        [NonSerialized] BattleWindSettings previous;

        public override string Summary => wind.intervals.Count == 0 ? "風 (未設定)"
            : string.Join(" → ", wind.intervals.Select(i => $"{i.direction} {i.seconds:0.#}s"));

        protected override void OnEnter(BattleContext ctx)
        {
            service = ctx.Stage?.Get<WindService>();
            if (service == null)
            {
                ctx.Stage?.WarnMissing<WindService>(nameof(WithWind));
                return;
            }
            previous = service.Current;
            service.Apply(wind);
        }

        protected override NodeStatus OnTick(BattleContext ctx) => Child.Tick(ctx);
        protected override void OnAbort(BattleContext ctx) => Child.Abort(ctx);

        protected override void OnExit(BattleContext ctx)
        {
            service?.Apply(previous);
            service = null;
            previous = null;
        }
    }
}
