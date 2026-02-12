using System.Collections.Generic;
using System.IO;
using System.Linq;
using Project.Scenes.Scenario.Scripts.Model;
using UnityEditor;
using UnityEngine;

namespace Project.Editor
{
    public class ScenarioImporter : UnityEditor.Editor
    {
        [MenuItem("Tools/Import Scenario")]
        public static void ImportScenario()
        {
            string path = EditorUtility.OpenFilePanel("Select Scenario Text", "Assets/Project/Editor/Scenario", "txt");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            string content = File.ReadAllText(path);
            ScenarioData data = ScriptableObject.CreateInstance<ScenarioData>();
            data.steps = ParseScenario(content);

            string fileName = Path.GetFileNameWithoutExtension(path);

            string assetPath = $"Assets/Project/DataStore/{fileName}.asset";
            AssetDatabase.CreateAsset(data, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = data;

            Debug.Log($"Scenario imported to: {assetPath}");
        }

        static List<ScenarioStep> ParseScenario(string content)
        {
            var steps = new List<ScenarioStep>();
            var lines = content.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("//") || line.StartsWith("----------------")) // Skip empty or comment lines
                {
                    continue;
                }

                if (line.StartsWith("@"))
                {
                    // Remove '@' and split by comma
                    string[] parts = line.Substring(1).Split(',');
                    string functionName = parts[0].Trim();
                    List<string> args = parts.Skip(1).Select(s => s.Trim()).ToList();

                    // ShowMessage系は追加でメッセージ行を読み込む
                    if (functionName == "ShowCastMessage" || functionName == "ShowMessage")
                    {
                        var messageLines = new List<string>();
                        while (i + 1 < lines.Length)
                        {
                            string nextLine = lines[i + 1].Trim();
                            if (string.IsNullOrEmpty(nextLine) || nextLine.StartsWith("@") || nextLine.StartsWith("----------------"))
                            {
                                break;
                            }

                            messageLines.Add(nextLine);
                            i++;
                        }
                        args.Add(string.Join("\n", messageLines));
                    }

                    steps.Add(new ScenarioStep
                    {
                        function = functionName,
                        args = args.ToArray()
                    });
                }
            }
            return steps;
        }
    }
}
