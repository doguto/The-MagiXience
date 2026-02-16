using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Project.Scenes.Battle.Scripts.Model
{
    [Serializable]
    public class EnemySpawnTrackDefinition : TrackDefinitionBase<SignalTrack, EnemySpawnDefinition>
    {

    }

    [Serializable]
    public class EnemySpawnDefinition : IClipDefinition<SignalTrack>
    {
        [SerializeField, Min(0)] double time;
        [SerializeField] Vector3 spawnPosition;
        [SerializeField] GameObject prefab;

        public void Build(SignalTrack track)
        {
            if (!track) return;

            // ランタイムでSignalAssetを動的生成
            var signal = ScriptableObject.CreateInstance<EnemySpawnSignal>();
            signal.name = $"EnemySpawn_{time}";
            signal.hideFlags = HideFlags.DontSave;

            // プロパティを設定
            signal.SetProperties(spawnPosition, prefab);

            var emitter = track.CreateMarker<SignalEmitter>(time);
            emitter.asset = signal;
        }
    }
}
