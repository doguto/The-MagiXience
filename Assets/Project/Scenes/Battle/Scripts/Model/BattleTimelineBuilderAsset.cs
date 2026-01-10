using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

namespace Project.Scenes.Battle.Scripts.Model
{
    [CreateAssetMenu(fileName = "BattleTimelineBuilder", menuName = "Battle/Timeline Builder")]
    public class BattleTimelineBuilderAsset : ScriptableObject
    {
        [SerializeField] List<SignalTrackDefinition> signalTracks = new();
        [SerializeField] List<AnimationTrackDefinition> animationTracks = new();

        public TimelineAsset BuildTimeline()
        {
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = $"{name}_Runtime";
            timeline.hideFlags = HideFlags.DontSave;

            foreach (var signalTrackDef in signalTracks)
            {
                if (string.IsNullOrEmpty(signalTrackDef.TrackName)) continue;
                var track = timeline.CreateTrack<SignalTrack>(null, signalTrackDef.TrackName);
                signalTrackDef.Build(track);
            }

            foreach (var animationTrackDef in animationTracks)
            {
                if (string.IsNullOrEmpty(animationTrackDef.TrackName)) continue;
                var track = timeline.CreateTrack<AnimationTrack>(null, animationTrackDef.TrackName);
                animationTrackDef.Build(track);
            }

            return timeline;
        }
    }

    [Serializable]
    public class SignalTrackDefinition
    {
        [SerializeField] string trackName;
        [SerializeField] List<SignalEmitterDefinition> emitters = new();

        public string TrackName => trackName;

        public void Build(SignalTrack track)
        {
            foreach (var emitterDef in emitters)
            {
                emitterDef.Build(track);
            }
        }
    }

    [Serializable]
    public class SignalEmitterDefinition
    {
        [SerializeField, Min(0)] double time;
        [SerializeField] SignalAsset signal;

        public void Build(SignalTrack track)
        {
            if (!track || !signal) return;

            var emitter = track.CreateMarker<SignalEmitter>(time);
            emitter.asset = signal;
        }
    }

    [Serializable]
    public class AnimationTrackDefinition
    {
        [SerializeField] string trackName;
        [SerializeField] List<AnimationClipDefinition> clips = new();

        public string TrackName => trackName;

        public void Build(AnimationTrack track)
        {
            foreach (var clipDef in clips)
            {
                clipDef.Build(track);
            }
        }
    }

    [Serializable]
    public class AnimationClipDefinition
    {
        [SerializeField, Min(0)] double start;
        [SerializeField, Min(0)] double duration;
        [SerializeField] AnimationClip animationClip;

        public void Build(AnimationTrack track)
        {
            if (!track || !animationClip) return;

            var clip = track.CreateClip<AnimationPlayableAsset>();
            clip.start = start;
            clip.duration = duration > 0 ? duration : animationClip.length;

            if (clip.asset is AnimationPlayableAsset playableAsset)
            {
                playableAsset.clip = animationClip;
            }
        }
    }
}
