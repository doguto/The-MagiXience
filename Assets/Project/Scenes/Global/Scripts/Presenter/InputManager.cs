using Project.Scripts.Extensions.Message;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Scenes.Global.Scripts.Presenter
{
    public class InputManager : MonoBehaviour
    {
        // カスタムInputActionAsset
        InputActionAsset inputActionAsset;
        
        // 購読解除用のアクションとハンドラーのペア
        private readonly System.Collections.Generic.List<(InputAction action, System.Action<InputAction.CallbackContext> handler, InputActionPhase phase)> actionHandlers = new System.Collections.Generic.List<(InputAction, System.Action<InputAction.CallbackContext>, InputActionPhase)>();

        private enum InputActionPhase
        {
            Started,
            Performed,
            Canceled
        }

        private void RegisterAction(InputAction action, System.Action<InputAction.CallbackContext> handler, InputActionPhase phase)
        {
            if (action == null) return;
            
            switch (phase)
            {
                case InputActionPhase.Started:
                    action.started += handler;
                    break;
                case InputActionPhase.Performed:
                    action.performed += handler;
                    break;
                case InputActionPhase.Canceled:
                    action.canceled += handler;
                    break;
            }
            
            actionHandlers.Add((action, handler, phase));
        }

        public void Setup(InputActionAsset inputActions)
        {
            inputActionAsset = inputActions;

            // Player ActionMap
            var playerMap = inputActionAsset.FindActionMap("Player");

            // Move
            var moveAction = playerMap.FindAction("Move");
            RegisterAction(moveAction, OnPlayerMove, InputActionPhase.Started);
            RegisterAction(moveAction, OnPlayerMove, InputActionPhase.Performed);
            RegisterAction(moveAction, OnPlayerMove, InputActionPhase.Canceled);

            // Look
            var lookAction = playerMap.FindAction("Look");
            RegisterAction(lookAction, OnPlayerLook, InputActionPhase.Started);
            RegisterAction(lookAction, OnPlayerLook, InputActionPhase.Performed);
            RegisterAction(lookAction, OnPlayerLook, InputActionPhase.Canceled);

            // Attack
            var attackAction = playerMap.FindAction("Attack");
            RegisterAction(attackAction, OnPlayerAttack, InputActionPhase.Started);

            // Jump
            var jumpAction = playerMap.FindAction("Jump");
            RegisterAction(jumpAction, OnPlayerJump, InputActionPhase.Started);

            // Interact
            var interactAction = playerMap.FindAction("Interact");
            RegisterAction(interactAction, OnPlayerInteract, InputActionPhase.Started);
            RegisterAction(interactAction, OnPlayerInteract, InputActionPhase.Performed);
            RegisterAction(interactAction, OnPlayerInteract, InputActionPhase.Canceled);

            // Crouch
            var crouchAction = playerMap.FindAction("Crouch");
            RegisterAction(crouchAction, OnPlayerCrouch, InputActionPhase.Started);
            RegisterAction(crouchAction, OnPlayerCrouch, InputActionPhase.Canceled);

            // Sprint
            var sprintAction = playerMap.FindAction("Sprint");
            RegisterAction(sprintAction, OnPlayerSprint, InputActionPhase.Started);
            RegisterAction(sprintAction, OnPlayerSprint, InputActionPhase.Canceled);

            // Previous
            var previousAction = playerMap.FindAction("Previous");
            RegisterAction(previousAction, OnPlayerPrevious, InputActionPhase.Started);

            // Next
            var nextAction = playerMap.FindAction("Next");
            RegisterAction(nextAction, OnPlayerNext, InputActionPhase.Started);

            // UI ActionMap
            var uiMap = inputActionAsset.FindActionMap("UI");

            // UI - Navigate
            var navigateAction = uiMap.FindAction("Navigate");
            RegisterAction(navigateAction, OnUINavigate, InputActionPhase.Started);
            RegisterAction(navigateAction, OnUINavigate, InputActionPhase.Performed);
            RegisterAction(navigateAction, OnUINavigate, InputActionPhase.Canceled);

            // UI - Submit
            var submitAction = uiMap.FindAction("Submit");
            RegisterAction(submitAction, OnUISubmit, InputActionPhase.Started);

            // UI - Cancel
            var cancelAction = uiMap.FindAction("Cancel");
            RegisterAction(cancelAction, OnUICancel, InputActionPhase.Started);

            // UI - Point
            var pointAction = uiMap.FindAction("Point");
            RegisterAction(pointAction, OnUIPoint, InputActionPhase.Started);
            RegisterAction(pointAction, OnUIPoint, InputActionPhase.Performed);

            // UI - Click
            var clickAction = uiMap.FindAction("Click");
            RegisterAction(clickAction, OnUIClick, InputActionPhase.Started);

            // UI - Right Click
            var rightClickAction = uiMap.FindAction("RightClick");
            RegisterAction(rightClickAction, OnUIRightClick, InputActionPhase.Started);

            // UI - Middle Click
            var middleClickAction = uiMap.FindAction("MiddleClick");
            RegisterAction(middleClickAction, OnUIMiddleClick, InputActionPhase.Started);

            // UI - Scroll Wheel
            var scrollWheelAction = uiMap.FindAction("ScrollWheel");
            RegisterAction(scrollWheelAction, OnUIScrollWheel, InputActionPhase.Started);
            RegisterAction(scrollWheelAction, OnUIScrollWheel, InputActionPhase.Performed);

            // InputSystemを有効化
            inputActionAsset.Enable();
        }

        void OnDestroy()
        {
            if (inputActionAsset == null) return;

            // 登録されたすべてのアクションハンドラーを購読解除
            foreach (var (action, handler, phase) in actionHandlers)
            {
                if (action == null) continue;
                
                switch (phase)
                {
                    case InputActionPhase.Started:
                        action.started -= handler;
                        break;
                    case InputActionPhase.Performed:
                        action.performed -= handler;
                        break;
                    case InputActionPhase.Canceled:
                        action.canceled -= handler;
                        break;
                }
            }
            
            actionHandlers.Clear();
            inputActionAsset.Disable();
        }

        public void OnPlayerMove(InputAction.CallbackContext context)
        {
            var value = context.ReadValue<Vector2>();
            MessageBroker.Default.Publish(new PlayerMoveMessage { value = value });
        }

        public void OnPlayerAttack(InputAction.CallbackContext context)
        {
            MessageBroker.Default.Publish(new PlayerAttackMessage());
        }

        public void OnPlayerLook(InputAction.CallbackContext context)
        {
            var value = context.ReadValue<Vector2>();
            MessageBroker.Default.Publish(new PlayerLookMessage { value = value });
        }

        public void OnPlayerJump(InputAction.CallbackContext context)
        {
            MessageBroker.Default.Publish(new PlayerJumpMessage());
        }

        public void OnPlayerInteract(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                MessageBroker.Default.Publish(new PlayerInteractMessage());
            }
        }

        public void OnPlayerCrouch(InputAction.CallbackContext context)
        {
            MessageBroker.Default.Publish(new PlayerCrouchMessage { isPressed = context.started });
        }

        public void OnPlayerSprint(InputAction.CallbackContext context)
        {
            MessageBroker.Default.Publish(new PlayerSprintMessage { isPressed = context.started });
        }

        public void OnPlayerPrevious(InputAction.CallbackContext context)
        {
            MessageBroker.Default.Publish(new PlayerPreviousMessage());
        }

        public void OnPlayerNext(InputAction.CallbackContext context)
        {
            MessageBroker.Default.Publish(new PlayerNextMessage());
        }

        public void OnUINavigate(InputAction.CallbackContext context)
        {
            var value = context.ReadValue<Vector2>();
            MessageBroker.Default.Publish(new UINavigateMessage { value = value });
        }

        public void OnUISubmit(InputAction.CallbackContext context)
        {
            MessageBroker.Default.Publish(new UISubmitMessage());
        }

        public void OnUICancel(InputAction.CallbackContext context)
        {
            MessageBroker.Default.Publish(new UICancelMessage());
        }

        public void OnUIPoint(InputAction.CallbackContext context)
        {
            var value = context.ReadValue<Vector2>();
            MessageBroker.Default.Publish(new UIPointMessage { value = value });
        }

        public void OnUIClick(InputAction.CallbackContext context)
        {
            MessageBroker.Default.Publish(new UIClickMessage());
        }

        public void OnUIRightClick(InputAction.CallbackContext context)
        {
            MessageBroker.Default.Publish(new UIRightClickMessage());
        }

        public void OnUIMiddleClick(InputAction.CallbackContext context)
        {
            MessageBroker.Default.Publish(new UIMiddleClickMessage());
        }

        public void OnUIScrollWheel(InputAction.CallbackContext context)
        {
            var value = context.ReadValue<Vector2>();
            MessageBroker.Default.Publish(new UIScrollWheelMessage { value = value });
        }
    }
}
