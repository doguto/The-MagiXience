using System;
using System.Collections.Generic;
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

        public IObservable<AttackEvent> OnAttackTiming => onAttackTiming;
        public bool IsCompleted { get; private set; }

        public void InitializeProviders(Func<Vector3> getPlayerPosition, Func<Vector3> getEnemyPosition)
        {
            foreach (var entry in entries)
            {
                entry.directionProvider?.Initialize(getPlayerPosition, getEnemyPosition);
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

        void ScheduleEntry(AttackTimelineEntry entry, float fireTime)
        {
            Observable.Timer(TimeSpan.FromSeconds(fireTime))
                .Subscribe(_ =>
                {
                    if (entry.signal != null)
                    {
                        onAttackTiming.OnNext(entry.signal.CreateEvent(entry.directionProvider));
                    }
                })
                .AddTo(disposables);
        }

        public void Update(float deltaTime) { }

        public void Dispose()
        {
            disposables?.Dispose();
            onAttackTiming?.Dispose();
        }
    }
}
