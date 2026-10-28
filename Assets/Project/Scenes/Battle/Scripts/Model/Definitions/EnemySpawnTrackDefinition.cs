using System;
using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.Model.Attack;
using Project.Scenes.Battle.Scripts.Model.Movement;
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
        [SerializeField] double time;
        [SerializeField] Vector3 spawnPosition;
        [SerializeField] GameObject prefab;
        [SerializeField] MovementPreset movementOverride;
        [SerializeField] AttackPreset attackOverride;
        [SerializeField, Tooltip("この出現の敵が撃つ弾のMovement Stepsを置き換える。攻撃Entryにも指定があればEntryを優先。Noneなら弾Prefabの設定。")]
        MovementPreset bulletMovementOverride;

        public double Time => time;

        public void Build(SignalTrack track)
        {
            if (!track) return;

            // ランタイムでSignalAssetを動的生成
            var signal = ScriptableObject.CreateInstance<EnemySpawnSignal>();
            signal.name = $"EnemySpawn_{time}";
            signal.hideFlags = HideFlags.DontSave;

            // プロパティを設定
            signal.SetProperties(spawnPosition, prefab, movementOverride, attackOverride, bulletMovementOverride);

            var emitter = track.CreateMarker<SignalEmitter>(time);
            emitter.asset = signal;
        }
    }
}
