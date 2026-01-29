using Project.Scripts.Extensions.Message;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Scenes.Global.Scripts.Presenter
{
    public class InputManager : MonoBehaviour
    {
        // カスタムInputActionAsset
        InputSystem_Actions inputSystemActions;

        public void Setup()
        {
            inputSystemActions = new InputSystem_Actions();

            // Move
            inputSystemActions.Player.Move.started += OnPlayerMove;
            inputSystemActions.Player.Move.performed += OnPlayerMove;
            inputSystemActions.Player.Move.canceled += OnPlayerMove;

            // Look
            inputSystemActions.Player.Look.started += OnPlayerLook;
            inputSystemActions.Player.Look.performed += OnPlayerLook;
            inputSystemActions.Player.Look.canceled += OnPlayerLook;

            // Attack
            inputSystemActions.Player.Attack.started += OnPlayerAttack;

            // Jump
            inputSystemActions.Player.Jump.started += OnPlayerJump;

            // Interact
            inputSystemActions.Player.Interact.started += OnPlayerInteract;
            inputSystemActions.Player.Interact.performed += OnPlayerInteract;
            inputSystemActions.Player.Interact.canceled += OnPlayerInteract;

            // Crouch
            inputSystemActions.Player.Crouch.started += OnPlayerCrouch;
            inputSystemActions.Player.Crouch.canceled += OnPlayerCrouch;

            // Sprint
            inputSystemActions.Player.Sprint.started += OnPlayerSprint;
            inputSystemActions.Player.Sprint.canceled += OnPlayerSprint;

            // Previous
            inputSystemActions.Player.Previous.started += OnPlayerPrevious;

            // Next
            inputSystemActions.Player.Next.started += OnPlayerNext;

            // UI - Navigate
            inputSystemActions.UI.Navigate.started += OnUINavigate;
            inputSystemActions.UI.Navigate.performed += OnUINavigate;
            inputSystemActions.UI.Navigate.canceled += OnUINavigate;

            // UI - Submit
            inputSystemActions.UI.Submit.started += OnUISubmit;

            // UI - Cancel
            inputSystemActions.UI.Cancel.started += OnUICancel;

            // UI - Point
            inputSystemActions.UI.Point.started += OnUIPoint;
            inputSystemActions.UI.Point.performed += OnUIPoint;

            // UI - Click
            inputSystemActions.UI.Click.started += OnUIClick;

            // UI - Right Click
            inputSystemActions.UI.RightClick.started += OnUIRightClick;

            // UI - Middle Click
            inputSystemActions.UI.MiddleClick.started += OnUIMiddleClick;

            // UI - Scroll Wheel
            inputSystemActions.UI.ScrollWheel.started += OnUIScrollWheel;
            inputSystemActions.UI.ScrollWheel.performed += OnUIScrollWheel;
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
