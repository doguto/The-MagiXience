using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Project.Scenes.StageList.Scripts.View
{
    public class StageCardView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI stageTitle;
        [SerializeField] TextMeshProUGUI stageIndexText;
 
        public void Setup((string id, string title) stage, bool isOpened, bool isLastStage)
        {
            // 最終面(EX)だけ区切りをコロンにする
            stageIndexText.text = isLastStage ? $"Phase:{stage.id}." : $"Phase {stage.id}.";
            stageTitle.text = isOpened ? stage.title : "？？？？？？？？";
        }
    }
}
