using System.Collections.Generic;
using UnityEngine;
using Project.Scenes.Battle.Scripts.Model.PhaseAction;

namespace Project.Scenes.Battle.Scripts.Model
{
    [CreateAssetMenu(fileName = "PhaseAction", menuName = "Battle/Phase Action")]
    public class PhaseActionAsset : ScriptableObject
    {
        [SerializeReference, SubclassSelector]
        List<IPhaseAction> actions = new();

        public IReadOnlyList<IPhaseAction> Actions => actions;

        public BossAction BossAction
        {
            get
            {
                foreach (var action in actions)
                {
                    if (action is BossAction bossAction) return bossAction;
                }
                return null;
            }
        }

        public int TotalEnemySpawnCount
        {
            get
            {
                int count = 0;
                foreach (var action in actions)
                {
                    if (action is EnemySpawnAction) count++;
                }
                return count;
            }
        }
    }
}
