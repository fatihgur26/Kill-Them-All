using KillThemAll.Core;
using UnityEngine;

namespace KillThemAll.Input
{
    public class MobileInputProvider : MonoBehaviour, IInputProvider
    {
        [SerializeField] private DynamicJoystick joystick;

        public void SetJoystick(DynamicJoystick newJoystick)
        {
            joystick = newJoystick;
        }

        public Vector2 GetMovementDirection()
        {
            if (joystick == null) return Vector2.zero;
            return joystick.Direction;
        }

        public bool HasInput()
        {
            return joystick != null && joystick.Direction.sqrMagnitude > 0.001f;
        }
    }
}
