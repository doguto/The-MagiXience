using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using Project.Scenes.Battle.Scripts.Model.Attack;
using Project.Scenes.Battle.Scripts.Model.Movement;

namespace Project.Scenes.Battle.Scripts.Model
{
    [CreateAssetMenu(fileName = "BattleTimelineBuilder", menuName = "Battle/Timeline Builder")]
    public class BattleTimelineBuilderAsset : ScriptableObject
    {
        [SerializeField] List<SignalTrackDefinition> signalTracks = new();
        [SerializeField] List<AnimationTrackDefinition> animationTracks = new();
        [SerializeField] List<ActivationTrackDefinition> activationTracks = new();
        [SerializeField] List<AudioTrackDefinition> audioTracks = new();
        [SerializeField] List<ControlTrackDefinition> controlTracks = new();
        [SerializeField] List<EnemySpawnTrackDefinition> enemySpawnTracks = new();
        [SerializeField] List<BulletClearTrackDefinition> bulletClearTracks = new();

        [Header("Boss")]
        [SerializeField] AttackPreset bossAttackPreset;
        [SerializeField] MovementPreset bossMovementPreset;
        [SerializeField] BossWindCueSettings bossWindCue = new();

        public AttackPreset BossAttackPreset => bossAttackPreset;
        public MovementPreset BossMovementPreset => bossMovementPreset;
        public BossWindCueSettings BossWindCue => bossWindCue;
 
        public int TotalEnemySpawnCount
        {
            get
            {
                int count = 0;
                foreach (var track in enemySpawnTracks)
                {
                    count += track.Clips.Count;
                }
                return count;
            }
        }

        public TimelineAsset BuildTimeline()
        {
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = $"{name}_Runtime";
            timeline.hideFlags = HideFlags.DontSave;

            BuildTracks<SignalTrack, SignalEmitterDefinition, SignalTrackDefinition>(timeline, signalTracks);
            BuildTracks<AnimationTrack, AnimationClipDefinition, AnimationTrackDefinition>(timeline, animationTracks);
            BuildTracks<ActivationTrack, ActivationClipDefinition, ActivationTrackDefinition>(timeline, activationTracks);
            BuildTracks<AudioTrack, AudioClipDefinition, AudioTrackDefinition>(timeline, audioTracks);
            BuildTracks<ControlTrack, ControlClipDefinition, ControlTrackDefinition>(timeline, controlTracks);
            BuildTracks<SignalTrack, EnemySpawnDefinition, EnemySpawnTrackDefinition>(timeline, enemySpawnTracks);
            BuildTracks<SignalTrack, BulletClearDefinition, BulletClearTrackDefinition>(timeline, bulletClearTracks);

            return timeline;
        }

        private void BuildTracks<TTrack, TClipDefinition, TDefinition>(TimelineAsset timeline, List<TDefinition> trackDefinitions)
            where TTrack : TrackAsset, new()
            where TClipDefinition : IClipDefinition<TTrack>
            where TDefinition : TrackDefinitionBase<TTrack, TClipDefinition>
        {
            foreach (var trackDef in trackDefinitions)
            {
                if (string.IsNullOrEmpty(trackDef.TrackName)) continue;
                var track = timeline.CreateTrack<TTrack>(null, trackDef.TrackName);
                trackDef.Build(track);
            }
        }
    }

    [Serializable]
    public class BossWindCueSettings
    {
        public bool enabled;
        public WindDirection direction = WindDirection.Up;
        [Min(0f), Tooltip("腕上げ中は前の風を保ち、この秒数後に切り替える。")]
        public float cueSeconds = 0.9f;
        [Range(0f, 1f)] public float playerSpeedRatio = 0.2f;
        [Min(0f)] public float bulletAcceleration = 1.5f;
        [Min(0f)] public float bulletDrag = 0.25f;
        [Tooltip("腕上げ中のスケール変化率。移動はMovementPreset側で設定する。")]
        public Vector2 cueScale = new(-0.04f, 0.08f);

        public BattleWindSettings CreateWind(WindDirection previousDirection)
        {
            var firstDirection = previousDirection == WindDirection.None ? direction : previousDirection;
            var intervals = new List<WindInterval>();
            if (cueSeconds > 0f)
                intervals.Add(new WindInterval { direction = firstDirection, seconds = cueSeconds });
            intervals.Add(new WindInterval { direction = direction, seconds = 3600f });
            return new BattleWindSettings
            {
                enabled = true, playerSpeedRatio = playerSpeedRatio,
                bulletAcceleration = bulletAcceleration, bulletDrag = bulletDrag, intervals = intervals
            };
        }
    }
}
