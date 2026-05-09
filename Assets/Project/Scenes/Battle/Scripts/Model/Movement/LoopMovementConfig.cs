using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Movement
{
    /// <summary>
    /// 複数の移動プリセット/インラインステップを順番に実行し、
    /// 全体をループさせたり、個別にリピート回数を指定できる移動ステップ。
    ///
    /// 内部では各ステップを逐次 Play → await し、
    /// 全エントリ完了後にループ設定に従って繰り返す。
    /// 外側から見ると1つの Tween（制御用ダミー）として扱える。
    /// Tween を Kill すると実行中の内部ステップも停止する。
    /// </summary>
    [Serializable]
    public class LoopMovementConfig : IMovementStep
    {
        [SerializeField, Tooltip("全体をループさせる")]
        bool loop = true;

        [SerializeField, Tooltip("全体のループ回数（0 = 無限ループ）。loop が true のときのみ有効"), Min(0)]
        int loopCount;

        [SerializeField]
        List<LoopMovementEntry> entries = new();

        // ScriptableObjectで同一インスタンスが共有されるケースで、
        // controlTween.OnKill経由のキャンセルが効かないことがあるため、
        // 外部から明示的に停止できるよう直近のCTSを保持する。
        CancellationTokenSource lastCts;

        public Tween Play(Transform target, Vector2 direction, Animator animator)
        {
            var cts = new CancellationTokenSource();
            lastCts = cts;

            var controlTween = DOVirtual.Float(0f, 1f, 999999f, _ => { });
            controlTween.OnKill(() =>
            {
                if (!cts.IsCancellationRequested) cts.Cancel();
            });

            RunLoop(controlTween, cts.Token, target, direction, animator).Forget();
            return controlTween;
        }

        /// <summary>
        /// Playで起動したRunLoopを外部から強制停止する。
        /// </summary>
        public void ForceStop()
        {
            // Cancel()がDOTweenのコールバック→RunLoop末尾を同期実行することがあり、
            // その流れでlastCtsが書き換えられた場合のNREを避けるためローカルに退避する。
            var ctsToCancel = lastCts;
            if (ctsToCancel == null) return;
            lastCts = null;
            ctsToCancel.Cancel();
            ctsToCancel.Dispose();
        }

        async UniTaskVoid RunLoop(
            Tween controlTween,
            CancellationToken ct,
            Transform target,
            Vector2 direction,
            Animator animator)
        {
            int iteration = 0;

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    foreach (var entry in entries)
                    {
                        if (entry == null) continue;
                        ct.ThrowIfCancellationRequested();

                        var steps = entry.GetSteps();
                        if (steps == null || steps.Count == 0) continue;

                        int repeat = Mathf.Max(1, entry.RepeatCount);
                        for (int r = 0; r < repeat; r++)
                        {
                            ct.ThrowIfCancellationRequested();

                            foreach (var step in steps)
                            {
                                if (step == null) continue;
                                ct.ThrowIfCancellationRequested();

                                var tween = step.Play(target, direction, animator);
                                if (tween == null) continue;

                                await tween.ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, ct);
                            }
                        }
                    }

                    iteration++;

                    if (!loop) break;
                    if (loopCount > 0 && iteration >= loopCount) break;
                }
            }
            catch (OperationCanceledException) { }

            // 全ステップ完了 → 制御 Tween を終了
            if (controlTween.IsActive())
            {
                controlTween.Kill();
            }
        }
    }

    /// <summary>
    /// LoopMovementConfig 内の1エントリ。
    /// プリセット参照またはインラインステップのどちらかを使用する（プリセット優先）。
    /// </summary>
    [Serializable]
    public class LoopMovementEntry
    {
        [SerializeField, Tooltip("プリセットを使用する場合に設定（インラインより優先）")]
        MovementPreset preset;

        [SerializeReference, SubclassSelector, Tooltip("インラインで直接ステップを定義する場合")]
        List<IMovementStep> inlineSteps = new();

        [SerializeField, Tooltip("このエントリの繰り返し回数（1 = 1回実行）"), Min(1)]
        int repeatCount = 1;

        public int RepeatCount => repeatCount;

        public IReadOnlyList<IMovementStep> GetSteps()
        {
            if (preset != null) return preset.Steps;
            return inlineSteps;
        }
    }
}
