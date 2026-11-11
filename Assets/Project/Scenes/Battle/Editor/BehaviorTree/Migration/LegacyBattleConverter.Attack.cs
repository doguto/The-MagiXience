using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Project.Scenes.Battle.Scripts.BehaviorTree;
using Project.Scenes.Battle.Scripts.Model.Attack;
using Project.Scenes.Battle.Scripts.Model.Attack.PhaseTrigger;
using Project.Scenes.Battle.Scripts.Model.Movement;
using Project.Scenes.Battle.Scripts.Presenter.Entity;
using Project.Scenes.Battle.Scripts.Runtime.Motion;
using Project.Scripts.Extensions;
using UnityEditor;
using UnityEngine;

namespace Project.Scenes.Battle.Editor.BehaviorTree.Migration
{
    public sealed partial class LegacyBattleConverter
    {
        // 旧実装で弾を撃つ側 (敵/ボス Prefab) が持っていた設定
        public sealed class Shooter
        {
            public string Name;
            public GameObject Prefab;
            public BulletPool[] Pools = Array.Empty<BulletPool>();
            public int Damage = 10;
            public GameObject[] SpawnPrefabs = Array.Empty<GameObject>();
            public int MaxHp = 1;

            public static Shooter From(GameObject prefab)
            {
                var s = new Shooter { Name = prefab ? prefab.name : "Stage", Prefab = prefab };
                if (prefab == null) return s;
                object presenter = prefab.GetComponent<BossEntityPresenter>();
                presenter ??= prefab.GetComponent<EnemyEntityPresenter>();
                if (presenter == null) return s;
                s.Pools = Get<BulletPool[]>(presenter, "bulletPools") ?? Array.Empty<BulletPool>();
                s.Damage = Get<int>(presenter, "bulletDamage");
                s.SpawnPrefabs = Get<GameObject[]>(presenter, "enemySpawnPrefabs") ?? Array.Empty<GameObject>();
                s.MaxHp = Get<int>(presenter, "maxHp");
                return s;
            }
        }

        // ---------------- 敵 1 体ぶんのツリー ----------------

        public BehaviorTreeAsset EnemyBehavior(GameObject prefab, MovementPreset movementOverride, AttackPreset attackOverride, MovementPreset bulletMovementOverride)
        {
            var presenter = prefab ? prefab.GetComponent<EnemyEntityPresenter>() : null;
            if (presenter == null) return null;

            var key = (prefab, movementOverride, attackOverride, bulletMovementOverride);
            if (cache.TryGetValue(key, out var cached)) return (BehaviorTreeAsset)cached;

            var shooter = Shooter.From(prefab);

            var movementNode = new SetMovementNodeDefinition();
            var movement = movementOverride ?? Get<MovementPreset>(presenter, "movementPreset");
            if (movement != null) Set(movementNode, "preset", ConvertMotionPreset(movement));
            else Set(movementNode, "steps", ConvertSteps(Get<IEnumerable<IMovementStep>>(presenter, "movementSteps")));

            AttackTimeline timeline = attackOverride != null ? attackOverride.CreateTimeline()
                : Get<AttackPreset>(presenter, "attackPreset") is { } preset ? preset.CreateTimeline()
                : Get<AttackTimeline>(presenter, "attackTimeline");

            var death = Get<AttackTimelineEntry>(presenter, "deathAttackEntry");
            if (death?.signal != null) Warn($"{prefab.name}: 死亡時攻撃は未移植 ({death.signal.GetType().Name})");

            var attack = timeline != null ? ConvertTimeline(timeline, shooter, bulletMovementOverride) : null;
            var root = Sequence(new NodeDefinition[] { movementNode, attack });

            var name = prefab.name
                + (movementOverride ? $"_M-{movementOverride.name}" : "")
                + (attackOverride ? $"_A-{attackOverride.name}" : "")
                + (bulletMovementOverride ? $"_B-{bulletMovementOverride.name}" : "");
            var tree = CreateTree("Enemy", $"{prefix}_{name}", root);
            cache[key] = tree;
            return tree;
        }

        // ---------------- タイムライン ----------------

        public NodeDefinition ConvertTimeline(AttackTimeline timeline, Shooter shooter, MovementPreset inheritedBullet)
        {
            var phases = Get<IList>(timeline, "phases")?.Cast<AttackPhase>().ToList() ?? new List<AttackPhase>();
            var nodes = new List<NodeDefinition>();
            for (var i = 0; i < phases.Count; i++)
            {
                var body = ConvertPhaseBody(phases[i], shooter, inheritedBullet);
                var isLast = i == phases.Count - 1;
                var trigger = isLast ? null : ConvertTrigger(Get<IAttackPhaseTriggerConfig>(phases[i], "nextPhaseTrigger"), shooter);
                if (!isLast && trigger == null)
                {
                    // 次へ進む条件が無いフェーズは、旧実装でもそこで止まり続ける
                    nodes.Add(WithComment(Sequence(new NodeDefinition[] { body, WaitUntil(new ConstantConditionDefinition()) }), $"Phase {i}"));
                    break;
                }
                nodes.Add(WithComment(isLast ? body : Until(trigger, body, UntilChildCompletion.Hold), $"Phase {i}"));
            }
            return Sequence(nodes);
        }

        NodeDefinition ConvertPhaseBody(AttackPhase phase, Shooter shooter, MovementPreset inheritedBullet)
        {
            var entries = Get<IList>(phase, "entries")?.Cast<AttackTimelineEntry>().Where(e => e != null).ToList() ?? new List<AttackTimelineEntry>();

            if (!Get<bool>(phase, "loop"))
            {
                return Timeline(entries.Select(e => Delay(Frames(e.time), ConvertEntry(e, shooter, inheritedBullet))));
            }

            var loopStart = Get<float>(phase, "loopStart");
            var loopEnd = Get<float>(phase, "loopEnd");
            var cycle = Mathf.Max(0.01f, Get<float>(phase, "cycleDuration"));
            var cycleFrames = Mathf.Max(1, Frames(cycle));

            NodeDefinition looped;
            if (entries.All(e => e.time < cycle))
            {
                var oneCycle = Timeline(entries.Select(e => Delay(Frames(e.time), ConvertEntry(e, shooter, inheritedBullet))), cycleFrames);
                looped = Loop(0, oneCycle);
            }
            else
            {
                // 周期より後ろに置かれたエントリがある場合は、周期をまたいで重なるため全周期を展開する
                var cycles = Mathf.FloorToInt((loopEnd - loopStart) / cycle);
                var all = new List<NodeDefinition>();
                for (var c = 0; c <= cycles; c++)
                {
                    foreach (var e in entries)
                    {
                        var t = c * cycle + e.time;
                        if (loopStart + t > loopEnd) continue;
                        all.Add(Delay(Frames(t), ConvertEntry(e, shooter, inheritedBullet)));
                    }
                }
                looped = Timeline(all);
            }

            // 旧実装は loopEnd を過ぎた発射を捨てる
            var body = Sequence(new NodeDefinition[] { loopStart > 0f ? Wait(Frames(loopStart)) : null, looped });
            return Until(FrameElapsed(Frames(loopEnd) + 1), body, UntilChildCompletion.Finish);
        }

        ConditionDefinition ConvertTrigger(IAttackPhaseTriggerConfig trigger, Shooter shooter)
        {
            switch (trigger)
            {
                case HpPercentThresholdPhaseTriggerConfig:
                    return HpBelow(HpTarget.Self, Get<float>(trigger, "hpThresholdPercent"));
                case HpThresholdPhaseTriggerConfig:
                    var hp = Get<int>(trigger, "hpThreshold");
                    return HpBelow(HpTarget.Self, shooter.MaxHp > 0 ? 100f * hp / shooter.MaxHp : 0f);
                case TimeElapsedPhaseTriggerConfig:
                    return FrameElapsed(Frames(Get<float>(trigger, "delaySeconds")));
                case null:
                    return null;
                default:
                    Warn($"未対応のフェーズ切替条件: {trigger.GetType().Name}");
                    return null;
            }
        }

        // ---------------- エントリ ----------------

        NodeDefinition ConvertEntry(AttackTimelineEntry entry, Shooter shooter, MovementPreset inheritedBullet)
        {
            var bulletMotion = entry.bulletMovementOverride != null ? entry.bulletMovementOverride : inheritedBullet;
            var signal = entry.signal;
            switch (signal)
            {
                case null:
                    return null;
                case PresetAttackSignal:
                    return ConvertPresetSignal(signal, shooter, bulletMotion);
                case EnemySpawnAttackSignal or LineEnemySpawnAttackSignal or NWayEnemySpawnAttackSignal:
                    return PerSourceIndex(entry, idx => ConvertSpawn(entry, signal, shooter, idx, bulletMotion));
                case ChangeMovementSignal:
                    var setMove = new SetMovementNodeDefinition();
                    var step = ConvertStep(Get<IMovementStep>(signal, "movement"));
                    Set(setMove, "steps", step != null ? new List<MotionStep> { step } : new List<MotionStep>());
                    return setMove;
                case DespawnSignal:
                    return new DespawnNodeDefinition();
                case SetInvincibleSignal:
                    var inv = new SetInvincibleNodeDefinition();
                    Set(inv, "invincible", Get<bool>(signal, "enabled"));
                    return inv;
                case BeamAttackSignal:
                    return ConvertBeam(entry, signal, shooter);
                case SpriteBeamAttackSignal:
                    return ConvertSpriteBeam(signal, shooter);
                case MoveTrailFanBulletSignal:
                    return PerSourceIndex(entry, idx => ConvertMoveTrail(entry, signal, shooter, idx, bulletMotion));
                default:
                    var pattern = ConvertPattern(signal);
                    if (pattern == null)
                    {
                        Warn($"{shooter.Name}: 未対応の攻撃 {signal.GetType().Name}");
                        return null;
                    }
                    return PerSourceIndex(entry, idx => Fire(entry, pattern, shooter, idx, bulletMotion));
            }
        }

        // 旧 RandomSourceIndex は発射ごとに弾/召喚の種類を抽選していた
        static NodeDefinition PerSourceIndex(AttackTimelineEntry entry, Func<int, NodeDefinition> build)
        {
            if (entry.sourceIndexProvider is RandomSourceIndex random)
            {
                var min = Get<int>(random, "min");
                var max = Get<int>(random, "max");
                var options = Enumerable.Range(min, Math.Max(1, max - min + 1)).Select(build).ToList();
                var node = new RandomNodeDefinition();
                Set(node, "avoidRepeat", false);
                return WithChildren(node, options);
            }
            var index = entry.sourceIndexProvider is ConstantSourceIndex constant ? Get<int>(constant, "index") : 0;
            return build(index);
        }

        ShotPattern ConvertPattern(IAttackSignal signal)
        {
            switch (signal)
            {
                case SingleBulletSignal:
                    return new SingleShot();
                case NWaySignal:
                    var nway = new NWayShot();
                    CopyByName(signal, nway);
                    Set(nway, "startDelayFrames", Frames(Get<float>(signal, "startDelay")));
                    return nway;
                case SequentialFanBulletSignal:
                    var fan = new SequentialFanShot();
                    CopyByName(signal, fan);
                    Set(fan, "spawnIntervalFrames", Frames(Get<float>(signal, "spawnInterval")));
                    Set(fan, "releaseDelayFrames", Frames(Get<float>(signal, "releaseDelay")));
                    return fan;
                case SequentialCircleBulletSignal:
                    var circle = new SequentialCircleShot();
                    CopyByName(signal, circle);
                    Set(circle, "spawnIntervalFrames", Frames(Get<float>(signal, "spawnInterval")));
                    Set(circle, "releaseDelayFrames", Frames(Get<float>(signal, "releaseDelay")));
                    return circle;
                case ChargeSignal:
                    var charge = new ChargeShot();
                    CopyByName(signal, charge);
                    Set(charge, "startDelayFrames", Frames(Get<float>(signal, "startDelay")));
                    return charge;
                default:
                    return null;
            }
        }

        NodeDefinition Fire(AttackTimelineEntry entry, ShotPattern pattern, Shooter shooter, int sourceIndex, MovementPreset bulletMotion)
        {
            var fire = new FireNodeDefinition();
            Set(fire, "bullet", Bullet(shooter, sourceIndex, bulletMotion));
            Set(fire, "pattern", pattern);
            Set(fire, "direction", ConvertDirection(entry.directionProvider));
            Set(fire, "rotation", ConvertRotation(entry.rotationProvider));
            Set(fire, "muzzleOffset", Muzzle(shooter, sourceIndex));
            Set(fire, "se", entry.seType);
            return fire;
        }

        BulletSpec Bullet(Shooter shooter, int sourceIndex, MovementPreset bulletMotion)
        {
            var pool = Pool(shooter, sourceIndex);
            var prefab = pool != null ? Get<BulletEntityPresenter>(pool, "bulletPrefab") : null;
            if (prefab == null) Warn($"{shooter.Name}: 弾 Prefab がありません (index {sourceIndex})");
            return new BulletSpec
            {
                prefab = prefab,
                damage = shooter.Damage,
                motionPreset = bulletMotion != null ? ConvertMotionPreset(bulletMotion) : DefaultBulletMotion(prefab),
            };
        }

        BulletPool Pool(Shooter shooter, int index)
        {
            if (shooter.Pools.Length == 0) return null;
            return shooter.Pools[index >= 0 && index < shooter.Pools.Length ? index : 0];
        }

        Vector2 Muzzle(Shooter shooter, int index)
        {
            var pool = Pool(shooter, index);
            return pool != null && shooter.Prefab != null && pool.transform != shooter.Prefab.transform
                ? (Vector2)(pool.transform.position - shooter.Prefab.transform.position)
                : Vector2.zero;
        }

        // 弾 Prefab に設定されていた移動を、弾ごとの既定の移動プリセットにする
        MotionPreset DefaultBulletMotion(BulletEntityPresenter prefab)
        {
            if (prefab == null) return null;
            if (cache.TryGetValue(prefab, out var cached)) return (MotionPreset)cached;

            var motion = CreateAsset<MotionPreset>("Motion", $"Bullet_{prefab.name}");
            motion.Steps.AddRange(ConvertSteps(Get<IEnumerable<IMovementStep>>(prefab, "movementSteps")));
            EditorUtility.SetDirty(motion);
            cache[prefab] = motion;
            return motion;
        }

        DirectionSource ConvertDirection(IDirectionProvider provider)
        {
            DirectionSource result = provider switch
            {
                AimDirectionConfig => new AimDirection(),
                ForwardDirectionConfig => new ForwardDirection(),
                ScreenEdgeDirectionConfig => new ScreenEdgeDirection(),
                FixedDirectionConfig => new FixedDirection(),
                null => new FixedDirection(),
                _ => null,
            };
            if (result == null)
            {
                Warn($"未対応の方向: {provider.GetType().Name}");
                return new FixedDirection();
            }
            if (provider != null) CopyByName(provider, result);
            return result;
        }

        RotationSource ConvertRotation(IRotationProvider provider)
        {
            RotationSource result = provider switch
            {
                AimRotationConfig => new AimRotation(),
                FixedRotationConfig => new FixedRotation(),
                OffsetRotationConfig => new OffsetRotation(),
                _ => new InheritRotation(),
            };
            if (provider != null) CopyByName(provider, result);
            return result;
        }

        // ---------------- プリセット参照 ----------------

        NodeDefinition ConvertPresetSignal(IAttackSignal signal, Shooter shooter, MovementPreset bulletMotion)
        {
            var preset = Get<AttackPreset>(signal, "preset");
            if (preset == null) return null;

            var sub = new SubTreeNodeDefinition { Tree = PresetEntriesTree(preset, shooter, bulletMotion) };
            var loop = Get<bool>(signal, "loop");
            var count = Get<int>(signal, "loopCount");
            var cycles = loop && count > 0 ? count : 1;
            if (cycles == 1) return sub;

            var cycle = Get<float>(signal, "cycleDuration");
            var copies = new List<NodeDefinition>();
            for (var c = 0; c < cycles; c++)
            {
                copies.Add(Delay(Frames(c * cycle), new SubTreeNodeDefinition { Tree = sub.Tree }));
            }
            return Timeline(copies);
        }

        // 旧 PresetAttackSignal は参照先プリセットの最初のフェーズのエントリだけを使っていた
        BehaviorTreeAsset PresetEntriesTree(AttackPreset preset, Shooter shooter, MovementPreset bulletMotion)
        {
            var key = (preset, shooter.Prefab, bulletMotion);
            if (cache.TryGetValue(key, out var cached)) return (BehaviorTreeAsset)cached;

            var timeline = preset.CreateTimeline();
            var phases = Get<IList>(timeline, "phases");
            var entries = phases != null && phases.Count > 0
                ? Get<IList>(phases[0], "entries")?.Cast<AttackTimelineEntry>().Where(e => e != null).ToList()
                : new List<AttackTimelineEntry>();

            var root = Timeline(entries.Select(e => Delay(Frames(e.time), ConvertEntry(e, shooter, bulletMotion))));
            var tree = CreateTree("Attack", $"{prefix}_{preset.name}_{shooter.Name}", root);
            cache[key] = tree;
            return tree;
        }

        // ---------------- 召喚 ----------------

        NodeDefinition ConvertSpawn(AttackTimelineEntry entry, IAttackSignal signal, Shooter shooter, int index, MovementPreset bulletMotion)
        {
            var prefab = shooter.SpawnPrefabs.Length == 0 ? null : shooter.SpawnPrefabs[index >= 0 && index < shooter.SpawnPrefabs.Length ? index : 0];
            if (prefab == null)
            {
                Warn($"{shooter.Name}: 召喚 Prefab がありません (index {index})");
                return null;
            }

            var movement = Get<MovementPreset>(signal, "movementOverride");
            var node = new SpawnNodeDefinition();
            Set(node, "prefab", prefab);
            Set(node, "behavior", EnemyBehavior(prefab, movement, null, bulletMotion));
            Set(node, "rotation", ConvertRotation(entry.rotationProvider));
            Set(node, "direction", ConvertDirection(entry.directionProvider));

            switch (signal)
            {
                case EnemySpawnAttackSignal:
                    var single = new SingleLayout();
                    Set(single, "offset", Get<Vector2>(signal, "offset"));
                    Set(node, "layout", single);
                    Set(node, "beamWidth", Get<float>(signal, "width"));
                    break;
                case LineEnemySpawnAttackSignal:
                    var line = new LineLayout();
                    CopyByName(signal, line);
                    Set(node, "layout", line);
                    break;
                case NWayEnemySpawnAttackSignal:
                    var fan = new FanLayout();
                    CopyByName(signal, fan);
                    Set(node, "layout", fan);
                    break;
            }
            return node;
        }

        // ---------------- ビーム ----------------

        NodeDefinition ConvertBeam(AttackTimelineEntry entry, IAttackSignal signal, Shooter shooter)
        {
            var line = Get<BeamLine>(signal, "line");
            var preset = Get<AttackPreset>(signal, "bulletPreset");
            var timeline = preset != null ? preset.CreateTimeline() : null;
            var phases = timeline != null ? Get<IList>(timeline, "phases") : null;
            var inner = phases != null && phases.Count > 0
                ? Get<IList>(phases[0], "entries")?.Cast<AttackTimelineEntry>().Where(e => e?.signal != null).ToList()
                : new List<AttackTimelineEntry>();

            var nodes = new List<NodeDefinition>();
            var warnIndex = Get<int>(signal, "warningSourceIndex");
            var first = true;
            foreach (var e in inner)
            {
                var pattern = ConvertPattern(e.signal);
                if (pattern == null)
                {
                    Warn($"{shooter.Name}: ビーム内の未対応の攻撃 {e.signal.GetType().Name}");
                    continue;
                }
                var idx = e.sourceIndexProvider is ConstantSourceIndex c ? Get<int>(c, "index") : 0;
                var beam = new BulletBeamNodeDefinition();
                Set(beam, "line", new BeamSegment { start = line.Start, end = line.End });
                Set(beam, "bullet", Bullet(shooter, idx, e.bulletMovementOverride));
                Set(beam, "pattern", pattern);
                Set(beam, "shotCount", Get<int>(signal, "shotCount"));
                Set(beam, "shotIntervalFrames", Mathf.Max(1, Frames(Get<float>(signal, "shotInterval"))));
                // 警告線はビーム 1 本につき 1 回だけ出す
                Set(beam, "showWarning", first && Get<bool>(signal, "showWarning"));
                Set(beam, "warningPrefab", SpawnPrefab(shooter, warnIndex));
                Set(beam, "warningFrames", Frames(Get<float>(signal, "warningDuration")));
                Set(beam, "warningWidth", Get<float>(signal, "warningWidth"));
                Set(beam, "se", e.seType != SeType.None ? e.seType : entry.seType);
                nodes.Add(Delay(Frames(e.time), beam));
                first = false;
            }
            return Timeline(nodes);
        }

        NodeDefinition ConvertSpriteBeam(IAttackSignal signal, Shooter shooter)
        {
            var line = Get<BeamLine>(signal, "line");
            var beam = new SpriteBeamNodeDefinition();
            Set(beam, "line", new BeamSegment { start = line.Start, end = line.End });
            Set(beam, "isRelative", Get<bool>(signal, "isRelative"));
            Set(beam, "bodyPrefab", SpawnPrefab(shooter, Get<int>(signal, "bodySourceIndex")));
            Set(beam, "bodyDuration", Get<float>(signal, "bodyDuration"));
            Set(beam, "showWarning", Get<bool>(signal, "showWarning"));
            Set(beam, "warningPrefab", SpawnPrefab(shooter, Get<int>(signal, "warningSourceIndex")));
            Set(beam, "warningFrames", Frames(Get<float>(signal, "warningDuration")));
            return beam;
        }

        GameObject SpawnPrefab(Shooter shooter, int index) =>
            shooter.SpawnPrefabs.Length == 0 ? null : shooter.SpawnPrefabs[index >= 0 && index < shooter.SpawnPrefabs.Length ? index : 0];

        NodeDefinition ConvertMoveTrail(AttackTimelineEntry entry, IAttackSignal signal, Shooter shooter, int index, MovementPreset bulletMotion)
        {
            var node = new MoveTrailFanNodeDefinition();
            CopyByName(signal, node);
            Set(node, "moveEase", new MotionEase { easeValue = Get<int>(signal, "moveEaseValue") });
            Set(node, "releaseDelayFrames", Frames(Get<float>(signal, "releaseDelay")));
            Set(node, "bullet", Bullet(shooter, index, bulletMotion));
            Set(node, "direction", ConvertDirection(entry.directionProvider));
            Set(node, "rotation", ConvertRotation(entry.rotationProvider));
            Set(node, "se", entry.seType);
            return node;
        }
    }
}
