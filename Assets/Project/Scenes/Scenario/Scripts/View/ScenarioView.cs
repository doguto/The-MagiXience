using System;
using System.Collections.Generic;
using DG.Tweening;
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

        // 文字送り設定
        bool isTypeWriterEnabled;
        float typeWriterCharactersPerSecond;
        Tween typingTween;
        Action onTypingCompleted;

        /// <summary>文字送り中かどうか</summary>
        public bool IsTyping => typingTween != null;

        public bool IsTypeWriterEnabled => isTypeWriterEnabled;

        /// <summary>文字送りで1文字を表示する間隔(秒)</summary>
        public float TypeWriterCharacterInterval => 1f / typeWriterCharactersPerSecond;

        public void SetTypeWriter(bool enabled, float charactersPerSecond)
        {
            isTypeWriterEnabled = enabled;
            typeWriterCharactersPerSecond = charactersPerSecond;
        }

        /// <param name="onDisplayed">全文の表示が完了したときに呼ばれる</param>
        public void ShowCastMessage(string characterName, string message, Action onDisplayed = null)
        {
            // ChangeCastLayer();
            StopGrayingCast(characterName);
            ShowMessage(characterName, message, onDisplayed);
        }

        /// <param name="onDisplayed">全文の表示が完了したときに呼ばれる</param>
        public void ShowMessage(string characterName, string message, Action onDisplayed = null)
        {
            characterNameText.text = characterName ?? "";
            contentsText.text = message ?? "";

            typingTween?.Kill();
            typingTween = null;
            contentsText.maxVisibleCharacters = int.MaxValue;

            if (!isTypeWriterEnabled)
            {
                onDisplayed?.Invoke();
                return;
            }

            // リッチテキストタグを除いた表示文字数を取得するためメッシュを更新する
            contentsText.ForceMeshUpdate();
            var totalCharacters = contentsText.textInfo.characterCount;
            if (totalCharacters == 0)
            {
                onDisplayed?.Invoke();
                return;
            }

            onTypingCompleted = onDisplayed;
            contentsText.maxVisibleCharacters = 0;
            typingTween = DOTween.To(() => contentsText.maxVisibleCharacters,
                                     x => contentsText.maxVisibleCharacters = x,
                                     totalCharacters,
                                     totalCharacters / typeWriterCharactersPerSecond)
                                 .SetEase(Ease.Linear)
                                 .SetLink(gameObject)
                                 .OnComplete(FinishTyping);
        }

        /// <summary>文字送りを打ち切って全文を表示する</summary>
        public void CompleteTyping()
        {
            if (typingTween == null) return;

            typingTween.Kill();
            FinishTyping();
        }

        void FinishTyping()
        {
            typingTween = null;
            contentsText.maxVisibleCharacters = int.MaxValue;

            var callback = onTypingCompleted;
            onTypingCompleted = null;
            callback?.Invoke();
        }

        public void ShowCast(string characterName, string stillType, string faceExpression,
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

        public void HideCast(string characterName, float fadeDuration)
        {
            if (!castPositions.TryGetValue(characterName, out var position)) return;

            Image targetImage = position switch
            {
                "LL" => playerImage,
                "RR" => enemyImage,
                _ => null
            };

            if (targetImage != null)
            {
                // 再表示時の競合を避けるため、進行中のTweenをkillしてからフェードアウト
                targetImage.DOKill();
                targetImage.DOFade(0f, fadeDuration)
                           .OnComplete(() => targetImage.gameObject.SetActive(false));
            }

            castPositions.Remove(characterName);
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
