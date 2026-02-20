#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Project.Scripts.Model;
using Project.Scripts.Repository.ModelRepository;
using UnityEngine;

namespace Project.Commons.Debugger.Scripts.Presenter
{
    public class DebugRuntimeModelInitializer : MonoBehaviour
    {
        [SerializeField, Min(1)] int stageNumber = 1;
        [SerializeField] BattleSituation situation = BattleSituation.Way;

        void Awake()
        {
            var runtimeModel = RuntimeModelRepository.Instance.Get();
            runtimeModel.SetForDebug(stageNumber, situation);
            Debug.Log($"[DebugRuntimeModelInitializer] Stage={stageNumber}, Situation={situation} に設定しました");
        }
    }
}
#endif
