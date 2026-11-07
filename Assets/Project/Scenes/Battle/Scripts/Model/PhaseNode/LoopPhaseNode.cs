using System;
using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.Model.ExitCondition;
using UniRx;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.PhaseNode
{
    [Serializable]
    public class LoopPhaseNode : IPhaseNode
    {
        [SerializeReference, SubclassSelector]
        IPhaseNode child;
        [Tooltip("0 = 無限")]
        [SerializeField, Min(0)] int loopCount;
        [Tooltip("成立したらループを抜ける。未設定ならloopCountのみで終了")]
        [SerializeReference, SubclassSelector]
        IExitConditionConfig exitCondition;
        [Tooltip("false: 周の区切りで判定する / true: 成立した瞬間に実行中のフェーズを打ち切り、その周の残りも飛ばして抜ける")]
        [SerializeField] bool interrupt;

        public LoopPhaseNode() { }

        public LoopPhaseNode(IPhaseNode child, int loopCount, IExitConditionConfig exitCondition = null, bool interrupt = false)
        {
            this.child = child;
            this.loopCount = loopCount;
            this.exitCondition = exitCondition;
            this.interrupt = interrupt;
        }

        public string Label => $"Loop({(loopCount == 0 ? "∞" : loopCount.ToString())}) {child?.Label}";

        public IEnumerable<BattlePhaseDefinition> Enumerate(PhaseNodeContext context)
        {
            if (child == null) yield break;

            var exitRequested = false;
            BattlePhaseModelBase watcher = null;
            IDisposable watcherSubscription = null;

            try
            {
                if (exitCondition != null)
                {
                    watcher = context.CreateWatcher(exitCondition);
                    if (watcher != null)
                    {
                        // Enter() 内で即成立するケースがあるため、購読を先に済ませる
                        watcherSubscription = watcher.OnExitPhase
                            .Take(1)
                            .Subscribe(_ =>
                            {
                                exitRequested = true;
                                if (interrupt) context.InterruptCurrentPhase();
                            });
                        watcher.Enter();
                    }
                }

                for (var i = 0; loopCount == 0 || i < loopCount; i++)
                {
                    if (exitRequested) yield break;

                    var yieldedAny = false;
                    foreach (var definition in child.Enumerate(context))
                    {
                        if (exitRequested && interrupt) yield break;

                        yieldedAny = true;
                        yield return definition;

                        // 外側のノードほど先に再開されるため、入れ子のLoopでも外側の打ち切りが優先される
                        if (exitRequested && interrupt) yield break;
                    }

                    // 子が1つもフェーズを返さない場合の無限ループ防止
                    if (!yieldedAny) yield break;
                }
            }
            finally
            {
                watcherSubscription?.Dispose();
                if (watcher != null)
                {
                    watcher.Exit();
                    watcher.Dispose();
                }
            }
        }
    }
}
