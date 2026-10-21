using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace Project.Editor
{
    public class AddressableAutoAddressAssigner : AssetPostprocessor
    {
        const string TargetRoot = "Assets/Project/Textures/";
        const string DeprecatedFolder = "/Deprecated/";

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
            changed |= AssignAddresses(importedAssets, null, settings);
            changed |= AssignAddresses(movedAssets, movedFromAssetPaths, settings);

            if (changed)
            {
                AssetDatabase.SaveAssets();
            }
        }

        static bool AssignAddresses(string[] assetPaths, string[] movedFromAssetPaths, AddressableAssetSettings settings)
        {
            var changed = false;

            for (var i = 0; i < assetPaths.Length; i++)
            {
                var assetPath = assetPaths[i];
                if (!assetPath.StartsWith(TargetRoot) ||
                    AssetDatabase.IsValidFolder(assetPath))
                {
                    continue;
                }

                var guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrEmpty(guid))
                {
                    continue;
                }

                var entry = settings.FindAssetEntry(guid);

                // Deprecatedに置いたアセットはビルドに含めないよう登録しない
                if (assetPath.Contains(DeprecatedFolder))
                {
                    if (entry != null)
                    {
                        settings.RemoveAssetEntry(guid, false);
                        changed = true;
                    }
                    continue;
                }

                if (entry == null)
                {
                    entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup, false, false);
                    entry.SetAddress(assetPath, false);
                    changed = true;
                    continue;
                }

                // 手動で付けたアドレス(BGMのSceneType_BgmType等)は上書きしない
                // 移動時は、旧パスのままのアドレスだけ新パスに追従させる
                var movedFrom = movedFromAssetPaths?[i];
                if (movedFrom == null || entry.address != movedFrom)
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
