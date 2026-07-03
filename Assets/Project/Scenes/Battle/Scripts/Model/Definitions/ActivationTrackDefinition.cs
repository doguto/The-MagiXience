using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

namespace Project.Scenes.Battle.Scripts.Model
{
    [Serializable]
    public class ActivationTrackDefinition : TrackDefinitionBase<ActivationTrack, ActivationClipDefinition>
    {

    }

    [Serializable]
    public class ActivationClipDefinition : IClipDefinition<ActivationTrack>
    {
        [SerializeField] double start;
        [SerializeField] double duration = 1.0;

        public void Build(ActivationTrack track)
        {
            if (!track) return;

            var clip = track.CreateDefaultClip();
            clip.start = start;
            clip.duration = duration;
        }
    }
}
