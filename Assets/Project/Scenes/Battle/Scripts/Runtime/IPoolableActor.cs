using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime
{
    // プールから出し入れされるときに状態を戻すコンポーネントが実装する
    public interface IPoolableActor
    {
        // 2 回目以降に出されたとき (初回は Awake で初期化されるので呼ばない)
        void OnTakenFromPool();
        void OnReturnedToPool();
    }
}
