using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.Runtime;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 1 ステージ分の定義。StageData のアドレスから読み込む。
    // Tree の一番上は Stage ノードにし、道中・会話・ボス戦をその下の Section として並べる。
    [CreateAssetMenu(fileName = "BattleStage", menuName = "Battle/Battle Stage")]
    public class BattleStageAsset : ScriptableObject
    {
        [SerializeReference, SubclassSelector, Tooltip("このステージでだけ使う機能 (風など)。宣言しないものは存在しない扱いになる")]
        List<StageServiceDefinition> services = new();

        [SerializeField, Tooltip("ステージ全体の進行。完了するとステージクリア")] BehaviorTreeAsset tree;

        public IReadOnlyList<StageServiceDefinition> Services => services;
        public BehaviorTreeAsset Tree => tree;
    }
}
