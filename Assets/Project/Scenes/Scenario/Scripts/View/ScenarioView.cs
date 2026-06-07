using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Scenes.Scenario.Scripts.View
{
    public class ScenarioView : MonoBehaviour
    {
        [SerializeField] Color grayedColor = new (0.5f, 0.5f, 0.5f, 1f);

        [SerializeField] TextMeshProUGUI characterNameText;
        [SerializeField] TextMeshProUGUI contentsText;
        [SerializeField] Image playerImage;
        [SerializeField] Image enemyImage;
        [SerializeField] Image faceImage;

        // キャラ名 → 位置(LL/RR) のマッピング
        readonly Dictionary<string, string> castPositions = new();

        public void ShowCastMessage(string characterName, string message)
        {
            // ChangeCastLayer();
            StopGrayingCast(characterName);
            ShowMessage(characterName, message);
        }

        public void ShowMessage(string characterName, string message)
        {
            characterNameText.text = characterName ?? "";
            contentsText.text = message ?? "";
        }

        public void ShowCast(string characterName, string unknownArg1, string faceExpression,
            string displayTime, string position, string unknownArg2,
            Sprite playerSprite, Sprite enemySprite, Sprite faceSprite)
        {
            // TODO: displayTime, unknownArgsの処理は後で実装

            castPositions[characterName] = position;

            ChangeFaceExpression(faceSprite);
            if (position == "LL")
            {
                playerImage.sprite = playerSprite;
                playerImage.SetNativeSize();
                playerImage.color = grayedColor;
                playerImage.gameObject.SetActive(true);
            }
            else if (position == "RR")
            {
                enemyImage.sprite = enemySprite;
                enemyImage.SetNativeSize();
                enemyImage.color = grayedColor;
                enemyImage.gameObject.SetActive(true);
            }
        }

        public void ChangeCastLayer(string characterName, int layer)
        {
            // TODO
        }

        public void ChangeFaceExpression(Sprite faceSprite)
        {
            if (faceSprite != null)
            {
                faceImage.sprite = faceSprite;
                faceImage.gameObject.SetActive(true);
            }
        }

        public void StopGrayingCast(string characterName)
        {
            if (!castPositions.TryGetValue(characterName, out var position)) return;

            if (position == "LL")
            {
                playerImage.color = Color.white;
                if (enemyImage.gameObject.activeSelf) enemyImage.color = grayedColor;
            }
            else if (position == "RR")
            {
                enemyImage.color = Color.white;
                if (playerImage.gameObject.activeSelf) playerImage.color = grayedColor;
            }
        }

        public void LogCommand(string function, string[] args)
        {
            Debug.Log($"[ScenarioView] Execute: {function}, Args: {string.Join(", ", args)}");
        }
        // TODO?: アニメーションをつける
    }
}
