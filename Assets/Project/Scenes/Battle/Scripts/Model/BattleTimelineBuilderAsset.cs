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
        [SerializeField] List<ActivationTrackDefinition> activationTracks = new();
        [SerializeField] List<AudioTrackDefinition> audioTracks = new();
        [SerializeField] List<ControlTrackDefinition> controlTracks = new();

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

            foreach (var activationTrackDef in activationTracks)
            {
                if (string.IsNullOrEmpty(activationTrackDef.TrackName)) continue;
                var track = timeline.CreateTrack<ActivationTrack>(null, activationTrackDef.TrackName);
                activationTrackDef.Build(track);
            }

            foreach (var audioTrackDef in audioTracks)
            {
                if (string.IsNullOrEmpty(audioTrackDef.TrackName)) continue;
                var track = timeline.CreateTrack<AudioTrack>(null, audioTrackDef.TrackName);
                audioTrackDef.Build(track);
            }

            foreach (var controlTrackDef in controlTracks)
            {
                if (string.IsNullOrEmpty(controlTrackDef.TrackName)) continue;
                var track = timeline.CreateTrack<ControlTrack>(null, controlTrackDef.TrackName);
                controlTrackDef.Build(track);
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

    [Serializable]
    public class ActivationTrackDefinition
    {
        [SerializeField] string trackName;
        [SerializeField] List<ActivationClipDefinition> clips = new();

        public string TrackName => trackName;

        public void Build(ActivationTrack track)
        {
            foreach (var clipDef in clips)
            {
                clipDef.Build(track);
            }
        }
    }

    [Serializable]
    public class ActivationClipDefinition
    {
        [SerializeField, Min(0)] double start;
        [SerializeField, Min(0)] double duration = 1.0;

        public void Build(ActivationTrack track)
        {
            if (!track) return;

            var clip = track.CreateDefaultClip();
            clip.start = start;
            clip.duration = duration;
        }
    }

    [Serializable]
    public class AudioTrackDefinition
    {
        [SerializeField] string trackName;
        [SerializeField] List<AudioClipDefinition> clips = new();

        public string TrackName => trackName;

        public void Build(AudioTrack track)
        {
            foreach (var clipDef in clips)
            {
                clipDef.Build(track);
            }
        }
    }

    [Serializable]
    public class AudioClipDefinition
    {
        [SerializeField, Min(0)] double start;
        [SerializeField, Min(0)] double duration;
        [SerializeField] AudioClip audioClip;

        public void Build(AudioTrack track)
        {
            if (!track || !audioClip) return;

            var clip = track.CreateClip<AudioPlayableAsset>();
            clip.start = start;
            clip.duration = duration > 0 ? duration : audioClip.length;

            if (clip.asset is AudioPlayableAsset playableAsset)
            {
                playableAsset.clip = audioClip;
            }
        }
    }

    [Serializable]
    public class ControlTrackDefinition
    {
        [SerializeField] string trackName;
        [SerializeField] List<ControlClipDefinition> clips = new();

        public string TrackName => trackName;

        public void Build(ControlTrack track)
        {
            foreach (var clipDef in clips)
            {
                clipDef.Build(track);
            }
        }
    }

    [Serializable]
    public class ControlClipDefinition
    {
        [SerializeField, Min(0)] double start;
        [SerializeField, Min(0)] double duration = 1.0;
        [SerializeField] GameObject sourceObject;

        public void Build(ControlTrack track)
        {
            if (!track || !sourceObject) return;

            var clip = track.CreateDefaultClip();
            clip.start = start;
            clip.duration = duration;

            // Set the prefab reference to ControlPlayableAsset
            if (clip.asset is ControlPlayableAsset controlAsset)
            {
                controlAsset.prefabGameObject = sourceObject;
            }
        }
    }
}
