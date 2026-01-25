using System;
using UnityEngine;

namespace Project.Scripts.Infra
{
    [Serializable]
    public class KeyConfigData
    {
        // デフォルト値の定数
        const KeyCode DefaultMoveUpKey = KeyCode.UpArrow;
        const KeyCode DefaultMoveDownKey = KeyCode.DownArrow;
        const KeyCode DefaultMoveLeftKey = KeyCode.LeftArrow;
        const KeyCode DefaultMoveRightKey = KeyCode.RightArrow;
        const KeyCode DefaultAttackKey = KeyCode.Z;
        
        // 移動キー設定
        public KeyCode moveUpKey = DefaultMoveUpKey;
        public KeyCode moveDownKey = DefaultMoveDownKey;
        public KeyCode moveLeftKey = DefaultMoveLeftKey;
        public KeyCode moveRightKey = DefaultMoveRightKey;
        
        // 攻撃キー設定
        public KeyCode attackKey = DefaultAttackKey;
        
        public void ResetToDefault()
        {
            moveUpKey = DefaultMoveUpKey;
            moveDownKey = DefaultMoveDownKey;
            moveLeftKey = DefaultMoveLeftKey;
            moveRightKey = DefaultMoveRightKey;
            attackKey = DefaultAttackKey;
        }
    }
}
