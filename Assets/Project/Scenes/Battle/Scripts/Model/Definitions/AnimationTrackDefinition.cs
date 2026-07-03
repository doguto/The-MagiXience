using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

namespace Project.Scenes.Battle.Scripts.Model
{
    [Serializable]
    public class AnimationTrackDefinition : TrackDefinitionBase<AnimationTrack, AnimationClipDefinition>
    {

    }

    [Serializable]
    public class AnimationClipDefinition : IClipDefinition<AnimationTrack>
    {
        [SerializeField] double start;
        [SerializeField] double duration;
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
