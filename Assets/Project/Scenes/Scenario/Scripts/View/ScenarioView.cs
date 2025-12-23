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
