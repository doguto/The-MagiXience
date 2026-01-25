using System;
using System.IO;
using Project.Scripts.Infra;
using UnityEngine;

namespace Project.Scripts.Model
{
    public class KeyConfigModel : ModelBase
    {
        KeyConfigData keyConfigData;
        readonly UserModel userModel;

        public KeyCode MoveUpKey => keyConfigData.moveUpKey;
        public KeyCode MoveDownKey => keyConfigData.moveDownKey;
        public KeyCode MoveLeftKey => keyConfigData.moveLeftKey;
        public KeyCode MoveRightKey => keyConfigData.moveRightKey;
        public KeyCode AttackKey => keyConfigData.attackKey;

        public KeyConfigModel(UserModel userModel)
        {
            this.userModel = userModel;
            keyConfigData = userModel.UserData.keyConfigData;
        }

        public void SetMoveUpKey(KeyCode keyCode)
        {
            keyConfigData.moveUpKey = keyCode;
            userModel.Save();
        }

        public void SetMoveDownKey(KeyCode keyCode)
        {
            keyConfigData.moveDownKey = keyCode;
            userModel.Save();
        }

        public void SetMoveLeftKey(KeyCode keyCode)
        {
            keyConfigData.moveLeftKey = keyCode;
            userModel.Save();
        }

        public void SetMoveRightKey(KeyCode keyCode)
        {
            keyConfigData.moveRightKey = keyCode;
            userModel.Save();
        }

        public void SetAttackKey(KeyCode keyCode)
        {
            keyConfigData.attackKey = keyCode;
            userModel.Save();
        }

        public void ResetToDefault()
        {
            keyConfigData.ResetToDefault();
            userModel.Save();
        }

        public bool IsKeyUsed(KeyCode keyCode)
        {
            return keyCode == keyConfigData.moveUpKey ||
                   keyCode == keyConfigData.moveDownKey ||
                   keyCode == keyConfigData.moveLeftKey ||
                   keyCode == keyConfigData.moveRightKey ||
                   keyCode == keyConfigData.attackKey;
        }
    }
}
