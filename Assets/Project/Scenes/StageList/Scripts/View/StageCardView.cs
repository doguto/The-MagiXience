using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Project.Scenes.StageList.Scripts.View
{
    // クリア時の外枠の種類。下に行くほどえらい
    public enum ClearFrameType
    {
        None,
        Normal,
        NoContinue,
        NoDamage,
    }

    public class StageCardView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI stageTitle;
        [SerializeField] TextMeshProUGUI stageIndexText;

        [Header("Clear Frame")]
        [SerializeField] SpriteRenderer clearFrame;
        [SerializeField] Color normalFrameColor = new(0.8f, 0.5f, 0.25f);
        [SerializeField] Color noContinueFrameColor = new(0.85f, 0.88f, 0.95f);
        [SerializeField] Color noDamageFrameColor = new(1f, 0.84f, 0.2f);
 
        public void Setup((string id, string title) stage, bool isOpened)
        {
            stageIndexText.text = $"Stage.{stage.id}";
            stageTitle.text = isOpened ? stage.title : "？？？？？？？？";
        }

        // クリア状況に応じた外枠を表示する。未クリアなら非表示
        public void SetClearFrame(ClearFrameType type)
        {
            if (clearFrame == null) return;

            clearFrame.gameObject.SetActive(type != ClearFrameType.None);
            clearFrame.color = type switch
            {
                ClearFrameType.NoDamage => noDamageFrameColor,
                ClearFrameType.NoContinue => noContinueFrameColor,
                _ => normalFrameColor
            };
        }
    }
}
