using System;
using UnityEngine;

namespace NakeDev.Core
{
    public enum GameState
    {
        Playing,
        Paused,
        Menu,
        Cutscene
    }

    /// <summary>
    /// Gerenciador global de estado via ScriptableObject (arquitetura desacoplada).
    /// Evita singleton pesado: qualquer script referencia esse asset para saber o estado do jogo.
    /// </summary>
    [CreateAssetMenu(fileName = "GameStateManager", menuName = "NakeDev/Core/GameStateManager")]
    public class GameStateSO : ScriptableObject
    {
        public GameState CurrentState { get; private set; }

        public event Action<GameState> OnStateChanged;

        private void OnEnable()
        {
            // Garante que o estado começa jogando (útil em Editor)
            CurrentState = GameState.Playing;
        }

        public void ChangeState(GameState newState)
        {
            if (CurrentState == newState) return;

            CurrentState = newState;
            OnStateChanged?.Invoke(CurrentState);

            if (CurrentState == GameState.Playing)
            {
                Time.timeScale = 1f;
            }
            else
            {
                Time.timeScale = 0f;
            }
        }

        public bool IsPlaying() => CurrentState == GameState.Playing;
    }
}
