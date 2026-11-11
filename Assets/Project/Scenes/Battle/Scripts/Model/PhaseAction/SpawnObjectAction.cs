using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.PhaseAction
{
    // Prefabを生成し、duration経過後かフェーズ終了時に破棄する。位置はPrefab自身の設定を使う
    [Serializable]
    public class SpawnObjectAction : ITimedPhaseAction
    {
        [SerializeField, Min(0f)] float time;
        [SerializeField, Min(0f)] float duration = 1f;
        [SerializeField] GameObject prefab;

        public SpawnObjectAction() { }

        public SpawnObjectAction(float time, float duration, GameObject prefab)
        {
            this.time = time;
            this.duration = duration;
            this.prefab = prefab;
        }

        public float Time => time;
        public float Duration => duration;
        public GameObject Prefab => prefab;
    }
}
