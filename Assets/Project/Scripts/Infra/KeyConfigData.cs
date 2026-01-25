using System;
using UnityEngine;

namespace Project.Scripts.Infra
{
    [Serializable]
    public class KeyConfigData
    {
        // 移動キー設定
        public KeyCode moveUpKey = KeyCode.UpArrow;
        public KeyCode moveDownKey = KeyCode.DownArrow;
        public KeyCode moveLeftKey = KeyCode.LeftArrow;
        public KeyCode moveRightKey = KeyCode.RightArrow;
        
        // 攻撃キー設定
        public KeyCode attackKey = KeyCode.Z;
        
        public KeyConfigData()
        {
            ResetToDefault();
        }
        
        public void ResetToDefault()
        {
            moveUpKey = KeyCode.UpArrow;
            moveDownKey = KeyCode.DownArrow;
            moveLeftKey = KeyCode.LeftArrow;
            moveRightKey = KeyCode.RightArrow;
            attackKey = KeyCode.Z;
        }
    }
}
