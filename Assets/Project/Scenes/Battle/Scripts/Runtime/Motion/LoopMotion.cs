using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    // 旧 LoopMovementConfig。エントリ (プリセット or インライン) を順に繰り返す
    [Serializable]
    public sealed class LoopMotion : MotionStep
    {
        [Serializable]
        public sealed class Entry
        {
            [Tooltip("指定するとインラインより優先")] public MotionPreset preset;
            [SerializeReference, SubclassSelector] public List<MotionStep> inlineSteps = new();
            [Min(1)] public int repeatCount = 1;

            public IReadOnlyList<MotionStep> Steps => preset != null ? preset.Steps : inlineSteps;
        }

        [SerializeField] bool loop = true;
        [SerializeField, Tooltip("0 で無限")] int loopCount;
        [SerializeField] List<Entry> entries = new();

        public override string Summary => loop ? (loopCount > 0 ? $"ループ ×{loopCount}" : "ループ ∞") : "1 周";

        public override MotionRunner CreateRunner() => new LoopRunner(this);

        sealed class LoopRunner : MotionRunner
        {
            readonly LoopMotion def;
            readonly List<IReadOnlyList<MotionStep>> playlist = new();
            int index;
            int pass;
            MotionSequence current;

            public LoopRunner(LoopMotion def) => this.def = def;

            protected override void OnBegin()
            {
                foreach (var entry in def.entries)
                {
                    if (entry == null) continue;
                    for (var i = 0; i < Mathf.Max(1, entry.repeatCount); i++) playlist.Add(entry.Steps);
                }
            }

            public override bool Tick(float dt)
            {
                if (playlist.Count == 0) return true;

                // 1 フレームで空のエントリを延々回さないよう、進める回数に上限を設ける
                for (var guard = 0; guard <= playlist.Count; guard++)
                {
                    current ??= new MotionSequence(playlist[index], Body, new MotionLaunch { Direction = Direction, AttackTime = AttackTime });
                    current.Tick(dt);
                    if (!current.IsFinished) return false;

                    current = null;
                    dt = 0f;
                    if (++index < playlist.Count) continue;

                    index = 0;
                    pass++;
                    if (!def.loop || (def.loopCount > 0 && pass >= def.loopCount)) return true;
                }
                return false;
            }

            public override void Stop() => current?.Stop();
        }
    }
}
