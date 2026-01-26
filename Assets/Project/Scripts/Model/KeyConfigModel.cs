using System.Linq;
using Project.Scripts.Infra;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Scripts.Model
{
    public class KeyConfigModel : ModelBase
    {
        readonly InputActionAsset inputActions;
        readonly KeyConfigData keyConfigData;
        readonly UserModel userModel;

        public KeyConfigModel(UserModel userModel, InputActionAsset inputActions)
        {
            this.userModel = userModel;
            keyConfigData = userModel.UserData.keyConfigData;
            this.inputActions = inputActions;

            LoadBindingOverrides();
        }

        void LoadBindingOverrides()
        {
            if (keyConfigData.bindingOverrides == null) return;
            if (keyConfigData.bindingOverrides.Count == 0) return;

            foreach (var bindingOverride in keyConfigData.bindingOverrides)
            {
                var action = inputActions.FindAction(bindingOverride.actionName);
                if (action == null) continue;

                var bindings = action.bindings;
                for (var i = 0; i < bindings.Count; i++)
                    if (bindings[i].id.ToString() == bindingOverride.bindingId)
                    {
                        action.ApplyBindingOverride(i, bindingOverride.overridePath);
                        break;
                    }
            }
        }

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

            action.ApplyBindingOverride(bindingIndex, newPath);

            var binding = action.bindings[bindingIndex];
            var bindingId = binding.id.ToString();

            var existingOverride = keyConfigData.bindingOverrides
                                                .FirstOrDefault(o =>
                                                    o.actionName == actionName && o.bindingId == bindingId
                                                );

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

        public void ResetAllBindings()
        {
            inputActions.RemoveAllBindingOverrides();
            keyConfigData.bindingOverrides.Clear();
            userModel.Save();
        }

        public void ResetActionBindings(string actionName)
        {
            var action = inputActions.FindAction(actionName);
            if (action == null) return;

            action.RemoveAllBindingOverrides();

            for (var i = keyConfigData.bindingOverrides.Count - 1; i >= 0; i--)
                if (keyConfigData.bindingOverrides[i].actionName == actionName)
                {
                    keyConfigData.bindingOverrides.RemoveAt(i);
                }

            userModel.Save();
        }

        public InputActionAsset GetInputActions()
        {
            return inputActions;
        }

        public InputAction GetAction(string actionName)
        {
            return inputActions.FindAction(actionName);
        }

        public string GetBindingOverridesJson()
        {
            return inputActions.SaveBindingOverridesAsJson();
        }

        public void LoadBindingOverridesJson(string json)
        {
            inputActions.LoadBindingOverridesFromJson(json);
        }
    }
}
