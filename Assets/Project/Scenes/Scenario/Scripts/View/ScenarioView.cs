using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    // TODO?: 喋ってないほうのStillを暗くする
    // TODO?: アニメーションをつける
}
