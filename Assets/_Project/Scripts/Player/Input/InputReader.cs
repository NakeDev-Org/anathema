using System;
using NakeDev.Attributes;
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
        [InspectorLine("References")]
        [Tooltip("Opcional: se referenciado, o InputReader só lê input de gameplay quando o estado for 'Playing'.")]
        [SerializeField] private GameStateSO _gameState;

        private PlayerControls _controls;

        public Vector2 MoveInput { get; private set; }
        public bool IsRunHeld { get; private set; }

        public event Action OnJumpPressed;
        public event Action OnJumpReleased;
        public event Action OnDashPressed;
        public event Action OnRunPressed;

        private void OnEnable()
        {
            if (_controls == null)
            {
                _controls = new PlayerControls();
                _controls.Player.SetCallbacks(this);
            }

            if (_gameState != null)
                _gameState.OnStateChanged += HandleGameStateChanged;

            _controls.Enable();
        }

        private void OnDisable()
        {
            if (_gameState != null)
                _gameState.OnStateChanged -= HandleGameStateChanged;

            _controls?.Disable();
            MoveInput = Vector2.zero;
            IsRunHeld = false;
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            if (_gameState != null && !_gameState.IsPlaying())
            {
                MoveInput = Vector2.zero;
                return;
            }

            Vector2 input = context.ReadValue<Vector2>();

            MoveInput = Mathf.Abs(input.x) < 0.2f
                ? Vector2.zero
                : new Vector2(Mathf.Sign(input.x), 0f);
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (_gameState != null && !_gameState.IsPlaying()) return;

            if (context.performed)
                OnJumpPressed?.Invoke();
            else if (context.canceled)
                OnJumpReleased?.Invoke();
        }

        public void OnDash(InputAction.CallbackContext context)
        {
            if (_gameState != null && !_gameState.IsPlaying())
                return;

            if (context.started)
                OnDashPressed?.Invoke();
        }

        public void OnRun(InputAction.CallbackContext context)
        {
            if (_gameState != null && !_gameState.IsPlaying())
            {
                IsRunHeld = false;
                return;
            }

            IsRunHeld = context.ReadValueAsButton();

            if (context.started)
                OnRunPressed?.Invoke();
        }

        public void OnPause(InputAction.CallbackContext context)
        {
            if (!context.performed || _gameState == null)
                return;

            switch (_gameState.CurrentState)
            {
                case GameState.Playing:
                    _gameState.ChangeState(GameState.Paused);
                    break;

                case GameState.Paused:
                    _gameState.ChangeState(GameState.Playing);
                    break;
            }
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.Playing)
                return;

            MoveInput = Vector2.zero;
            IsRunHeld = false;
        }
    }
}
