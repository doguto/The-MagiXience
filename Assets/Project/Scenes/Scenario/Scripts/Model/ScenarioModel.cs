using System.Collections.Generic;
using Project.Scripts.Model;
using Project.Scripts.Repository.AssetRepository;
using UnityEngine;

namespace Project.Scenes.Scenario.Scripts.Model
{
    public class ScenarioModel : ModelBase
    {
        readonly StillAssetRepository StillAssetRepository = new();

        // TODO: 現在進行中のステージ番号の管理・取得方法を考える
        readonly int clearedStageNumber;
        Sprite playerStillSprite;
        Sprite enemyStillSprite;
        Sprite playerFaceSprite;
        Sprite enemyFaceSprite;
        public ScenarioModel(int clearedStageNumber)
        {
            this.clearedStageNumber = clearedStageNumber;
        }

        public ScenarioStep CurrentStep => steps[currentIndex];
        public bool IsEnd => currentIndex >= steps.Count;

        List<ScenarioStep> steps;
        int currentIndex;


        public void LoadData(List<ScenarioStep> steps)
        {
            this.steps = steps;
            currentIndex = 0;
        }

        public void Next()
        {
            currentIndex++;
        }
    }
}
