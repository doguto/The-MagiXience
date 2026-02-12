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

        public void Setup(InputActionAsset inputActions)
        {
            inputActionAsset = inputActions;

            // Player ActionMap
            var playerMap = inputActionAsset.FindActionMap("Player");

            // Move
            var moveAction = playerMap.FindAction("Move");
            moveAction.started += OnPlayerMove;
            moveAction.performed += OnPlayerMove;
            moveAction.canceled += OnPlayerMove;

            // Look
            var lookAction = playerMap.FindAction("Look");
            lookAction.started += OnPlayerLook;
            lookAction.performed += OnPlayerLook;
            lookAction.canceled += OnPlayerLook;

            // Attack
            var attackAction = playerMap.FindAction("Attack");
            attackAction.started += OnPlayerAttack;

            // Jump
            var jumpAction = playerMap.FindAction("Jump");
            jumpAction.started += OnPlayerJump;

            // Interact
            var interactAction = playerMap.FindAction("Interact");
            interactAction.started += OnPlayerInteract;
            interactAction.performed += OnPlayerInteract;
            interactAction.canceled += OnPlayerInteract;

            // Crouch
            var crouchAction = playerMap.FindAction("Crouch");
            crouchAction.started += OnPlayerCrouch;
            crouchAction.canceled += OnPlayerCrouch;

            // Sprint
            var sprintAction = playerMap.FindAction("Sprint");
            sprintAction.started += OnPlayerSprint;
            sprintAction.canceled += OnPlayerSprint;

            // Previous
            var previousAction = playerMap.FindAction("Previous");
            previousAction.started += OnPlayerPrevious;

            // Next
            var nextAction = playerMap.FindAction("Next");
            nextAction.started += OnPlayerNext;

            // UI ActionMap
            var uiMap = inputActionAsset.FindActionMap("UI");

            // UI - Navigate
            var navigateAction = uiMap.FindAction("Navigate");
            navigateAction.started += OnUINavigate;
            navigateAction.performed += OnUINavigate;
            navigateAction.canceled += OnUINavigate;

            // UI - Submit
            var submitAction = uiMap.FindAction("Submit");
            submitAction.started += OnUISubmit;

            // UI - Cancel
            var cancelAction = uiMap.FindAction("Cancel");
            cancelAction.started += OnUICancel;

            // UI - Point
            var pointAction = uiMap.FindAction("Point");
            pointAction.started += OnUIPoint;
            pointAction.performed += OnUIPoint;

            // UI - Click
            var clickAction = uiMap.FindAction("Click");
            clickAction.started += OnUIClick;

            // UI - Right Click
            var rightClickAction = uiMap.FindAction("RightClick");
            rightClickAction.started += OnUIRightClick;

            // UI - Middle Click
            var middleClickAction = uiMap.FindAction("MiddleClick");
            middleClickAction.started += OnUIMiddleClick;

            // UI - Scroll Wheel
            var scrollWheelAction = uiMap.FindAction("ScrollWheel");
            scrollWheelAction.started += OnUIScrollWheel;
            scrollWheelAction.performed += OnUIScrollWheel;

            // InputSystemを有効化
            inputActionAsset.Enable();
        }

        void OnDestroy()
        {
            inputActionAsset?.Disable();
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
