using UnityEditor;
using UnityEditor.AddressableAssets.Settings;

namespace Project.Editor
{
    public class AddressableAutoAddressAssigner : AssetPostprocessor
    {
        const string TargetRoot = "Assets/Project/DataStore/";
        const string TargetExtension = ".asset";

        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null || settings.DefaultGroup == null)
            {
                return;
            }

            var changed = false;
            changed |= AssignAddresses(importedAssets, settings);
            changed |= AssignAddresses(movedAssets, settings);

            if (changed)
            {
                AssetDatabase.SaveAssets();
            }
        }

        static bool AssignAddresses(string[] assetPaths, AddressableAssetSettings settings)
        {
            var changed = false;

            foreach (var assetPath in assetPaths)
            {
                if (!assetPath.StartsWith(TargetRoot) ||
                    !assetPath.EndsWith(TargetExtension) ||
                    AssetDatabase.IsValidFolder(assetPath))
                {
                    continue;
                }

                var guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrEmpty(guid))
                {
                    continue;
                }

                var entry = settings.FindAssetEntry(guid)
                            ?? settings.CreateOrMoveEntry(guid, settings.DefaultGroup, false, false);

                if (entry.address == assetPath)
                {
                    continue;
                }

                entry.SetAddress(assetPath, false);
                changed = true;
            }

            return changed;
        }
    }
}
