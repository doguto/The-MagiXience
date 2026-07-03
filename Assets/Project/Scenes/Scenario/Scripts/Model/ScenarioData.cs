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
        public List<ScenarioStep> steps = new List<ScenarioStep>();
    }
}
