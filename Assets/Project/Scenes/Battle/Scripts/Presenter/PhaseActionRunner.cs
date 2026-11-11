using System;
using System.Collections.Generic;
using System.Linq;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Model.PhaseAction;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Project.Scenes.Battle.Scripts.Presenter
{
    /// <summary>
    /// フェーズの時間指定アクションを経過時間に応じて発火する。
    /// BossAction は時間指定ではないため対象外（BattleScenePresenter がフェーズ開始時に適用する）。
    /// </summary>
    public class PhaseActionRunner
    {
        readonly Action<EnemySpawnAction> spawnEnemy;
        readonly Action clearBullets;
        readonly List<ITimedPhaseAction> pending = new();
        readonly List<(GameObject instance, float destroyAt)> spawnedObjects = new();

        float elapsed;
        int nextIndex;
        // 発火中に敵の出現などからフェーズが切り替わり Start/Stop が呼ばれた場合に、古い列の処理を打ち切るための世代番号
        int generation;

        public PhaseActionRunner(Action<EnemySpawnAction> spawnEnemy, Action clearBullets)
        {
            this.spawnEnemy = spawnEnemy;
            this.clearBullets = clearBullets;
        }

        // 初回の発火は次の Tick で行う。フェーズの終了条件が出現の購読を済ませてから発火させるため
        public void Start(PhaseActionAsset asset)
        {
            Stop();
            if (asset == null) return;

            pending.AddRange(asset.Actions.OfType<ITimedPhaseAction>().OrderBy(action => action.Time));
        }

        public void Tick(float deltaTime)
        {
            elapsed += deltaTime;

            var currentGeneration = generation;
            while (nextIndex < pending.Count && pending[nextIndex].Time <= elapsed)
            {
                var action = pending[nextIndex];
                nextIndex++;
                Fire(action);
                if (generation != currentGeneration) return;
            }

            for (var i = spawnedObjects.Count - 1; i >= 0; i--)
            {
                if (spawnedObjects[i].destroyAt > elapsed) continue;
                DestroyIfAlive(spawnedObjects[i].instance);
                spawnedObjects.RemoveAt(i);
            }
        }

        public void Stop()
        {
            generation++;
            pending.Clear();
            nextIndex = 0;
            elapsed = 0f;

            foreach (var (instance, _) in spawnedObjects)
            {
                DestroyIfAlive(instance);
            }
            spawnedObjects.Clear();
        }

        void Fire(ITimedPhaseAction action)
        {
            switch (action)
            {
                case EnemySpawnAction enemySpawn:
                    spawnEnemy?.Invoke(enemySpawn);
                    break;
                case BulletClearAction:
                    clearBullets?.Invoke();
                    break;
                case SpawnObjectAction spawnObject when spawnObject.Prefab != null:
                    spawnedObjects.Add((Object.Instantiate(spawnObject.Prefab), spawnObject.Time + spawnObject.Duration));
                    break;
            }
        }

        static void DestroyIfAlive(GameObject instance)
        {
            if (instance) Object.Destroy(instance);
        }
    }
}
