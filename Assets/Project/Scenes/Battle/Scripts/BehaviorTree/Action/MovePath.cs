using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("経路 (Trail Path) に沿ってこの敵を等速で動かす。移動が終わると完了", "Action/Move")]
    public sealed class MovePath : ActionNode
    {
        [SerializeField] BtObject<TrailPath> path;

        [NonSerialized] TrailPath resolvedPath;
        [NonSerialized] TrailPolyline line;
        [NonSerialized] int elapsed;

        public override string Summary => TrailSummary.Of(path);

        protected override void OnSetup(TreeScope scope) => resolvedPath = path.Resolve(scope);

        protected override void OnEnter(BattleContext ctx)
        {
            elapsed = 0;
            line = ctx.Self != null && resolvedPath != null ? new TrailPolyline(ctx.Self.Body.Position, resolvedPath) : null;
        }

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            if (line == null) return NodeStatus.Failure;

            elapsed++;
            var p = line.PositionAtProgress(elapsed, resolvedPath.Frames);
            var body = ctx.Self.Body;
            body.Position = new Vector3(p.x, p.y, body.Position.z);
            return elapsed >= resolvedPath.Frames ? NodeStatus.Success : NodeStatus.Running;
        }
    }
}
