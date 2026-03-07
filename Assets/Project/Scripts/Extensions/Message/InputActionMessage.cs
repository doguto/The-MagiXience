using UnityEngine;

namespace Project.Scripts.Extensions.Message
{
    public struct PlayerMoveMessage
    {
        public Vector2 value;
    }

    public struct PlayerAttackMessage
    {
    }

    public struct PlayerJumpMessage
    {
    }

    public struct PlayerLookMessage
    {
        public Vector2 value;
    }

    public struct PlayerInteractMessage
    {
    }

    public struct PlayerCrouchMessage
    {
        public bool isPressed;
    }

    public struct PlayerSprintMessage
    {
        public bool isPressed;
    }

    public struct PlayerChargeMessage
    {
        public bool isPressed;
    }

    public struct PlayerPreviousMessage
    {
    }

    public struct PlayerNextMessage
    {
    }

    // UI Messages
    public struct UINavigateMessage
    {
        public Vector2 value;
    }

    public struct UISubmitMessage
    {
    }

    public struct UICancelMessage
    {
    }

    public struct UIPointMessage
    {
        public Vector2 value;
    }

    public struct UIClickMessage
    {
    }

    public struct UIRightClickMessage
    {
    }

    public struct UIMiddleClickMessage
    {
    }

    public struct UIScrollWheelMessage
    {
        public Vector2 value;
    }
}
