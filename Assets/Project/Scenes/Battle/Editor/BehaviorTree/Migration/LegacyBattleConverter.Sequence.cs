using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Project.Scenes.Battle.Scripts.BehaviorTree;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Model.Attack;
using Project.Scenes.Battle.Scripts.Model.ExitCondition;
using Project.Scenes.Battle.Scripts.Model.Movement;
using Project.Scenes.Battle.Scripts.Presenter.Entity;
using Project.Scenes.Battle.Scripts.Runtime.Motion;
using Project.Scripts.Model;
using UnityEditor;
using UnityEngine;

namespace Project.Scenes.Battle.Editor.BehaviorTree.Migration
{
    public sealed partial class LegacyBattleConverter
    {
        public BattleStageAsset ConvertSequence(BattleSequenceAsset sequence)
        {
            var isBoss = sequence.Situation == BattleSituation.Boss;
            var bossShooter = isBoss ? Shooter.From(sequence.BossPrefab) : null;
            // フェーズ通知を使うのは進行ゲージを持つボスだけ
            emitPhaseMarkers = isBoss && sequence.BossPrefab != null
                && sequence.BossPrefab.GetComponentInChildren<BossPhaseProgressGauge>(true) != null;

            var nodes = new List<NodeDefinition>();
            var groupIndex = 0;
            foreach (var group in sequence.SequenceGroups)
            {
                nodes.Add(WithComment(ConvertGroup(group, isBoss, bossShooter), $"Group {groupIndex++}"));
            }

            var stage = CreateAsset<BattleStageAsset>("", $"{sequence.name}_BT");
            Set(stage, "situation", sequence.Situation);

            var tree = CreateTree("Stage", $"{sequence.name}_Tree", Sequence(nodes));
            if (UsesWind(sequence))
            {
                // 風を使うステージにだけ風のサービスを宣言する。シーケンス全体の風は開始時の風になる
                var wind = new Project.Scenes.Battle.Scripts.Runtime.WindServiceDefinition();
                Set(wind, "initial", CopyWind(sequence.Wind));
                Set(stage, "services", new List<Project.Scenes.Battle.Scripts.Runtime.StageServiceDefinition> { wind });
            }
            if (isBoss)
            {
                Set(stage, "bossPrefab", sequence.BossPrefab);
                Set(stage, "bossSpawnPosition", sequence.BossSpawnPosition);
                Set(stage, "bossEntrance", ConvertSteps(sequence.BossEntranceMovement));
                Set(stage, "bossTree", tree);
            }
            else
            {
                Set(stage, "tree", tree);
            }
            EditorUtility.SetDirty(stage);
            return stage;
        }

        bool emitPhaseMarkers;

        NodeDefinition ConvertGroup(SequenceGroup group, bool isBoss, Shooter bossShooter)
        {
            var phases = group.Phases.Where(p => p != null).ToList();

            if (group.RandomPick && phases.Count > 0)
            {
                var interlude = group.Interlude;
                var options = phases.Select(p =>
                {
                    NodeDefinition after = null;
                    if (interlude != null)
                    {
                        var def = p.InterludeBuilderOverride != null
                            ? interlude.WithTimelineBuilder(p.InterludeBuilderOverride, p.InterludeTimeLimitOverride)
                            : interlude;
                        after = WithComment(ConvertPhase(def, isBoss, bossShooter), "Interlude");
                    }
                    return Sequence(new NodeDefinition[] { ConvertPhase(p, isBoss, bossShooter), after });
                }).ToList();

                var random = WithChildren(new RandomNodeDefinition(), options);
                NodeDefinition iteration = random;
                if (group.EndBossHpPercent > 0f)
                {
                    // 旧実装はボス HP を周回の区切りでのみ判定していた
                    var inverter = new InverterNodeDefinition();
                    inverter.SetChildren(new NodeDefinition[] { Check(HpBelow(isBoss ? HpTarget.Self : HpTarget.Boss, group.EndBossHpPercent)) });
                    iteration = Sequence(new NodeDefinition[] { inverter, random });
                }

                var loop = Loop(group.Loop ? group.LoopCount : 1, iteration);
                var safe = new ForceSuccessNodeDefinition();
                safe.SetChildren(new NodeDefinition[] { loop });
                return safe;
            }

            var body = Sequence(phases.Select(p => ConvertPhase(p, isBoss, bossShooter)));
            return group.Loop ? Loop(group.LoopCount, body) : body;
        }

        NodeDefinition ConvertPhase(BattlePhaseDefinition phase, bool isBoss, Shooter bossShooter)
        {
            var normal = ConvertPhaseVariant(phase, phase.TimelineBuilder, isBoss, bossShooter);
            NodeDefinition core = normal;

            if (isBoss && phase.TimelineBuilderStrong != null)
            {
                var strong = Sequence(new[]
                {
                    Check(new NormalHpDepletedConditionDefinition()),
                    new EnterStrongModeNodeDefinition(),
                    WithComment(ConvertPhaseVariant(phase, phase.TimelineBuilderStrong, true, bossShooter), "Strong"),
                });
                core = WithChildren(new SelectorNodeDefinition(), new NodeDefinition[] { strong, normal });
            }

            var body = WithWind(phase.WindOverride, core);
            return WithComment(Sequence(new[] { emitPhaseMarkers ? Marker(phase.PhaseId) : null, body }),
                phase.TimelineBuilder ? phase.TimelineBuilder.name : phase.PhaseId);
        }

        NodeDefinition ConvertPhaseVariant(BattlePhaseDefinition phase, BattleTimelineBuilderAsset builder, bool isBoss, Shooter bossShooter)
        {
            var content = BuilderContent(builder, isBoss);

            NodeDefinition actor = null;
            if (isBoss && builder != null)
            {
                var parts = new List<NodeDefinition>();
                if (builder.BossMovementPreset != null)
                {
                    var move = new SetMovementNodeDefinition();
                    Set(move, "preset", ConvertMotionPreset(builder.BossMovementPreset));
                    Set(move, "continueIfSame", true);
                    parts.Add(move);
                }
                if (builder.BossAttackPreset != null)
                {
                    parts.Add(WithComment(ConvertTimeline(builder.BossAttackPreset.CreateTimeline(), bossShooter, null), builder.BossAttackPreset.name));
                }
                if (builder.BossWindCue != null && builder.BossWindCue.enabled)
                {
                    var cueNode = new BossWindCueNodeDefinition();
                    var src = builder.BossWindCue;
                    Set(cueNode, "cue", new BossWindCueSettings
                    {
                        enabled = true, direction = src.direction, cueSeconds = src.cueSeconds,
                        playerSpeedRatio = src.playerSpeedRatio, bulletAcceleration = src.bulletAcceleration,
                        bulletDrag = src.bulletDrag, cueScale = src.cueScale,
                    });
                    parts.Insert(0, cueNode);
                }
                actor = Sequence(parts);
            }

            var exit = phase.ExitConditionConfig;
            if (exit is AllEnemiesDefeatedExitConditionConfig)
            {
                var waitClear = Sequence(new NodeDefinition[] { content, WaitUntil(new NoActiveEnemiesConditionDefinition()) });
                return actor == null ? waitClear : Parallel(new NodeDefinition[] { waitClear, actor }, ParallelPolicy.RequireOne);
            }

            var condition = ConvertExit(exit);
            var body = Parallel(new NodeDefinition[] { content, actor });
            return Until(condition, body, UntilChildCompletion.Hold);
        }

        ConditionDefinition ConvertExit(IExitConditionConfig exit)
        {
            switch (exit)
            {
                case TimeLimitExitConditionConfig:
                    return FrameElapsed(Frames(Get<float>(exit, "timeLimitSeconds")));
                case BgmPositionExitConditionConfig:
                    var bgm = new BgmSamplesConditionDefinition();
                    Set(bgm, "thresholdSamples", Get<int>(exit, "thresholdSamples"));
                    return bgm;
                case BossHpThresholdExitConditionConfig:
                    return HpBelow(HpTarget.Boss, Get<float>(exit, "hpThresholdPercent"));
                case AllEnemiesDefeatedExitConditionConfig:
                    return new NoActiveEnemiesConditionDefinition();
                case CompositeExitConditionConfig composite:
                    var children = composite.Conditions.Select(ConvertExit).Where(c => c != null).ToList();
                    if (composite.Mode == CompositeMode.Or)
                    {
                        var or = new OrConditionDefinition();
                        Set(or, "conditions", children);
                        return or;
                    }
                    var and = new AndConditionDefinition();
                    Set(and, "conditions", children);
                    return and;
                default:
                    Warn($"未対応の終了条件: {exit?.GetType().Name ?? "null"}");
                    return new ConstantConditionDefinition();
            }
        }

        // 旧 Unity Timeline のトラックを、開始時刻つきのノードに置き換える
        NodeDefinition BuilderContent(BattleTimelineBuilderAsset builder, bool worldSpace)
        {
            var items = new List<NodeDefinition>();
            if (builder == null) return Timeline(items);

            foreach (var track in Get<IList>(builder, "enemySpawnTracks") ?? new List<object>())
            {
                foreach (var clip in Get<IList>(track, "clips") ?? new List<object>())
                {
                    var prefab = Get<GameObject>(clip, "prefab");
                    if (prefab == null) continue;
                    var spawn = new SpawnNodeDefinition();
                    Set(spawn, "prefab", prefab);
                    Set(spawn, "position", (Vector2)Get<Vector3>(clip, "spawnPosition"));
                    Set(spawn, "worldSpace", worldSpace);
                    Set(spawn, "tracked", prefab.GetComponent<EnemyEntityPresenter>() != null);
                    Set(spawn, "behavior", EnemyBehavior(prefab,
                        Get<MovementPreset>(clip, "movementOverride"),
                        Get<AttackPreset>(clip, "attackOverride"),
                        Get<MovementPreset>(clip, "bulletMovementOverride")));
                    items.Add(Delay(Frames(Get<double>(clip, "time")), spawn));
                }
            }

            foreach (var track in Get<IList>(builder, "bulletClearTracks") ?? new List<object>())
            {
                foreach (var clip in Get<IList>(track, "clips") ?? new List<object>())
                {
                    var clear = new ClearBulletsNodeDefinition();
                    Set(clear, "clearEnemies", true);
                    items.Add(Delay(Frames(Get<double>(clip, "time")), clear));
                }
            }

            foreach (var track in Get<IList>(builder, "controlTracks") ?? new List<object>())
            {
                foreach (var clip in Get<IList>(track, "clips") ?? new List<object>())
                {
                    var prefab = Get<GameObject>(clip, "sourceObject");
                    if (prefab == null) continue;
                    var with = new WithPrefabNodeDefinition();
                    Set(with, "prefab", prefab);
                    Set(with, "position", prefab.transform.position);
                    with.SetChildren(new NodeDefinition[] { Wait(Frames(Get<double>(clip, "duration"))) });
                    items.Add(Delay(Frames(Get<double>(clip, "start")), with));
                }
            }

            foreach (var unused in new[] { "signalTracks", "animationTracks", "activationTracks", "audioTracks" })
            {
                var tracks = Get<IList>(builder, unused);
                if (tracks == null) continue;
                foreach (var track in tracks)
                {
                    var clips = Get<IList>(track, "clips");
                    if (clips != null && clips.Count > 0) Warn($"{builder.name}: 未移植のトラック {unused}");
                }
            }

            return Timeline(items);
        }

        static bool HasWind(BattleWindSettings wind) => wind != null && wind.intervals != null && wind.intervals.Count > 0;

        static bool UsesWind(BattleSequenceAsset sequence)
        {
            if (HasWind(sequence.Wind)) return true;
            foreach (var group in sequence.SequenceGroups)
            {
                var phases = group.Phases.Where(p => p != null).ToList();
                if (group.Interlude != null) phases.Add(group.Interlude);
                foreach (var phase in phases)
                {
                    if (phase.WindOverride != null) return true;
                    foreach (var builder in new[] { phase.TimelineBuilder, phase.TimelineBuilderStrong, phase.InterludeBuilderOverride })
                    {
                        if (builder != null && builder.BossWindCue != null && builder.BossWindCue.enabled) return true;
                    }
                }
            }
            return false;
        }

        static BattleWindSettings CopyWind(BattleWindSettings wind) => wind == null ? new BattleWindSettings() : new BattleWindSettings
        {
            enabled = wind.enabled,
            startSe = wind.startSe,
            playerSpeedRatio = wind.playerSpeedRatio,
            bulletAcceleration = wind.bulletAcceleration,
            bulletDrag = wind.bulletDrag,
            intervals = (wind.intervals ?? new List<WindInterval>()).Select(i => new WindInterval { direction = i.direction, seconds = i.seconds }).ToList(),
        };

        // フェーズで風を上書きする区間だけを囲む。設定が空なら何も挟まない
        static NodeDefinition WithWind(BattleWindSettings wind, NodeDefinition child)
        {
            if (!HasWind(wind)) return child;

            var node = new WithWindNodeDefinition();
            Set(node, "wind", CopyWind(wind));
            node.SetChildren(new[] { child });
            return node;
        }
    }
}
