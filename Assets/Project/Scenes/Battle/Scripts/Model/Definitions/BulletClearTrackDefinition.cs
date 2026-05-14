using System;
using UnityEngine;
using UnityEngine.Timeline;

namespace Project.Scenes.Battle.Scripts.Model
{
    [Serializable]
    public class BulletClearTrackDefinition : TrackDefinitionBase<SignalTrack, BulletClearDefinition>
    {

    }

    [Serializable]
    public class BulletClearDefinition : IClipDefinition<SignalTrack>
    {
        [SerializeField] double time;

        public double Time => time;

        public void Build(SignalTrack track)
        {
            if (!track) return;

            var signal = ScriptableObject.CreateInstance<BulletClearSignal>();
            signal.name = $"BulletClear_{time}";
            signal.hideFlags = HideFlags.DontSave;

            var emitter = track.CreateMarker<SignalEmitter>(time);
            emitter.asset = signal;
        }
    }
}
