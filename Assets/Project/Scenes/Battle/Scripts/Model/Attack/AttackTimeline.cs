using System;
using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.Model.Attack.PhaseTrigger;
using Project.Scenes.Battle.Scripts.Model.Movement;
using Project.Scripts.Extensions;
using UniRx;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Model.Attack
{
    [Serializable]
    public class AttackTimeline : IAttackStrategy
    {
        [SerializeField] List<AttackPhase> phases = new() { new AttackPhase() };

        Subject<AttackEvent> onAttackTiming;
        CompositeDisposable disposables;
        CompositeDisposable phaseDisposables;
        Func<Vector3> getPlayerPosition;
        Func<Vector3> getEnemyPosition;
        Func<Quaternion> getEnemyRotation;
        IReadOnlyReactiveProperty<int> currentHp;
        int maxHp;

        public IObservable<AttackEvent> OnAttackTiming => onAttackTiming;
        public bool IsCompleted { get; private set; }

        public void InitializeProviders(Func<Vector3> getPlayerPosition, Func<Vector3> getEnemyPosition, Func<Quaternion> getEnemyRotation, IReadOnlyReactiveProperty<int> currentHp = null, int maxHp = 0)
        {
            this.getPlayerPosition = getPlayerPosition;
            this.getEnemyPosition = getEnemyPosition;
            this.getEnemyRotation = getEnemyRotation;
            this.currentHp = currentHp;
            this.maxHp = maxHp;

            foreach (var phase in phases)
            {
                foreach (var entry in phase.entries)
                {
                    InitializeEntryProviders(entry);
                }
            }
        }

        void InitializeEntryProviders(AttackTimelineEntry entry)
        {
            entry.directionProvider?.Initialize(getPlayerPosition, getEnemyPosition, getEnemyRotation);
            entry.rotationProvider.Initialize(getPlayerPosition, getEnemyPosition, getEnemyRotation);
        }

        public void Initialize()
        {
            onAttackTiming = new Subject<AttackEvent>();
            disposables = new CompositeDisposable();
            IsCompleted = false;

            if (phases.Count == 0) return;

            StartPhase(0);
        }

        // Phaseを配列の先頭から順番に進める。nextPhaseTriggerがSubscribe直後に条件を満たしていれば
        // (例: 開始済みの敵にいきなり低いHP閾値のPresetを差し替えた場合)そのまま連鎖して先のPhaseまで進む。
        void StartPhase(int index)
        {
            var phase = phases[index];

            if (phaseDisposables != null)
            {
                disposables.Remove(phaseDisposables);
                phaseDisposables.Dispose();
            }
            phaseDisposables = new CompositeDisposable();
            disposables.Add(phaseDisposables);

            bool isLastPhase = index >= phases.Count - 1;
            ScheduleEntries(phase, isLastPhase);

            if (!isLastPhase && phase.nextPhaseTrigger != null)
            {
                var context = new AttackPhaseTriggerContext(currentHp, maxHp);
                phase.nextPhaseTrigger.CreateTrigger(context)
                    .Subscribe(_ => StartPhase(index + 1))
                    .AddTo(phaseDisposables);
            }
        }

        void ScheduleEntries(AttackPhase phase, bool isLastPhase)
        {
            if (phase.entries.Count == 0) return;

            if (phase.loop)
            {
                int totalCycles = Mathf.FloorToInt((phase.loopEnd - phase.loopStart) / phase.cycleDuration);

                for (int cycle = 0; cycle <= totalCycles; cycle++)
                {
                    foreach (var entry in phase.entries)
                    {
                        float fireTime = phase.loopStart + cycle * phase.cycleDuration + entry.time;
                        if (fireTime > phase.loopEnd) continue;

                        ScheduleEntry(entry, fireTime);
                    }
                }

                if (isLastPhase)
                {
                    Observable.Timer(TimeSpan.FromSeconds(phase.loopEnd))
                        .Subscribe(_ => IsCompleted = true)
                        .AddTo(phaseDisposables);
                }
            }
            else
            {
                float maxTime = 0f;
                foreach (var entry in phase.entries)
                {
                    ScheduleEntry(entry, entry.time);
                    if (entry.time > maxTime) maxTime = entry.time;
                }

                if (isLastPhase)
                {
                    Observable.Timer(TimeSpan.FromSeconds(maxTime))
                        .Subscribe(_ => IsCompleted = true)
                        .AddTo(phaseDisposables);
                }
            }
        }

        const int MaxPresetDepth = 8;

        void ScheduleEntry(AttackTimelineEntry entry, float fireTime, int depth = 0, MovementPreset inheritedBulletMovement = null)
        {
            if (entry.signal is PresetAttackSignal presetSignal)
            {
                if (presetSignal.Preset == null) return;
                if (depth >= MaxPresetDepth)
                {
                    Debug.LogError("[AttackTimeline] Preset nesting depth limit reached. Circular reference?");
                    return;
                }
                ExpandPreset(presetSignal, entry.seType, fireTime, depth + 1,
                    entry.bulletMovementOverride != null ? entry.bulletMovementOverride : inheritedBulletMovement);
                return;
            }

            Observable.Timer(TimeSpan.FromSeconds(fireTime))
                .Subscribe(_ =>
                {
                    if (entry.signal != null)
                    {
                        onAttackTiming.OnNext(entry.CreateEvent(inheritedBulletMovement));
                    }
                })
                .AddTo(phaseDisposables);
        }

        void ExpandPreset(PresetAttackSignal signal, SeType parentSeType, float baseTime, int depth, MovementPreset inheritedBulletMovement)
        {
            var timeline = signal.Preset.CreateTimeline();
            if (timeline == null) return;

            // ネストしたプリセットは常に先頭Phase(通常状態)のみを展開する。
            // プリセット部品自体にHP閾値のフェーズ切替を持たせる使い方は想定していない。
            var entries = timeline.phases.Count > 0 ? timeline.phases[0].entries : null;
            if (entries == null || entries.Count == 0) return;

            foreach (var inner in entries)
            {
                InitializeEntryProviders(inner);
                // 内側がNoneなら外側のseTypeを引き継ぐ
                if (inner.seType == SeType.None && parentSeType != SeType.None)
                {
                    inner.seType = parentSeType;
                }
            }

            var totalCycles = signal.Loop && signal.LoopCount > 0 ? signal.LoopCount : 1;
            for (var cycle = 0; cycle < totalCycles; cycle++)
            {
                foreach (var inner in entries)
                {
                    var innerTime = baseTime + cycle * signal.CycleDuration + inner.time;
                    ScheduleEntry(inner, innerTime, depth, inheritedBulletMovement);
                }
            }
        }

        public AttackTimeline DeepCopy()
        {
            var copy = new AttackTimeline
            {
                phases = new List<AttackPhase>(phases.Count)
            };
            foreach (var phase in phases)
            {
                copy.phases.Add(phase.DeepCopy());
            }
            return copy;
        }

        public void Update(float deltaTime) { }

        public void Dispose()
        {
            phaseDisposables?.Dispose();
            disposables?.Dispose();
            onAttackTiming?.Dispose();
        }
    }
}
