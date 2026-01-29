using Project.Scenes.Global.Scripts.Presenter.Message;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Scenes.Global.Scripts.Presenter
{
    public class InputManager : MonoBehaviour
    {
        // カスタムInputActionAsset
        InputSystem_Actions inputSystem_Actions;

        public void Setup()
        {
            inputSystem_Actions = new InputSystem_Actions();

            // Move
            inputSystem_Actions.Player.Move.started += OnPlayerMove;
            inputSystem_Actions.Player.Move.performed += OnPlayerMove;
            inputSystem_Actions.Player.Move.canceled += OnPlayerMove;

            // Look
            inputSystem_Actions.Player.Look.started += OnPlayerLook;
            inputSystem_Actions.Player.Look.performed += OnPlayerLook;
            inputSystem_Actions.Player.Look.canceled += OnPlayerLook;

            // Attack
            inputSystem_Actions.Player.Attack.started += OnPlayerAttack;

            // Jump
            inputSystem_Actions.Player.Jump.started += OnPlayerJump;

            // Interact
            inputSystem_Actions.Player.Interact.started += OnPlayerInteract;
            inputSystem_Actions.Player.Interact.performed += OnPlayerInteract;
            inputSystem_Actions.Player.Interact.canceled += OnPlayerInteract;

            // Crouch
            inputSystem_Actions.Player.Crouch.started += OnPlayerCrouch;
            inputSystem_Actions.Player.Crouch.canceled += OnPlayerCrouch;

            // Sprint
            inputSystem_Actions.Player.Sprint.started += OnPlayerSprint;
            inputSystem_Actions.Player.Sprint.canceled += OnPlayerSprint;

            // Previous
            inputSystem_Actions.Player.Previous.started += OnPlayerPrevious;

            // Next
            inputSystem_Actions.Player.Next.started += OnPlayerNext;

            // UI - Navigate
            inputSystem_Actions.UI.Navigate.started += OnUINavigate;
            inputSystem_Actions.UI.Navigate.performed += OnUINavigate;
            inputSystem_Actions.UI.Navigate.canceled += OnUINavigate;

            // UI - Submit
            inputSystem_Actions.UI.Submit.started += OnUISubmit;

            // UI - Cancel
            inputSystem_Actions.UI.Cancel.started += OnUICancel;

            // UI - Point
            inputSystem_Actions.UI.Point.started += OnUIPoint;
            inputSystem_Actions.UI.Point.performed += OnUIPoint;

            // UI - Click
            inputSystem_Actions.UI.Click.started += OnUIClick;

            // UI - Right Click
            inputSystem_Actions.UI.RightClick.started += OnUIRightClick;

            // UI - Middle Click
            inputSystem_Actions.UI.MiddleClick.started += OnUIMiddleClick;

            // UI - Scroll Wheel
            inputSystem_Actions.UI.ScrollWheel.started += OnUIScrollWheel;
            inputSystem_Actions.UI.ScrollWheel.performed += OnUIScrollWheel;
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
