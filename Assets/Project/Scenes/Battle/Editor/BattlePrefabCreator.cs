using System.IO;
using UnityEditor;
using UnityEngine;

namespace Project.Scenes.Battle.Editor
{
    public static class BattlePrefabCreator
    {
        const string SourcePrefabPath = "Assets/Project/Scenes/Battle/Prefabs/Base/EnemyEntityBase.prefab";
        const string BulletPrefabPath = "Assets/Project/Scenes/Battle/Prefabs/Base/Bullet.prefab";

        [MenuItem("Assets/Create/Battle/Enemy Entity Base")]
        public static void CreateEnemyEntityBase()
        {
            var targetFolder = GetSelectedFolderPath();
            var newPath = AssetDatabase.GenerateUniqueAssetPath(targetFolder + "/EnemyEntityBase.prefab");

            AssetDatabase.CopyAsset(SourcePrefabPath, newPath);
            AssetDatabase.Refresh();

            var newAsset = AssetDatabase.LoadAssetAtPath<Object>(newPath);
            Selection.activeObject = newAsset;
            EditorGUIUtility.PingObject(newAsset);
        }

        [MenuItem("Assets/Create/Battle/Enemy Entity Base", true)]
        public static bool CreateEnemyEntityBaseValidation()
        {
            return !string.IsNullOrEmpty(GetSelectedFolderPath());
        }

        [MenuItem("Assets/Create/Battle/Bullet")]
        public static void CreateBullet()
        {
            var targetFolder = GetSelectedFolderPath();
            var newPath = AssetDatabase.GenerateUniqueAssetPath(targetFolder + "/Bullet.prefab");

            AssetDatabase.CopyAsset(BulletPrefabPath, newPath);
            AssetDatabase.Refresh();

            var newAsset = AssetDatabase.LoadAssetAtPath<Object>(newPath);
            Selection.activeObject = newAsset;
            EditorGUIUtility.PingObject(newAsset);
        }

        [MenuItem("Assets/Create/Battle/Bullet", true)]
        public static bool CreateBulletValidation()
        {
            return !string.IsNullOrEmpty(GetSelectedFolderPath());
        }

        static string GetSelectedFolderPath()
        {
            if (Selection.activeObject == null)
            {
                return "Assets";
            }

            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(path))
            {
                return "Assets";
            }

            return Directory.Exists(path) ? path : Path.GetDirectoryName(path)?.Replace("\\", "/") ?? "Assets";
        }
    }
}
