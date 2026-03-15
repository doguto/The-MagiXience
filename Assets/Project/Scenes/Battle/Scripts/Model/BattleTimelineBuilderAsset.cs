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

        [Header("Boss")]
        [SerializeField] AttackPreset bossAttackPreset;
        [SerializeField] MovementPreset bossMovementPreset;

        public AttackPreset BossAttackPreset => bossAttackPreset;
        public MovementPreset BossMovementPreset => bossMovementPreset;

        public double LastEnemySpawnTime
        {
            get
            {
                double last = -1;
                foreach (var track in enemySpawnTracks)
                {
                    foreach (var clip in track.Clips)
                    {
                        if (clip.Time > last) last = clip.Time;
                    }
                }
                return last;
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
}
