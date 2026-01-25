using System;
using System.Linq;
using Project.Scripts.Infra;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Scripts.Model
{
    /// <summary>
    /// Input Systemを使用したキー設定管理モデル
    /// バインディングのオーバーライドと永続化を管理
    /// </summary>
    public class KeyConfigModel : ModelBase
    {
        readonly KeyConfigData keyConfigData;
        readonly UserModel userModel;
        readonly InputActionAsset inputActions;

        public KeyConfigModel(UserModel userModel, InputActionAsset inputActions)
        {
            this.userModel = userModel;
            this.keyConfigData = userModel.UserData.keyConfigData;
            this.inputActions = inputActions;
            
            // 保存されているバインディングオーバーライドを適用
            LoadBindingOverrides();
        }

        /// <summary>
        /// 保存されているバインディングオーバーライドをInput Actionに適用
        /// </summary>
        void LoadBindingOverrides()
        {
            if (keyConfigData.bindingOverrides == null || keyConfigData.bindingOverrides.Count == 0)
                return;

            foreach (var bindingOverride in keyConfigData.bindingOverrides)
            {
                var action = inputActions.FindAction(bindingOverride.actionName);
                if (action == null) continue;

                var bindingIndex = action.bindings.IndexOf(b => b.id.ToString() == bindingOverride.bindingId);
                if (bindingIndex != -1)
                {
                    action.ApplyBindingOverride(bindingIndex, bindingOverride.overridePath);
                }
            }
        }

        /// <summary>
        /// 特定のアクションのバインディングをオーバーライド
        /// </summary>
        /// <param name="actionName">アクション名（例: "Player/Move"）</param>
        /// <param name="bindingIndex">バインディングのインデックス</param>
        /// <param name="newPath">新しいバインディングパス（例: "&lt;Keyboard&gt;/w"）</param>
        public void SetBindingOverride(string actionName, int bindingIndex, string newPath)
        {
            var action = inputActions.FindAction(actionName);
            if (action == null)
            {
                Debug.LogError($"Action not found: {actionName}");
                return;
            }

            if (bindingIndex < 0 || bindingIndex >= action.bindings.Count)
            {
                Debug.LogError($"Invalid binding index: {bindingIndex}");
                return;
            }

            // バインディングをオーバーライド
            action.ApplyBindingOverride(bindingIndex, newPath);

            // データに保存
            var binding = action.bindings[bindingIndex];
            var bindingId = binding.id.ToString();
            
            var existingOverride = keyConfigData.bindingOverrides
                .FirstOrDefault(o => o.actionName == actionName && o.bindingId == bindingId);

            if (existingOverride != null)
            {
                existingOverride.overridePath = newPath;
            }
            else
            {
                keyConfigData.bindingOverrides.Add(new KeyConfigData.BindingOverride
                {
                    actionName = actionName,
                    bindingId = bindingId,
                    overridePath = newPath
                });
            }

            userModel.Save();
        }

        /// <summary>
        /// すべてのバインディングをデフォルトに戻す
        /// </summary>
        public void ResetAllBindings()
        {
            inputActions.RemoveAllBindingOverrides();
            keyConfigData.bindingOverrides.Clear();
            userModel.Save();
        }

        /// <summary>
        /// 特定のアクションのバインディングをデフォルトに戻す
        /// </summary>
        public void ResetActionBindings(string actionName)
        {
            var action = inputActions.FindAction(actionName);
            if (action == null) return;

            action.RemoveAllBindingOverrides();
            
            // 保存データから該当するオーバーライドを削除
            keyConfigData.bindingOverrides.RemoveAll(o => o.actionName == actionName);
            userModel.Save();
        }

        /// <summary>
        /// Input ActionAssetへの参照を取得
        /// </summary>
        public InputActionAsset GetInputActions() => inputActions;
        
        /// <summary>
        /// 特定のアクションを取得
        /// </summary>
        public InputAction GetAction(string actionName)
        {
            return inputActions.FindAction(actionName);
        }
        
        /// <summary>
        /// すべてのバインディングオーバーライドをJSON形式で取得
        /// </summary>
        public string GetBindingOverridesJson()
        {
            return inputActions.SaveBindingOverridesAsJson();
        }
        
        /// <summary>
        /// JSON形式のバインディングオーバーライドを読み込む
        /// </summary>
        public void LoadBindingOverridesJson(string json)
        {
            inputActions.LoadBindingOverridesFromJson(json);
        }
    }
}
