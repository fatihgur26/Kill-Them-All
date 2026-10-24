using KillThemAll.Core;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace KillThemAll.Input
{
    public class StandaloneInputProvider : MonoBehaviour, IInputProvider
    {
        public Vector2 GetMovementDirection()
        {
            Vector2 input = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
            }
#else
            input.x = UnityEngine.Input.GetAxisRaw("Horizontal");
            input.y = UnityEngine.Input.GetAxisRaw("Vertical");
#endif

            return input.sqrMagnitude > 1f ? input.normalized : input;
        }

        public bool HasInput()
        {
            return GetMovementDirection().sqrMagnitude > 0.001f;
        }
    }
}
