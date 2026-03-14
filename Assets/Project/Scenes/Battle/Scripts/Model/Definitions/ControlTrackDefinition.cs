using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

namespace Project.Scenes.Battle.Scripts.Model
{
    [Serializable]
    public class ControlTrackDefinition : TrackDefinitionBase<ControlTrack, ControlClipDefinition>
    {

    }

    [Serializable]
    public class ControlClipDefinition : IClipDefinition<ControlTrack>
    {
        [SerializeField] double start;
        [SerializeField] double duration = 1.0;
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
