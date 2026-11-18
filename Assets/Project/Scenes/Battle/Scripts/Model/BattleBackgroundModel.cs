using UniRx;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Project.Scenes.Battle.Scripts.Model
{
    public class BattleBackgroundModel
    {
        readonly ReactiveProperty<Sprite> currentBackground = new();

        public IReadOnlyReactiveProperty<Sprite> CurrentBackground => currentBackground;

        public void SetStage(string backgroundAddress)
        {
            if (string.IsNullOrEmpty(backgroundAddress))
            {
                Debug.LogWarning("[BattleBackgroundModel] backgroundAddress is empty.");
                return;
            }

            currentBackground.Value = Addressables.LoadAssetAsync<Sprite>(backgroundAddress).WaitForCompletion();
        }

        public void Dispose()
        {
            currentBackground?.Dispose();
        }
    }
}
