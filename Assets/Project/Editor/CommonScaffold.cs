using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;

namespace Project.Editor
{
    public class CommonScaffold : EditorWindow
    {
        private string scriptName = "";

        [MenuItem("Tools/CommonScaffold")]
        public static void ShowWindow()
        {
            GetWindow<CommonScaffold>("CommonScaffold");
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("CommonScaffold", EditorStyles.boldLabel);
            EditorGUIUtility.labelWidth = 250;
            scriptName = EditorGUILayout.TextField(scriptName);
            EditorGUILayout.LabelField("");
            EditorGUILayout.Space(5);

            if (GUILayout.Button("CommonScaffold"))
            {
                if (string.IsNullOrEmpty(scriptName))
                {
                    EditorUtility.DisplayDialog("Error", "Please enter a folder name", "OK");
                    return;
                }

                // パス区切りや".."を含む名前でCommons配下の外へ書き込まないよう、フォルダ名を制限する
                if (!Regex.IsMatch(scriptName, "^[A-Za-z_][A-Za-z0-9_]*$"))
                {
                    EditorUtility.DisplayDialog("Error", "Folder name must contain only letters, digits and '_'", "OK");
                    return;
                }

                CreateAssemblyReferences();
                this.Close();
            }
        }

        void CreateAssemblyReferences()
        {
            const string scriptsBasePath = "Assets/Project/Commons";
            string[] asmdefNames = { "Model", "Presenter", "View" };
            
            string path1 = Path.Combine(scriptsBasePath, scriptName);
            string path2 =  Path.Combine(path1, "Scripts");

            Directory.CreateDirectory(path2);
            Directory.CreateDirectory(Path.Combine(path1, "Prefabs"));
            foreach (string asmdefName in asmdefNames)
            {
                string subFolderPath = Path.Combine(path2, asmdefName);
                if (Directory.Exists(subFolderPath))
                {
                    Debug.LogWarning($"{asmdefName} already exists");
                    continue;
                }
                Directory.CreateDirectory(subFolderPath);
                
                string asmrefPath = Path.Combine(subFolderPath, asmdefName+".asmref");

                string asmrefContent = 
                    $@"{{
                        ""reference"": ""{asmdefName}""
                    }}";
                File.WriteAllText(asmrefPath, asmrefContent);
                Debug.Log($"Created asmref: {asmrefPath}");
            }
            
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Done", $"Assembly References for '{scriptName}' Created!" , "OK");
        }
    }
}