using System.Collections.Generic;
using Project.Scripts.Model;
using Project.Scripts.Repository.AssetRepository;
using UnityEngine;

namespace Project.Scenes.Scenario.Scripts.Model
{
    public class ScenarioModel : ModelBase
    {
        readonly StillAssetRepository stillAssetRepository = new();

        public Sprite PlayerStillSprite { get; private set; }
        public Sprite EnemyStillSprite { get; private set; }

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

        /// <summary>
        /// キャラクター画像を読み込む
        /// </summary>
        /// <param name="enemyCharaName">敵キャラクター名</param>
        public void LoadCharacterSprites(string enemyCharaName)
        {
            // プレイヤー（テン）のStillをロード
            PlayerStillSprite = stillAssetRepository.Load("Ten", false);
            
            // 敵キャラのStillをロード
            EnemyStillSprite = stillAssetRepository.Load(enemyCharaName, false);
        }
    }
}
