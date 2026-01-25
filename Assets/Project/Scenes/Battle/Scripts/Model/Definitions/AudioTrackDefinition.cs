using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

namespace Project.Scenes.Battle.Scripts.Model
{
    [Serializable]
    public class AudioTrackDefinition : TrackDefinitionBase<AudioTrack, AudioClipDefinition>
    {

    }

    [Serializable]
    public class AudioClipDefinition : IClipDefinition<AudioTrack>
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
}
