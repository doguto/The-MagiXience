using System;
using Project.Scenes.Battle.Scripts.Model.Attack;
using Project.Scenes.Battle.Scripts.Model.Movement;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.PhaseAction
{
    // フェーズ開始時にボスへ適用する。攻撃・移動ともフェーズ終了まで継続する
    [Serializable]
    public class BossAction : IPhaseAction
    {
        [SerializeField] AttackPreset attackPreset;
        [SerializeField] MovementPreset movementPreset;
        [Tooltip("フェーズ開始時にボスを発狂モードにする。発狂後は超過ダメージ倍率を使わず発狂HPを直接減らす")]
        [SerializeField] bool enterStrongMode;

        public BossAction() { }

        public BossAction(AttackPreset attackPreset, MovementPreset movementPreset, bool enterStrongMode)
        {
            this.attackPreset = attackPreset;
            this.movementPreset = movementPreset;
            this.enterStrongMode = enterStrongMode;
        }

        public AttackPreset AttackPreset => attackPreset;
        public MovementPreset MovementPreset => movementPreset;
        public bool EnterStrongMode => enterStrongMode;
    }
}
