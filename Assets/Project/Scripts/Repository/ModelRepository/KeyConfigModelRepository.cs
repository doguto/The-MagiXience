using Project.Scripts.Model;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Scripts.Repository.ModelRepository
{
    public class KeyConfigModelRepository
    {
        public static KeyConfigModelRepository Instance { get; } = new();

        readonly UserModelRepository userModelRepository;
        KeyConfigModel keyConfigModel;
        InputActionAsset inputActions;

        public KeyConfigModelRepository()
        {
            userModelRepository = UserModelRepository.Instance;
        }
        
        /// <summary>
        /// InputActionAssetを設定
        /// GlobalScenePresenterから呼び出される想定
        /// </summary>
        public void Initialize(InputActionAsset inputActions)
        {
            this.inputActions = inputActions;
            keyConfigModel = null; // 再初期化のためクリア
        }

        public KeyConfigModel Get()
        {
            if (keyConfigModel != null) return keyConfigModel;
            
            if (inputActions == null)
            {
                // InputActionsが未設定の場合はResourcesからロード
                inputActions = Resources.Load<InputActionAsset>("InputSystem_Actions");
                if (inputActions == null)
                {
                    Debug.LogError("InputSystem_Actions not found in Resources");
                    return null;
                }
            }
            
            var userModel = userModelRepository.Get();
            keyConfigModel = new KeyConfigModel(userModel, inputActions);
            return keyConfigModel;
        }
        
        public void Refresh()
        {
            keyConfigModel = null;
        }
    }
}
