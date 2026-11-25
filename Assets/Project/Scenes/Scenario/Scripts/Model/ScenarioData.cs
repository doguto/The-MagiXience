using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Scenes.Scenario.Scripts.Model
{
    [Serializable]
    public class ScenarioStep
    {
        public string function;
        public string[] args;
    }

    [CreateAssetMenu(fileName = "NewScenarioData", menuName = "Scenario/ScenarioData")]
    public class ScenarioData : ScriptableObject
    {
        // 会話シーン自身が処理する演出。これ以外の行はコマンドとしてバトル側へ通知する
        static readonly HashSet<string> PresentationFunctions = new()
        {
            "ShowCastMessage",
            "ShowMessage",
            "TypeWriter",
            "AutoAdvance",
            "ShowCast",
            "HideCast",
            "ChangeCastAnimation",
            "ShakeCastV",
        };

        public List<ScenarioStep> steps = new List<ScenarioStep>();

        public static bool IsPresentation(string function) => PresentationFunctions.Contains(function);

        public IEnumerable<ScenarioStep> Commands
        {
            get
            {
                foreach (var step in steps)
                {
                    if (step != null && !string.IsNullOrEmpty(step.function) && !IsPresentation(step.function)) yield return step;
                }
            }
        }
    }
}
