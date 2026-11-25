using System;
using Project.Scripts.Extensions;
using Project.Scripts.Model;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("ステージの区切り。リトライするとこの Section の頭からやり直す。Situation は BGM・背景などシーン側の扱いに使う", "Flow")]
    public sealed class Section : DecoratorNode
    {
        [SerializeField] string label;
        [SerializeField] BattleSituation situation;

        public string Label => label;
        public BattleSituation Situation => situation;
        public override string Summary => $"{(string.IsNullOrEmpty(label) ? "(名前なし)" : label)} [{situation}]";

        protected override NodeStatus OnTick(BattleContext ctx) => Child.Tick(ctx);
        protected override void OnAbort(BattleContext ctx) => Child.Abort(ctx);
    }
}
