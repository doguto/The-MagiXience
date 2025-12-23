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

        public void LogCommand(string function, string[] args)
        {
            Debug.Log($"[ScenarioView] Execute: {function}, Args: {string.Join(", ", args)}");
        }
        // TODO?: 喋ってないほうのStillを暗くする
        // TODO?: アニメーションをつける
    }
}
