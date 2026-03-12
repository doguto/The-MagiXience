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
        [SerializeField, Min(0.01f)] float loopDuration = 3f;
        [SerializeField] List<AttackTimelineEntry> entries = new();

        readonly Subject<AttackEvent> onAttackTiming = new();
        bool[] fired;
        float elapsed;
        
        public IObservable<AttackEvent> OnAttackTiming => onAttackTiming;
        public bool IsCompleted => !loop && fired != null && Array.TrueForAll(fired, f => f);

        public void InitializeProviders(Func<Vector3> getPlayerPosition, Func<Vector3> getEnemyPosition)
        {
            foreach (var entry in entries)
            {
                entry.directionProvider?.Initialize(getPlayerPosition, getEnemyPosition);
            }
        }

        public void Initialize()
        {
            elapsed = 0f;
            fired = new bool[entries.Count];
        }

        public void Update(float deltaTime)
        {
            if (IsCompleted) return;

            elapsed += deltaTime;

            for (var i = 0; i < entries.Count; i++)
            {
                if (!fired[i] && elapsed >= entries[i].time)
                {
                    fired[i] = true;
                    var entry = entries[i];
                    if (entry.signal != null)
                    {
                        onAttackTiming.OnNext(entry.signal.CreateEvent(entry.directionProvider));
                    }
                }
            }

            if (loop && elapsed >= loopDuration)
            {
                elapsed -= loopDuration;
                for (var i = 0; i < fired.Length; i++) fired[i] = false;
            }
        }

        public void Dispose()
        {
            onAttackTiming?.Dispose();
        }
    }
}
