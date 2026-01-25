using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

namespace Project.Scenes.Battle.Scripts.Model
{
    public interface IClipDefinition<in TTrack> where TTrack : TrackAsset
    {
        void Build(TTrack track);
    }

    public abstract class TrackDefinitionBase<TTrack, TClipDefinition> where TTrack : TrackAsset
        where TClipDefinition : IClipDefinition<TTrack>
    {
        [SerializeField] string trackName;
        [SerializeField] List<TClipDefinition> clips = new();

        public string TrackName => trackName;

        public virtual void Build(TTrack track)
        {
            foreach (var clipDef in clips)
            {
                clipDef.Build(track);
            }
        }
    }
}
