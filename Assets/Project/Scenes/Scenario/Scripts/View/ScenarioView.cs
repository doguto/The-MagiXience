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

        /// <summary>
        /// ChangeCastLayerとStopGrayingCastの機能を内蔵してキャラ名とセリフを表示
        /// </summary>
        public void ShowCastMessage(string characterName, string message)
        {
            ChangeCastLayer(characterName, 1);
            StopGrayingCast(characterName);
            ShowMessage(characterName, message);
        }

        /// <summary>
        /// キャラ名とセリフを表示
        /// </summary>
        public void ShowMessage(string characterName, string message)
        {
            characterNameText.text = characterName ?? "";
            contentsText.text = message ?? "";
        }

        /// <summary>
        /// キャラクターを表示する
        /// </summary>
        /// <param name="characterName">キャラ名</param>
        /// <param name="unknownArg1">不明な引数1</param>
        /// <param name="faceExpression">表情差分</param>
        /// <param name="displayTime">表示までの時間(s)</param>
        /// <param name="position">表示位置（LL or RR）</param>
        /// <param name="unknownArg2">不明な引数2</param>
        /// <param name="playerSprite">プレイヤーのSprite</param>
        /// <param name="enemySprite">敵のSprite</param>
        public void ShowCast(string characterName, string unknownArg1, string faceExpression, 
            string displayTime, string position, string unknownArg2, 
            Sprite playerSprite, Sprite enemySprite)
        {
            // TODO: displayTime, faceExpression, unknownArgsの処理は後で実装
            
            // 位置に応じてSpriteを表示
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

        /// <summary>
        /// 指定したキャラのレイヤーを変更
        /// </summary>
        public void ChangeCastLayer(string characterName, int layer)
        {
            // TODO
        }

        /// <summary>
        /// キャラにかかっている灰色のフィルターを外す（=スポットライトを当てる）
        /// </summary>
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
