using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

namespace Project.Scenes.Battle.Scripts.Model
{
    [Serializable]
    public class SignalTrackDefinition : TrackDefinitionBase<SignalTrack, SignalEmitterDefinition>
    {

    }

    [Serializable]
    public class SignalEmitterDefinition : IClipDefinition<SignalTrack>
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
}
