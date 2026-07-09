using UniRx;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Project.Scenes.Battle.Scripts.Model
{
    public class BattleBackgroundModel
    {
        readonly ReactiveProperty<Texture> currentBackground = new();

        public IReadOnlyReactiveProperty<Texture> CurrentBackground => currentBackground;

        public void SetStage(string backgroundAddress)
        {
            if (string.IsNullOrEmpty(backgroundAddress))
            {
                Debug.LogWarning("[BattleBackgroundModel] backgroundAddress is empty.");
                return;
            }

            currentBackground.Value = Addressables.LoadAssetAsync<Texture2D>(backgroundAddress).WaitForCompletion();
        }

        public void Dispose()
        {
            currentBackground?.Dispose();
        }
    }
}
