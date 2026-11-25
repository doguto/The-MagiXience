using System;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Presenter;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    [Serializable]
    public sealed class WindServiceDefinition : StageServiceDefinition
    {
        [SerializeField, Tooltip("ステージ開始時の風。Intervals が空なら無風で始まる")] BattleWindSettings initial = new();
        [SerializeField, Tooltip("風向きを示す矢印。ステージ開始時に背景の下へ生成する")] WindArrowsPresenter arrowsPrefab;

        public BattleWindSettings Initial => initial;

        public override IStageService Create() => new WindService(initial, arrowsPrefab);
    }
}
