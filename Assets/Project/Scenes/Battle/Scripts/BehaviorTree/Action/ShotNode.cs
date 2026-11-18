using System;
using System.Collections.Generic;
using Project.Scripts.Extensions;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 弾を撃つノードの共通部分。撃ち方 (本数・角度・並べ方) は派生クラスが 1 つずつ持ち、グラフ上はノードの種類で区別できる
    [Serializable]
    public abstract class ShotNode : ActionNode
    {
        [SerializeField] BulletConfig bullet = new();
        [SerializeReference, SubclassSelector] DirectionSource direction = new FixedDirection();
        [SerializeReference, SubclassSelector] RotationSource rotation = new InheritRotation();
        [SerializeField, Tooltip("撃つ敵の位置からのずれ")] Vector2 muzzleOffset;
        [SerializeField] SeType se;

        [NonSerialized] BulletConfig resolvedBullet;
        [NonSerialized] ShotPattern resolvedPattern;
        [NonSerialized] List<Shot> shots;

        protected abstract ShotPattern Pattern { get; }

        public override string Summary => $"{Pattern?.Summary ?? "?"} / {direction?.Summary ?? "?"} / {bullet}";

        protected override void OnSetup(TreeScope scope)
        {
            resolvedBullet = bullet.Resolve(scope);
            resolvedPattern = Pattern?.Resolve(scope);
            shots = new List<Shot>();
        }

        protected override NodeStatus OnTick(BattleContext ctx)
        {
            var e = new EmitContext(ctx, muzzleOffset, Scope);
            var dirSource = direction ?? new FixedDirection();
            var baseDirection = dirSource.GetDirection(e);
            var worldOrigin = dirSource.GetOrigin(e);
            var rot = rotation?.GetRotation(e) ?? Quaternion.identity;

            shots.Clear();
            resolvedPattern?.Build(baseDirection, shots);
            Emitter.PlaySe(ctx, se);

            foreach (var shot in shots)
            {
                var offset = shot.Offset;
                Func<EmitContext, Vector2> position = worldOrigin.HasValue
                    ? _ => worldOrigin.Value + offset
                    : at => at.Origin + offset;
                Emitter.Spawn(ctx, resolvedBullet, muzzleOffset, position, shot.Direction, rot, shot.SpawnDelayFrames, shot.StartDelayFrames);
            }
            return NodeStatus.Success;
        }
    }
}
