using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Project.Scenes.Battle.Scripts.Presenter
{
    public class BattleTimelineBindingMap : MonoBehaviour
    {
        [SerializeField] List<BindingEntry> bindings = new();

        public void ApplyBindings(PlayableDirector director, TimelineAsset timeline)
        {
            if (!director || !timeline) return;

            foreach (var output in timeline.outputs)
            {
                var trackName = output.streamName;
                if (string.IsNullOrEmpty(trackName)) continue;

                var target = GetBinding(trackName);
                if (!target)
                {
                    continue;
                }

                director.SetGenericBinding(output.sourceObject, target);
            }
        }

        UnityEngine.Object GetBinding(string trackName)
        {
            foreach (var entry in bindings)
            {
                if (entry != null && entry.Matches(trackName))
                {
                    return entry.Target;
                }
            }

            return null;
        }

        [Serializable]
        class BindingEntry
        {
            [SerializeField] string trackName;
            [SerializeField] UnityEngine.Object target;

            public UnityEngine.Object Target => target;

            public bool Matches(string name)
            {
                return !string.IsNullOrEmpty(trackName) && string.Equals(trackName, name, StringComparison.Ordinal);
            }
        }
    }
}
