using System;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.PhaseAction
{
    // 画面上の弾・予告線・ビーム・雑魚敵をすべて消す
    [Serializable]
    public class BulletClearAction : ITimedPhaseAction
    {
        [SerializeField, Min(0f)] float time;

        public BulletClearAction() { }

        public BulletClearAction(float time)
        {
            this.time = time;
        }

        public float Time => time;
    }
}
