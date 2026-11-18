
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// DB作成の簡易化を目的に作られたスクリプト
/// UnityEditorのウィンドウメニュー→Tools→Create DB Generatorで使用
/// </summary>

public class CreateDB : EditorWindow
{
    private string soDataName = "";
    private string sObjectName = "";
    private string repositoryName = "";
    private string menuName = "";
    
    
    //int,string,List<T>の数と変数名,型を管理
    [System.Serializable]
    private class ColumnField
    {
        public string name = "";
        public string type = "";

        public bool IsNullOrEmpty
        {
            get => string.IsNullOrEmpty(name) && string.IsNullOrEmpty(type);
        }
    }
    
    private List<ColumnField> columnFields = new() {new ColumnField()};
    
    private Vector2 scrollPosition;
    
    
    //メニュー生成
    [MenuItem("Tools/Create DB Generator")]
    private static void ShowWindow()
    {
        GetWindow<CreateDB>("DB Generator");
    }
    
    private void OnGUI()
    {
        EditorGUILayout.LabelField("ScriptableObject Settings");
        EditorGUIUtility.labelWidth = 250; 
        soDataName = EditorGUILayout.TextField("データ名", soDataName);
        sObjectName = $"{soDataName}DataObject";
        repositoryName = $"{soDataName}ModelRepository";
        menuName = $"Database/{soDataName}Data";
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.TextField("オブジェクト名", sObjectName);
        EditorGUILayout.TextField("リポジトリ名", repositoryName);
        EditorGUILayout.TextField("メニューパス", menuName);
        EditorGUI.EndDisabledGroup();
        EditorGUIUtility.labelWidth = 0; 
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Field Definitions");
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("型", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("変数名");
        EditorGUILayout.EndHorizontal();
        
        //フィールド定義
        for (int i = 0; i < columnFields.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            columnFields[i].type = EditorGUILayout.TextField(columnFields[i].type);
            columnFields[i].name = EditorGUILayout.TextField(columnFields[i].name);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.Space(10);
        
        //追加・削除ボタン
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("変数を追加"))
        {
            columnFields.Add(new ColumnField());
        }
        
        EditorGUI.BeginDisabledGroup(columnFields.Count == 0);
        if (GUILayout.Button("変数を削除"))
        {
            columnFields.RemoveAt(columnFields.Count - 1);
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
        
        
        EditorGUILayout.Space(20);
        
        //生成開始ボタン
        if (GUILayout.Button("Generate"))
        {
            if (string.IsNullOrEmpty(soDataName))
            {
                EditorUtility.DisplayDialog("Error", "empty filename", "Ok");
                return;
            }

            // 入力値はそのままファイルパスとC#コードに埋め込まれるため、識別子として妥当なものに限定する
            if (!IsValidIdentifier(soDataName))
            {
                EditorUtility.DisplayDialog("Error", "データ名は英字または_で始まる英数字/_のみ使用できます", "Ok");
                return;
            }

            if (columnFields.Exists(column => !column.IsNullOrEmpty && !IsValidIdentifier(column.name)))
            {
                EditorUtility.DisplayDialog("Error", "変数名は英字または_で始まる英数字/_のみ使用できます", "Ok");
                return;
            }

            GenerateFiles();
        }
        
    }
    
    
    

    
    static bool IsValidIdentifier(string value)
    {
        return !string.IsNullOrEmpty(value) && Regex.IsMatch(value, "^[A-Za-z_][A-Za-z0-9_]*$");
    }

    //{sObjectName}.csと{repositoryName}.csを生成
    private void GenerateFiles()
    {
        const string soPath = "Assets/Project/Scripts/Infra";
        if (!Directory.Exists(soPath))
        {
            Directory.CreateDirectory(soPath);
        }

        var soFilePath = $"{soPath}/{sObjectName}.cs";
        var repoFilePath = $"Assets/Project/Scripts/Repository/{repositoryName}.cs";
        if (File.Exists(soFilePath) || File.Exists(repoFilePath))
        {
            EditorUtility.DisplayDialog("Error", "同名のスクリプトが既に存在します", "Ok");
            return;
        }

        string soCode = GenerateScriptableObjectCode();
        File.WriteAllText(soFilePath, soCode);
        
        
        const string repoPath = "Assets/Project/Scripts/Repository";
        if (!Directory.Exists(repoPath))
        {
            Directory.CreateDirectory(repoPath);
        }
        
        string repoCode = GenerateRepositoryCode();
        File.WriteAllText(repoFilePath, repoCode);
        
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Success", "Generated Scripts successfully\n Please Create Asset File Yourself", "Ok");
        this.Close();
    }

    //ScriptableObjectの定義コード
    //Project.Scripts.Infra内が前提
    private string GenerateScriptableObjectCode()
    {
        StringBuilder sb = new();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine();
        sb.AppendLine("namespace Project.Scripts.Infra");
        sb.AppendLine("{");
        sb.AppendLine($"    [CreateAssetMenu(fileName = \"{sObjectName}\", menuName = \"{menuName}\")]");
        sb.AppendLine($"    public class {sObjectName} : ScriptableObject");
        sb.AppendLine("    {");
        sb.AppendLine($"        public List<{soDataName}Data> data = new();");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    [Serializable]");
        sb.AppendLine($"    public class {soDataName}Data");
        sb.AppendLine("    {");
        foreach (var column in columnFields)
        {
            if (!column.IsNullOrEmpty) sb.AppendLine($"        public {column.type} {column.name};");
        }
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    //Repositoryのコード
    //Project.Scripts.Repository内が前提
    private string GenerateRepositoryCode()
    {
        StringBuilder sb = new();
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using Project.Scenes.QuestList.Scripts.Model;");
        sb.AppendLine("using Project.Scripts.Infra;");
        sb.AppendLine("using UnityEngine.AddressableAssets;");
        sb.AppendLine();
        sb.AppendLine("namespace Project.Scripts.Repository");
        sb.AppendLine("{");
        sb.AppendLine($"    public class {repositoryName} : ModelRepository");
        sb.AppendLine("    {");
        sb.AppendLine($"        public static {repositoryName} Instance {{ get; }} = new();");
        sb.AppendLine($"        public {repositoryName}()");
        sb.AppendLine("        {");
        sb.AppendLine();
        sb.AppendLine("        }");
        sb.AppendLine("        //必要に応じてGetとLoadData");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return  sb.ToString();
    }
    
    
}
