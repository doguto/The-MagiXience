using System.Linq;
using Project.Scenes.Battle.Scripts.BehaviorTree;
using Project.Scenes.Battle.Scripts.Model;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Project.Scenes.Battle.Editor.BehaviorTree.Migration
{
    // 旧ステージデータを BehaviorTree 形式に変換し、StageData と Addressables を新アセットへ向け替える
    public static class LegacyStageMigrationMenu
    {
        const string StageDataPath = "Assets/Project/DataStore/StageData.asset";

        [MenuItem("Tools/Battle/Migrate Legacy Stage/Stage 1")] static void Stage1() => Migrate(1);
        [MenuItem("Tools/Battle/Migrate Legacy Stage/Stage 2")] static void Stage2() => Migrate(2);
        [MenuItem("Tools/Battle/Migrate Legacy Stage/Stage 3")] static void Stage3() => Migrate(3);
        [MenuItem("Tools/Battle/Migrate Legacy Stage/Stage 4")] static void Stage4() => Migrate(4);

        public static string Migrate(int stageNumber)
        {
            var wayPath = $"Assets/Project/DataStore/Stage{stageNumber}/Way/Stage{stageNumber}Way.asset";
            var bossPath = $"Assets/Project/DataStore/Stage{stageNumber}/Boss/Stage{stageNumber}Boss.asset";
            var outDir = $"Assets/Project/DataStore/Stage{stageNumber}/BT";

            var way = AssetDatabase.LoadAssetAtPath<BattleSequenceAsset>(wayPath);
            var boss = AssetDatabase.LoadAssetAtPath<BattleSequenceAsset>(bossPath);
            if (way == null && boss == null)
            {
                Debug.LogError($"[LegacyStageMigration] Legacy sequence not found for stage {stageNumber}.");
                return "not found";
            }

            if (AssetDatabase.IsValidFolder(outDir)) AssetDatabase.DeleteAsset(outDir);

            var converter = new LegacyBattleConverter(outDir, $"S{stageNumber}");
            var newWay = way != null ? converter.ConvertSequence(way) : null;
            var newBoss = boss != null ? converter.ConvertSequence(boss) : null;
            AssetDatabase.SaveAssets();

            Register(stageNumber, newWay, newBoss);
            AssetDatabase.SaveAssets();

            var summary = $"[LegacyStageMigration] Stage {stageNumber} converted. warnings={converter.Warnings.Count}\n" + string.Join("\n", converter.Warnings.Distinct());
            Debug.Log(summary);
            return summary;
        }

        static void Register(int stageNumber, BattleStageAsset way, BattleStageAsset boss)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var group = settings.DefaultGroup;
            string AddressOf(BattleStageAsset asset)
            {
                if (asset == null) return "";
                var path = AssetDatabase.GetAssetPath(asset);
                var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), group);
                entry.address = path;
                return path;
            }

            var wayAddress = AddressOf(way);
            var bossAddress = AddressOf(boss);

            var stageData = AssetDatabase.LoadMainAssetAtPath(StageDataPath);
            var so = new SerializedObject(stageData);
            var it = so.GetIterator();
            while (it.Next(true))
            {
                if (it.propertyType != SerializedPropertyType.Generic) continue;
                var number = it.FindPropertyRelative("stageNumber");
                if (number == null || number.intValue != stageNumber) continue;
                it.FindPropertyRelative("waySequenceAddress").stringValue = wayAddress;
                it.FindPropertyRelative("bossSequenceAddress").stringValue = bossAddress;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }
    }
}
