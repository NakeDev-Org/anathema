using System;
using UnityEngine;

namespace NakeDev.Player
{
    /// <summary>
    /// Contrato de input de movimento. Sistemas de gameplay dependem desta interface,
    /// nunca do InputReader concreto (Regra 2 de CONVENTIONS.md).
    /// </summary>
    public interface IMovementInput
    {
        Vector2 MoveInput { get; }
        bool IsRunHeld { get; }

        event Action OnJumpPressed;
        //event Action OnJumpReleased;
        event Action OnDashPressed;
        event Action OnRunPressed;
    }
}
