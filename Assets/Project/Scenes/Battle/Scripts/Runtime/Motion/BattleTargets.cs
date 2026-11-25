using UnityEngine;

namespace Project.Scenes.Battle.Scripts.Runtime.Motion
{
    public static class BattleTargets
    {
        // 自機の Transform。PlayerEntityPresenter が登録する
        public static Transform Player { get; set; }

        public static bool TryGetPlayerPosition(out Vector3 position)
        {
            var player = Player;
            position = player != null ? player.position : default;
            return player != null;
        }
    }
}
