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

                if (!TryGetBinding(trackName, out var target) || !target)
                {
                    continue;
                }

                director.SetGenericBinding(output.sourceObject, target);
            }
        }

        bool TryGetBinding(string trackName, out UnityEngine.Object target)
        {
            foreach (var entry in bindings)
            {
                if (entry != null && entry.Matches(trackName))
                {
                    target = entry.Target;
                    return true;
                }
            }

            target = null;
            return false;
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
