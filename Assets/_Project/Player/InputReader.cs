using System;
using UnityEngine;
using UnityEngine.InputSystem;
using NakeDev.Core;
using NakeDev.Player.GeneratedInput;

namespace NakeDev.Player
{
    /// <summary>
    /// Lê o New Input System e expõe input como properties/eventos.
    /// Componente por entidade (sem singleton/InputManager global — Regra 2/CONVENTIONS.md).
    /// </summary>
    public class InputReader : MonoBehaviour, PlayerControls.IPlayerActions, IMovementInput
    {
        [Tooltip("Opcional: se referenciado, o InputReader só lê input de gameplay quando o estado for 'Playing'.")]
        [SerializeField] private GameStateSO _gameState;

        private PlayerControls _controls;

        public Vector2 MoveInput { get; private set; }

        public event Action OnJumpPressed;

        private void OnEnable()
        {
            if (_controls == null)
            {
                _controls = new PlayerControls();
                _controls.Player.SetCallbacks(this);
            }
            _controls.Enable();
        }

        private void OnDisable()
        {
            _controls?.Disable();
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            if (_gameState != null && !_gameState.IsPlaying())
            {
                MoveInput = Vector2.zero;
                return;
            }

            MoveInput = context.ReadValue<Vector2>();
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (_gameState != null && !_gameState.IsPlaying()) return;

            if (context.performed)
                OnJumpPressed?.Invoke();
        }
    }
}
