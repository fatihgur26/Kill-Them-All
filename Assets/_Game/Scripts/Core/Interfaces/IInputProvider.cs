using UnityEngine;

namespace KillThemAll.Core
{
    public interface IInputProvider
    {
        Vector2 GetMovementDirection();
        bool HasInput();
    }
}
