using System;
using System.Collections.Generic;
using System.Linq;
using Project.Scripts.Infra;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Scripts.Model
{
    public class KeyConfigModel : ModelBase
    {
        public enum KeyConfigAction
        {
            Up,
            Down,
            Left,
            Right,
            Attack,
            Charge,
            Submit,
            Cancel
        }

        public enum KeySlot
        {
            Primary,
            Secondary
        }

        public class BindingTarget
        {
            public string ActionPath { get; }
            public string BindingId { get; }
            public string DefaultPath { get; }

            public BindingTarget(string actionPath, string bindingId, string defaultPath)
            {
                ActionPath = actionPath;
                BindingId = bindingId;
                DefaultPath = defaultPath;
            }
        }

        public class KeyConfigEntry
        {
            public KeyConfigAction Action { get; }
            public string DisplayName { get; }
            public IReadOnlyDictionary<KeySlot, IReadOnlyList<BindingTarget>> TargetsBySlot { get; }

            public KeyConfigEntry(
                KeyConfigAction action,
                string displayName,
                IReadOnlyDictionary<KeySlot, IReadOnlyList<BindingTarget>> targetsBySlot
            )
            {
                Action = action;
                DisplayName = displayName;
                TargetsBySlot = targetsBySlot;
            }
        }

        public class BindingChange
        {
            public KeyConfigAction Action { get; }
            public KeySlot Slot { get; }
            public string Path { get; }

            public BindingChange(KeyConfigAction action, KeySlot slot, string path)
            {
                Action = action;
                Slot = slot;
                Path = path;
            }
        }

        readonly InputActionAsset inputActions;
        readonly KeyConfigData keyConfigData;
        readonly UserModel userModel;
        readonly Dictionary<KeyConfigAction, KeyConfigEntry> entries;
        public List<KeyConfigAction> DisplayOrder { get; }

        public KeyConfigModel(UserModel userModel, InputActionAsset inputActions)
        {
            this.userModel = userModel;
            this.inputActions = inputActions;
            keyConfigData = userModel.UserData.keyConfigData ?? new KeyConfigData();
            userModel.UserData.keyConfigData ??= keyConfigData;

            entries = CreateEntries();
            DisplayOrder = CreateDisplayOrder();
            LoadBindingOverrides();
        }

        Dictionary<KeyConfigAction, KeyConfigEntry> CreateEntries()
        {
            return new Dictionary<KeyConfigAction, KeyConfigEntry>
            {
                {
                    KeyConfigAction.Up,
                    new KeyConfigEntry(
                        KeyConfigAction.Up,
                        "上",
                        new Dictionary<KeySlot, IReadOnlyList<BindingTarget>>
                        {
                            {
                                KeySlot.Primary,
                                new List<BindingTarget>
                                {
                                    new("Player/Move", "7e383947-06f2-4657-aedc-783fe3aced74", "<Keyboard>/w"),
                                    new("UI/Navigate", "70f4d56d-5d80-4a81-8be9-82420fa370aa", "<Keyboard>/w")
                                }
                            },
                            {
                                KeySlot.Secondary,
                                new List<BindingTarget>
                                {
                                    new("Player/Move", "4569f7f3-f0e9-4e29-a7d1-79d97e195b46", "<Keyboard>/upArrow"),
                                    new("UI/Navigate", "60543508-b5d5-4df3-bdc8-e3f519be12c7", "<Keyboard>/upArrow")
                                }
                            }
                        }
                    )
                },
                {
                    KeyConfigAction.Down,
                    new KeyConfigEntry(
                        KeyConfigAction.Down,
                        "下",
                        new Dictionary<KeySlot, IReadOnlyList<BindingTarget>>
                        {
                            {
                                KeySlot.Primary,
                                new List<BindingTarget>
                                {
                                    new("Player/Move", "3598e500-0612-4a79-8c33-d5cfb700230e", "<Keyboard>/s"),
                                    new("UI/Navigate", "9e5167b1-e306-45f4-88bc-0e5aa43c0d15", "<Keyboard>/s")
                                }
                            },
                            {
                                KeySlot.Secondary,
                                new List<BindingTarget>
                                {
                                    new("Player/Move", "188d3809-9054-4bc6-85bb-2ee71280acf8", "<Keyboard>/downArrow"),
                                    new("UI/Navigate", "6b752bc4-ba07-4d11-afa2-564b32699337", "<Keyboard>/downArrow")
                                }
                            }
                        }
                    )
                },
                {
                    KeyConfigAction.Left,
                    new KeyConfigEntry(
                        KeyConfigAction.Left,
                        "左",
                        new Dictionary<KeySlot, IReadOnlyList<BindingTarget>>
                        {
                            {
                                KeySlot.Primary,
                                new List<BindingTarget>
                                {
                                    new("Player/Move", "feba1612-dd2c-42b5-8758-7a78d79761ea", "<Keyboard>/a"),
                                    new("UI/Navigate", "776cc8cf-5eab-4b35-9d83-063e09270d94", "<Keyboard>/a")
                                }
                            },
                            {
                                KeySlot.Secondary,
                                new List<BindingTarget>
                                {
                                    new("Player/Move", "2687a5fe-f8bb-4023-8fb5-5061928ea0a2", "<Keyboard>/leftArrow"),
                                    new("UI/Navigate", "af654146-5fd0-4a3d-b4c5-bda6121ef630", "<Keyboard>/leftArrow")
                                }
                            }
                        }
                    )
                },
                {
                    KeyConfigAction.Right,
                    new KeyConfigEntry(
                        KeyConfigAction.Right,
                        "右",
                        new Dictionary<KeySlot, IReadOnlyList<BindingTarget>>
                        {
                            {
                                KeySlot.Primary,
                                new List<BindingTarget>
                                {
                                    new("Player/Move", "88ddea62-a339-4ac8-89b3-83d56bb418c0", "<Keyboard>/d"),
                                    new("UI/Navigate", "329cc788-0de4-4d0f-a1ee-0343b4cc0663", "<Keyboard>/d")
                                }
                            },
                            {
                                KeySlot.Secondary,
                                new List<BindingTarget>
                                {
                                    new("Player/Move", "f70c418a-bdfd-4a52-821a-33b01b6e8785", "<Keyboard>/rightArrow"),
                                    new("UI/Navigate", "a6067903-cc5c-4e06-892d-2b77dccfe8af", "<Keyboard>/rightArrow")
                                }
                            }
                        }
                    )
                },
                {
                    KeyConfigAction.Attack,
                    new KeyConfigEntry(
                        KeyConfigAction.Attack,
                        "攻撃",
                        new Dictionary<KeySlot, IReadOnlyList<BindingTarget>>
                        {
                            {
                                KeySlot.Primary,
                                new List<BindingTarget>
                                {
                                    new("Player/Attack", "6e531e4e-dba2-4c80-b35b-e4c378faed86", "<Keyboard>/k")
                                }
                            },
                            {
                                KeySlot.Secondary,
                                new List<BindingTarget>
                                {
                                    new("Player/Attack", "924410b2-3002-4248-9227-4a166338f567", "<Keyboard>/z")
                                }
                            }
                        }
                    )
                },
                {
                    KeyConfigAction.Charge,
                    new KeyConfigEntry(
                        KeyConfigAction.Charge,
                        "チャージ",
                        new Dictionary<KeySlot, IReadOnlyList<BindingTarget>>
                        {
                            {
                                KeySlot.Primary,
                                new List<BindingTarget>
                                {
                                    new("Player/Charge", "ee57e007-a5f5-475b-9fed-e70f954c443b", "<Keyboard>/leftShift")
                                }
                            },
                            {
                                KeySlot.Secondary,
                                new List<BindingTarget>
                                {
                                    new("Player/Charge", "95054fa1-126b-42fd-818f-2b659b71b3ac", "<Keyboard>/l")
                                }
                            }
                        }
                    )
                },
                {
                    KeyConfigAction.Submit,
                    new KeyConfigEntry(
                        KeyConfigAction.Submit,
                        "決定",
                        new Dictionary<KeySlot, IReadOnlyList<BindingTarget>>
                        {
                            {
                                KeySlot.Primary,
                                new List<BindingTarget>
                                {
                                    new("UI/Submit", "573e2db7-7b12-49b4-a3ab-2106fc358e0e", "<Keyboard>/enter")
                                }
                            },
                            {
                                KeySlot.Secondary,
                                new List<BindingTarget>
                                {
                                    new("UI/Submit", "7083566c-b47c-42c7-ac2f-fa95e7c6697e", "<Keyboard>/z")
                                }
                            }
                        }
                    )
                },
                {
                    KeyConfigAction.Cancel,
                    new KeyConfigEntry(
                        KeyConfigAction.Cancel,
                        "戻る",
                        new Dictionary<KeySlot, IReadOnlyList<BindingTarget>>
                        {
                            {
                                KeySlot.Primary,
                                new List<BindingTarget>
                                {
                                    new("UI/Cancel", "b4b2df84-0cc6-4b1c-8cbf-e4e2fce90d7d", "<Keyboard>/escape")
                                }
                            },
                            {
                                KeySlot.Secondary,
                                new List<BindingTarget>
                                {
                                    new("UI/Cancel", "61a48a46-5d87-4de9-8672-44930981cf58", "<Keyboard>/backspace")
                                }
                            }
                        }
                    )
                }
            };
        }

        List<KeyConfigAction> CreateDisplayOrder()
        {
            return new List<KeyConfigAction>
            {
                KeyConfigAction.Up,
                KeyConfigAction.Down,
                KeyConfigAction.Left,
                KeyConfigAction.Right,
                KeyConfigAction.Attack,
                KeyConfigAction.Charge,
                KeyConfigAction.Submit,
                KeyConfigAction.Cancel
            };
        }

        void LoadBindingOverrides()
        {
            if (keyConfigData.bindingOverrides == null) return;
            if (keyConfigData.bindingOverrides.Count == 0) return;

            foreach (var bindingOverride in keyConfigData.bindingOverrides) ApplyBindingOverride(bindingOverride.actionPath, bindingOverride.bindingId, bindingOverride.overridePath, false);
        }

        public IReadOnlyList<KeyConfigEntry> GetEntries()
        {
            return DisplayOrder.Select(action => entries[action]).ToList();
        }

        public IReadOnlyList<KeyConfigAction> GetDisplayOrder()
        {
            return DisplayOrder;
        }

        public KeyConfigEntry GetEntry(KeyConfigAction action)
        {
            return entries[action];
        }

        public string GetDisplayName(KeyConfigAction action)
        {
            return entries[action].DisplayName;
        }

        public bool HasSlot(KeyConfigAction action, KeySlot slot)
        {
            return entries[action].TargetsBySlot.TryGetValue(slot, out var targets) && targets.Count > 0;
        }

        public string GetBindingPath(KeyConfigAction action, KeySlot slot)
        {
            var target = GetPrimaryTarget(action, slot);
            if (target == null) return string.Empty;

            var inputAction = inputActions.FindAction(target.ActionPath);
            if (inputAction == null) return target.DefaultPath;

            var bindingIndex = FindBindingIndexById(inputAction, target.BindingId);
            if (bindingIndex < 0) return target.DefaultPath;

            return string.IsNullOrEmpty(inputAction.bindings[bindingIndex].effectivePath)
                ? target.DefaultPath
                : inputAction.bindings[bindingIndex].effectivePath;
        }

        public string GetDefaultPath(KeyConfigAction action, KeySlot slot)
        {
            var target = GetPrimaryTarget(action, slot);
            return target?.DefaultPath ?? string.Empty;
        }

        public string GetDisplayString(KeyConfigAction action, KeySlot slot)
        {
            var path = GetBindingPath(action, slot);
            if (string.IsNullOrEmpty(path)) return "-";

            return InputControlPath.ToHumanReadableString(
                path,
                InputControlPath.HumanReadableStringOptions.OmitDevice
            );
        }

        public void ApplyChanges(IReadOnlyList<BindingChange> changes)
        {
            foreach (var change in changes)
            {
                if (!entries[change.Action].TargetsBySlot.TryGetValue(change.Slot, out var targets))
                {
                    continue;
                }

                var defaultPath = GetDefaultPath(change.Action, change.Slot);

                foreach (var target in targets)
                    if (change.Path == defaultPath)
                    {
                        RemoveBindingOverride(target.ActionPath, target.BindingId, false);
                    }
                    else
                    {
                        ApplyBindingOverride(target.ActionPath, target.BindingId, change.Path, false);
                    }
            }

            userModel.Save();
        }

        public void SetBindingOverride(KeyConfigAction action, KeySlot slot, string newPath)
        {
            if (string.IsNullOrEmpty(newPath))
            {
                Debug.LogError("[KeyConfigModel] newPath is null or empty.");
                return;
            }

            if (!entries[action].TargetsBySlot.TryGetValue(slot, out var targets))
            {
                Debug.LogWarning($"[KeyConfigModel] Slot not found. action={action}, slot={slot}");
                return;
            }

            foreach (var target in targets) ApplyBindingOverride(target.ActionPath, target.BindingId, newPath, false);

            userModel.Save();
        }

        public void ResetBinding(KeyConfigAction action, KeySlot slot)
        {
            if (!entries[action].TargetsBySlot.TryGetValue(slot, out var targets))
            {
                return;
            }

            foreach (var target in targets) RemoveBindingOverride(target.ActionPath, target.BindingId, false);

            userModel.Save();
        }

        public void ResetActionBindings(KeyConfigAction action)
        {
            ResetBinding(action, KeySlot.Primary);
            ResetBinding(action, KeySlot.Secondary);
        }

        public void ResetAllBindings()
        {
            inputActions.RemoveAllBindingOverrides();
            keyConfigData.bindingOverrides.Clear();
            userModel.Save();
        }

        void ApplyBindingOverride(string actionPath, string bindingId, string overridePath, bool save)
        {
            var action = inputActions.FindAction(actionPath);
            if (action == null)
            {
                Debug.LogWarning($"[KeyConfigModel] Action not found: {actionPath}");
                return;
            }

            var bindingIndex = FindBindingIndexById(action, bindingId);
            if (bindingIndex < 0)
            {
                Debug.LogWarning($"[KeyConfigModel] Binding not found. actionPath={actionPath}, bindingId={bindingId}");
                return;
            }

            action.ApplyBindingOverride(bindingIndex, overridePath);

            var existingOverride = keyConfigData.bindingOverrides.FirstOrDefault(x =>
                x.actionPath == actionPath &&
                x.bindingId == bindingId
            );

            if (existingOverride != null)
            {
                existingOverride.overridePath = overridePath;
            }
            else
            {
                keyConfigData.bindingOverrides.Add(new KeyConfigData.BindingOverride
                {
                    actionPath = actionPath,
                    bindingId = bindingId,
                    overridePath = overridePath
                });
            }

            if (save)
            {
                userModel.Save();
            }
        }

        void RemoveBindingOverride(string actionPath, string bindingId, bool save)
        {
            var action = inputActions.FindAction(actionPath);
            if (action != null)
            {
                var bindingIndex = FindBindingIndexById(action, bindingId);
                if (bindingIndex >= 0)
                {
                    action.RemoveBindingOverride(bindingIndex);
                }
            }

            for (var i = keyConfigData.bindingOverrides.Count - 1; i >= 0; i--)
            {
                var bindingOverride = keyConfigData.bindingOverrides[i];
                if (bindingOverride.actionPath == actionPath && bindingOverride.bindingId == bindingId)
                {
                    keyConfigData.bindingOverrides.RemoveAt(i);
                }
            }

            if (save)
            {
                userModel.Save();
            }
        }

        BindingTarget GetPrimaryTarget(KeyConfigAction action, KeySlot slot)
        {
            if (!entries[action].TargetsBySlot.TryGetValue(slot, out var targets)) return null;
            return targets.FirstOrDefault();
        }

        int FindBindingIndexById(InputAction action, string bindingId)
        {
            var bindings = action.bindings;
            for (var i = 0; i < bindings.Count; i++)
            {
                if (bindings[i].id.ToString() == bindingId)
                {
                    return i;
                }
            }

            return -1;
        }

        public InputActionAsset GetInputActions()
        {
            return inputActions;
        }

        public InputAction GetAction(string actionPath)
        {
            return inputActions.FindAction(actionPath);
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
