using System;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    /// <summary>
    /// 中心座標（anchor）の周りを「移動→停止→移動→停止→…」と繰り返す移動ステップ。
    /// ボスや中ボスなど、長く居座る敵に向いている。
    ///
    /// 移動先の計算:
    ///   randomDir  = ランダム角度の単位ベクトル * moveDistance
    ///   pull       = (anchor - 現在位置).normalized * pullStrength
    ///   candidate  = 現在位置 + pull + randomDir
    ///   → candidate が moveBounds の範囲外なら範囲内にクランプ
    /// </summary>
    [Serializable]
    public class DriftMovementConfig : IMovementStep
    {
        [SerializeField, Tooltip("中心座標（ワールド座標）")]
        Vector3 anchor = new Vector3(-2f, 0f, 0f);

        [SerializeField, Tooltip("中心に引き寄せる力の強さ")]
        float pullStrength = 0.5f;

        [SerializeField, Min(0.01f), Tooltip("1回の移動距離（固定長）")]
        float moveDistance = 0.5f;

        [SerializeField, Tooltip("anchor を中心とした移動可能範囲（横幅, 縦幅）")]
        Vector2 moveBounds = new Vector2(3f, 2f);

        [SerializeField, Min(0.01f), Tooltip("移動にかける秒数")]
        float moveDuration = 0.6f;

        [SerializeField, Min(0f), Tooltip("移動後の停止時間（秒）")]
        float pauseDuration = 0.3f;

        [SerializeField, Tooltip("移動のイージング")]
        Ease moveEase = Ease.InOutSine;

        [SerializeField, Min(0f), Tooltip("全体の継続時間（秒）。0で無限。")]
        float duration = 0f;

        public Tween Play(Transform target, Vector2 direction, Animator animator)
        {
            float cycleDuration = moveDuration + pauseDuration;
            Vector3 moveStart = Vector3.zero;
            Vector3 moveEnd = Vector3.zero;
            float cycleElapsed = 0f;

            return PullMovementHelper.Create(target, duration,
                onStart: t =>
                {
                    moveStart = t.position;
                    moveEnd = CalcNextTarget(moveStart);
                },
                onUpdate: (t, dt) =>
                {
                    cycleElapsed += dt;

                    if (cycleElapsed >= cycleDuration)
                    {
                        // サイクル完了 → 次の移動先を決める
                        t.position = moveEnd;
                        moveStart = moveEnd;
                        moveEnd = CalcNextTarget(moveStart);
                        cycleElapsed -= cycleDuration;
                    }

                    if (cycleElapsed < moveDuration)
                    {
                        // 移動フェーズ
                        float rawT = Mathf.Clamp01(cycleElapsed / moveDuration);
                        float easedT = DOVirtual.EasedValue(0f, 1f, rawT, moveEase);
                        t.position = Vector3.Lerp(moveStart, moveEnd, easedT);
                    }
                    else
                    {
                        // 停止フェーズ
                        t.position = moveEnd;
                    } 
                }, ease: Ease.Linear);
        }

        Vector3 CalcNextTarget(Vector3 current)
        {
            // pull: anchor 方向へ引き寄せ
            Vector3 toAnchor = anchor - current;
            Vector3 pull = toAnchor.normalized * Mathf.Min(pullStrength, toAnchor.magnitude);

            // ランダム方向（角度のみランダム、長さは任意）
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 randomDir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

            // pull + random の合算方向を維持しつつ、長さを moveDistance に固定
            Vector3 combined = pull + randomDir;
            Vector3 offset = combined.sqrMagnitude > 0f
                ? combined.normalized * moveDistance
                : Vector3.right * moveDistance;

            Vector3 candidate = current + offset;

            // moveBounds 内にクランプ
            float halfW = moveBounds.x * 0.5f;
            float halfH = moveBounds.y * 0.5f;
            candidate.x = Mathf.Clamp(candidate.x, anchor.x - halfW, anchor.x + halfW);
            candidate.y = Mathf.Clamp(candidate.y, anchor.y - halfH, anchor.y + halfH);

            return candidate;
        }
    }
}
