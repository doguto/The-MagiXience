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
        // ノーコンは白い枠画像に色を掛けて使い、通常・ノーダメは枠画像の色をそのまま使う
        [SerializeField] Sprite plainFrameSprite;
        [SerializeField] Sprite noContinueFrameSprite;
        [SerializeField] Sprite noDamageFrameSprite;
        [SerializeField] Color normalFrameColor = Color.white;
        [SerializeField] Color noContinueFrameColor = new(1f, 0.84f, 0.2f);

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
            clearFrame.sprite = type switch
            {
                ClearFrameType.NoDamage => noDamageFrameSprite,
                ClearFrameType.NoContinue => noContinueFrameSprite,
                _ => plainFrameSprite
            };
            clearFrame.color = type switch
            {
                ClearFrameType.NoDamage => Color.white,
                ClearFrameType.NoContinue => noContinueFrameColor,
                _ => normalFrameColor
            };
        }
    }
}
