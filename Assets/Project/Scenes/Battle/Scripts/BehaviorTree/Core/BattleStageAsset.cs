using System.Collections.Generic;
using Project.Scenes.Battle.Scripts.Model;
using Project.Scenes.Battle.Scripts.Runtime;
using Project.Scenes.Battle.Scripts.Runtime.Motion;
using Project.Scripts.Model;
using UnityEngine;

namespace Project.Scenes.Battle.Scripts.BehaviorTree
{
    // 道中またはボス戦 1 本分の定義。StageData のアドレスから読み込む。
    // 道中: Tree をステージ直下で実行し、完了したら次へ進む。
    // ボス戦: Boss Prefab を出し、Boss Tree をボス自身に持たせる。ボス撃破で完了。
    [CreateAssetMenu(fileName = "BattleStage", menuName = "Battle/Battle Stage")]
    public class BattleStageAsset : ScriptableObject
    {
        [SerializeField] BattleSituation situation = BattleSituation.Way;

        [Header("Services")]
        [SerializeReference, SubclassSelector, Tooltip("このステージでだけ使う機能 (風など)。宣言しないものは存在しない扱いになる")]
        List<StageServiceDefinition> services = new();

        [Header("Stage Tree")]
        [SerializeField, Tooltip("ステージ直下で実行するツリー。道中ではこれが完了すると次へ進む")] BehaviorTreeAsset tree;

        [Header("Boss")]
        [SerializeField] GameObject bossPrefab;
        [SerializeField] Vector3 bossSpawnPosition;
        [SerializeReference, SubclassSelector] List<MotionStep> bossEntrance = new();
        [SerializeField, Tooltip("ボス自身に持たせるツリー。シナリオ明けに開始する")] BehaviorTreeAsset bossTree;

        public BattleSituation Situation => situation;
        public IReadOnlyList<StageServiceDefinition> Services => services;
        public BehaviorTreeAsset Tree => tree;
        public GameObject BossPrefab => bossPrefab;
        public Vector3 BossSpawnPosition => bossSpawnPosition;
        public IReadOnlyList<MotionStep> BossEntrance => bossEntrance;
        public BehaviorTreeAsset BossTree => bossTree;
    }
}
