using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Scenes.Scenario.Scripts.View
{
    public class ScenarioView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI characterNameText;
        [SerializeField] TextMeshProUGUI contentsText;
        [SerializeField] Image playerSpriteRenderer;
        [SerializeField] Image enemySpriteRenderer;
        [SerializeField] Image faceSpriteRenderer;

        public void Init(string characterName, string contents, Sprite playerStill, Sprite enemyStill, Sprite face)
        {
            UpdateCanvas(characterName, contents, playerStill, enemyStill, face);
        }

        public void UpdateCanvas(string characterName, string contents, Sprite playerStill, Sprite enemyStill, Sprite face)
        {
            characterNameText.text = characterName;
            contentsText.text = contents;
            playerSpriteRenderer.sprite = playerStill;
            enemySpriteRenderer.sprite = enemyStill;
            faceSpriteRenderer.sprite = face;
        }

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
            Sprite playerSprite, Sprite enemySprite)
        {
            // TODO: displayTime, faceExpression, unknownArgsの処理は後で実装
            
            // 位置に応じてSpriteを表示
            // TODO: 位置は定数で定義
            if (position == "LL")
            {
                // 左側 = プレイヤー
                playerSpriteRenderer.sprite = playerSprite;
                playerSpriteRenderer.gameObject.SetActive(true);
            }
            else if (position == "RR")
            {
                // 右側 = 敵キャラ
                enemySpriteRenderer.sprite = enemySprite;
                enemySpriteRenderer.gameObject.SetActive(true);
            }
        }

        public void ChangeCastLayer(string characterName, int layer)
        {
            // TODO
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
