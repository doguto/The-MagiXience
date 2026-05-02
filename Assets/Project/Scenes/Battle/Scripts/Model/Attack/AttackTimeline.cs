using System;
using System.Collections.Generic;
using Project.Scripts.Extensions;
using UniRx;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    [Serializable]
    public class AttackTimeline : IAttackStrategy
    {
        [SerializeField] bool loop;
        [SerializeField] float loopStart;
        [SerializeField] float loopEnd = 5f;
        [SerializeField, Min(0.01f)] float cycleDuration = 2f;
        [SerializeField] List<AttackTimelineEntry> entries = new();

        Subject<AttackEvent> onAttackTiming;
        CompositeDisposable disposables;
        Func<Vector3> getPlayerPosition;
        Func<Vector3> getEnemyPosition;
        Func<Quaternion> getEnemyRotation;

        public IObservable<AttackEvent> OnAttackTiming => onAttackTiming;
        public bool IsCompleted { get; private set; }

        public void InitializeProviders(Func<Vector3> getPlayerPosition, Func<Vector3> getEnemyPosition, Func<Quaternion> getEnemyRotation)
        {
            this.getPlayerPosition = getPlayerPosition;
            this.getEnemyPosition = getEnemyPosition;
            this.getEnemyRotation = getEnemyRotation;

            foreach (var entry in entries)
            {
                entry.directionProvider?.Initialize(getPlayerPosition, getEnemyPosition, getEnemyRotation);
            }
        }

        public void Initialize()
        {
            onAttackTiming = new Subject<AttackEvent>();
            disposables = new CompositeDisposable();
            IsCompleted = false;

            if (entries.Count == 0) return;

            if (loop)
            {
                int totalCycles = Mathf.FloorToInt((loopEnd - loopStart) / cycleDuration);

                for (int cycle = 0; cycle <= totalCycles; cycle++)
                {
                    foreach (var entry in entries)
                    {
                        float fireTime = loopStart + cycle * cycleDuration + entry.time;
                        if (fireTime > loopEnd) continue;

                        ScheduleEntry(entry, fireTime);
                    }
                }

                Observable.Timer(TimeSpan.FromSeconds(loopEnd))
                    .Subscribe(_ => IsCompleted = true)
                    .AddTo(disposables);
            }
            else
            {
                float maxTime = 0f;
                foreach (var entry in entries)
                {
                    ScheduleEntry(entry, entry.time);
                    if (entry.time > maxTime) maxTime = entry.time;
                }

                Observable.Timer(TimeSpan.FromSeconds(maxTime))
                    .Subscribe(_ => IsCompleted = true)
                    .AddTo(disposables);
            }
        }

        const int MaxPresetDepth = 8;

        void ScheduleEntry(AttackTimelineEntry entry, float fireTime, int depth = 0)
        {
            if (entry.signal is PresetAttackSignal presetSignal)
            {
                if (presetSignal.Preset == null) return;
                if (depth >= MaxPresetDepth)
                {
                    Debug.LogError("[AttackTimeline] Preset nesting depth limit reached. Circular reference?");
                    return;
                }
                ExpandPreset(presetSignal, entry.seType, fireTime, depth + 1);
                return;
            }

            Observable.Timer(TimeSpan.FromSeconds(fireTime))
                .Subscribe(_ =>
                {
                    if (entry.signal != null)
                    {
                        onAttackTiming.OnNext(entry.signal.CreateEvent(entry.directionProvider, entry.bulletPoolIndex, entry.seType));
                    }
                })
                .AddTo(disposables);
        }

        void ExpandPreset(PresetAttackSignal signal, SeType parentSeType, float baseTime, int depth)
        {
            var timeline = signal.Preset.CreateTimeline();
            if (timeline == null || timeline.entries.Count == 0) return;

            // 展開したエントリのDirectionProviderを初期化
            foreach (var inner in timeline.entries)
            {
                inner.directionProvider?.Initialize(getPlayerPosition, getEnemyPosition, getEnemyRotation);
                // 内側がNoneなら外側のseTypeを引き継ぐ
                if (inner.seType == SeType.None && parentSeType != SeType.None)
                {
                    inner.seType = parentSeType;
                }
            }

            var totalCycles = signal.Loop && signal.LoopCount > 0 ? signal.LoopCount : 1;
            for (var cycle = 0; cycle < totalCycles; cycle++)
            {
                foreach (var inner in timeline.entries)
                {
                    var innerTime = baseTime + cycle * signal.CycleDuration + inner.time;
                    ScheduleEntry(inner, innerTime, depth);
                }
            }
        }

        public AttackTimeline DeepCopy()
        {
            var copy = new AttackTimeline
            {
                loop = loop,
                loopStart = loopStart,
                loopEnd = loopEnd,
                cycleDuration = cycleDuration,
                entries = new List<AttackTimelineEntry>(entries.Count)
            };
            foreach (var entry in entries)
            {
                copy.entries.Add(entry.DeepCopy());
            }
            return copy;
        }

        public void Update(float deltaTime) { }

        public void Dispose()
        {
            disposables?.Dispose();
            onAttackTiming?.Dispose();
        }
    }
}
