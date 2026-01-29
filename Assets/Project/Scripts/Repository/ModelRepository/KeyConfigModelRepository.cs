using Project.Scripts.Model;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Scripts.Repository.ModelRepository
{
    public class KeyConfigModelRepository
    {
        readonly UserModelRepository userModelRepository;
        InputActionAsset inputActions;
        KeyConfigModel keyConfigModel;

        public KeyConfigModelRepository()
        {
            userModelRepository = UserModelRepository.Instance;
        }

        public static KeyConfigModelRepository Instance { get; } = new();

        public void Initialize(InputActionAsset inputActions)
        {
            this.inputActions = inputActions;
            keyConfigModel = null;
        }

        public KeyConfigModel Get()
        {
            if (keyConfigModel != null) return keyConfigModel;

            if (inputActions == null)
            {
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
