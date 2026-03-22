using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace Project.Scripts.Model
{
    public class KeyConfigEditingModel
    {
        readonly KeyConfigModel sourceModel;
        readonly Dictionary<KeyConfigModel.KeyConfigAction, Dictionary<KeyConfigModel.KeySlot, string>> draftPaths = new();

        public KeyConfigEditingModel(KeyConfigModel sourceModel)
        {
            this.sourceModel = sourceModel;

            foreach (var action in sourceModel.GetDisplayOrder())
            {
                draftPaths[action] = new Dictionary<KeyConfigModel.KeySlot, string>();

                foreach (KeyConfigModel.KeySlot slot in System.Enum.GetValues(typeof(KeyConfigModel.KeySlot)))
                {
                    if (!sourceModel.HasSlot(action, slot)) continue;
                    draftPaths[action][slot] = sourceModel.GetBindingPath(action, slot);
                }
            }
        }

        public IReadOnlyList<KeyConfigModel.KeyConfigAction> GetDisplayOrder()
        {
            return sourceModel.GetDisplayOrder();
        }

        public string GetDisplayName(KeyConfigModel.KeyConfigAction action)
        {
            return sourceModel.GetDisplayName(action);
        }

        public bool HasSlot(KeyConfigModel.KeyConfigAction action, KeyConfigModel.KeySlot slot)
        {
            return sourceModel.HasSlot(action, slot);
        }

        public string GetDisplayString(KeyConfigModel.KeyConfigAction action, KeyConfigModel.KeySlot slot)
        {
            if (!HasSlot(action, slot)) return "-";
            if (!draftPaths.TryGetValue(action, out var slots)) return "-";
            if (!slots.TryGetValue(slot, out var path)) return "-";
            if (string.IsNullOrEmpty(path)) return "-";

            return InputControlPath.ToHumanReadableString(
                path,
                InputControlPath.HumanReadableStringOptions.OmitDevice
            );
        }

        public void SetBindingPath(KeyConfigModel.KeyConfigAction action, KeyConfigModel.KeySlot slot, string path)
        {
            if (!HasSlot(action, slot)) return;
            if (string.IsNullOrEmpty(path)) return;

            draftPaths[action][slot] = path;
        }

        public void ResetAction(KeyConfigModel.KeyConfigAction action)
        {
            foreach (KeyConfigModel.KeySlot slot in System.Enum.GetValues(typeof(KeyConfigModel.KeySlot)))
            {
                if (!HasSlot(action, slot)) continue;
                draftPaths[action][slot] = sourceModel.GetDefaultPath(action, slot);
            }
        }

        public void Apply()
        {
            var changes = new List<KeyConfigModel.BindingChange>();

            foreach (var action in sourceModel.GetDisplayOrder())
            {
                foreach (KeyConfigModel.KeySlot slot in System.Enum.GetValues(typeof(KeyConfigModel.KeySlot)))
                {
                    if (!HasSlot(action, slot)) continue;
                    if (!draftPaths[action].TryGetValue(slot, out var path)) continue;

                    changes.Add(new KeyConfigModel.BindingChange(action, slot, path));
                }
            }

            sourceModel.ApplyChanges(changes);
        }
    }
}
