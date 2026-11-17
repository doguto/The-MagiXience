using System;
using Project.Scenes.Battle.Scripts.Runtime;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    [Serializable, NodeInfo("予測線を出して待ってから、子のビームを撃つ。予測線とビームの位置・向きは開始時に 1 回だけ決めて揃える", "Decorator")]
    public sealed class WarningLine : DecoratorNode
    {
        [SerializeField, Tooltip("予測線の Prefab。未指定なら予測線を出さずに待つだけ")] GameObject prefab;
        [SerializeField, Min(0), Tooltip("子のビームを撃つまでのフレーム数")] int frames = 60;
        [SerializeField, Min(0), Tooltip("予測線を表示するフレーム数。0 なら Frames と同じ")] int displayFrames;
        [SerializeField, Min(0f), Tooltip("予測線の太さ。0 なら Prefab の値")] float width;
        [SerializeField, Tooltip("ON: ビームを撃つまで Running (後ろのノードを待たせる)。OFF: 予測線を出したらすぐ完了し、ビームは予約で撃つ (途中で打ち切られても撃つ)")]
        bool waitForBeam = true;

        [NonSerialized] IBeam beam;
        [NonSerialized] BeamAim aim;
        [NonSerialized] int elapsed;
        [NonSerialized] bool childStarted;

        public override string Summary => (prefab ? prefab.name : "(予測線なし)") + $" / {frames}f" + (waitForBeam ? "" : " / 待たない");

        protected override void OnSetup(TreeScope scope)
        {
            beam = Child as IBeam;
            if (beam == null && Child is not EmptyNode) Debug.LogWarning($"[BehaviorTree] WarningLine の子がビームではありません: {Child.GetType().Name}");
        }

        protected override void OnEnter(BattleContext ctx)
        {
            elapsed = 0;
            childStarted = false;
            aim = beam?.CreateAim(ctx);
            if (aim == null || prefab == null) return;

            var display = (displayFrames > 0 ? displayFrames : frames) * BattleWorld.DeltaTime;
            var z = ctx.Self != null ? ctx.Self.Body.Position.z : 0f;
            foreach (var line in aim.Lines)
            {
                ctx.Services.Spawner?.Spawn(new ActorSpawn
                {
                    Prefab = prefab,
                    Position = new Vector3(line.Start.x, line.Start.y, z),
                    Rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(line.Direction.y, line.Direction.x) * Mathf.Rad2Deg),
                    BeamRange = line.Length,
                    BeamDuration = display,
                    BeamWidth = width,
                });
            }
        }

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            if (!waitForBeam)
            {
                // ビームのノードは撃つ瞬間に予約まで済ませて即完了するので、1 回 Tick すれば足りる
                var captured = aim;
                var child = Child;
                ctx.World.Schedule(frames, () =>
                {
                    if (captured != null) beam?.UseAim(captured);
                    child.Tick(ctx);
                }, ctx.Self);
                return NodeStatus.Success;
            }

            if (elapsed < frames)
            {
                elapsed++;
                return NodeStatus.Running;
            }
            if (!childStarted)
            {
                childStarted = true;
                if (aim != null) beam?.UseAim(aim);
            }
            return Child.Tick(ctx);
        }

        protected override void OnAbort(BattleContext ctx)
        {
            if (childStarted) Child.Abort(ctx);
        }
    }
}
