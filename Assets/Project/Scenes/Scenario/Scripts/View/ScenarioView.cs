using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Scenes.Scenario.Scripts.View
{
    public class ScenarioView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI characterNameText;
        [SerializeField] TextMeshProUGUI contentsText;
        [SerializeField] Image playerImage;
        [SerializeField] Image enemyImage;
        [SerializeField] Image faceImage;

        public void ShowCastMessage(string characterName, string message)
        {
            // TODO: まだ関数の中身は未実装
            // ChangeCastLayer(characterName, 1);
            // StopGrayingCast(characterName);
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

            // 位置に応じてSpriteを表示
            // TODO: 位置は定数で定義
            ChangeFaceExpression(faceSprite);
            if (position == "LL")
            {
                // 左側 = プレイヤー
                playerImage.sprite = playerSprite;
                playerImage.SetNativeSize();
                playerImage.gameObject.SetActive(true);
            }
            else if (position == "RR")
            {
                // 右側 = 敵キャラ
                enemyImage.sprite = enemySprite;
                enemyImage.SetNativeSize();
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
            // TODO
        }

        public void LogCommand(string function, string[] args)
        {
            Debug.Log($"[ScenarioView] Execute: {function}, Args: {string.Join(", ", args)}");
        }
        // TODO?: 喋ってないほうのStillを暗くする
        // TODO?: アニメーションをつける
    }
}
