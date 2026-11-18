using System;
using Project.Scenes.Battle.Scripts.Runtime;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable]
    public struct BeamSegment
    {
        public Vector2 start;
        public Vector2 end;

        public Vector2 Direction => (end - start).sqrMagnitude > 0f ? (end - start).normalized : Vector2.left;
        public float Length => Vector2.Distance(start, end);
        public Quaternion Rotation => Quaternion.Euler(0f, 0f, Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg);
    }

    [Serializable, NodeInfo("画像のビーム本体を出す。予測線は WarningLine で包んで出す", "Action/Attack")]
    public sealed class SpriteBeam : ActionNode, IBeam
    {
        [SerializeField] BeamSegment line = new() { start = new Vector2(5f, 0f), end = new Vector2(-9f, 0f) };
        [SerializeField, Tooltip("ON: 線分を撃つ敵の位置からの相対にする (狙いを決めた時点の位置で固定)")] bool isRelative;
        [SerializeField] GameObject bodyPrefab;
        [SerializeField, Min(0f), Tooltip("秒")] float bodyDuration = 1f;

        [NonSerialized] BeamAim presetAim;

        public override string Summary => $"画像ビーム / {bodyDuration:0.##}s";

        sealed class Aim : BeamAim
        {
            public BeamSegment Segment;
        }

        public BeamAim CreateAim(BattleContext ctx)
        {
            var segment = line;
            if (isRelative && ctx.Self != null)
            {
                var origin = (Vector2)ctx.Self.Body.Position;
                segment.start += origin;
                segment.end += origin;
            }
            var aim = new Aim { Segment = segment };
            aim.Lines.Add(new BeamLine { Start = segment.start, Direction = segment.Direction, Length = segment.Length });
            return aim;
        }

        public void UseAim(BeamAim aim) => presetAim = aim;

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            var aim = presetAim as Aim ?? (Aim)CreateAim(ctx);
            presetAim = null;
            ctx.Services.Spawner?.Spawn(new EnemySpawn
            {
                Prefab = bodyPrefab,
                Position = aim.Segment.start,
                Rotation = aim.Segment.Rotation,
                BeamRange = aim.Segment.Length,
                BeamDuration = bodyDuration,
            });
            return NodeStatus.Success;
        }
    }
}
