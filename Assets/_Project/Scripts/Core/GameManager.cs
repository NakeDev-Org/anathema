using UnityEngine;
using NakeDev.Core;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameStateSO _gameState;

    // public void Paused()
    // {
    //     Cursor.visible = true;
    //     Cursor.lockState = CursorLockMode.None;
    // }

    //     public void Playing()
    // {
    //     Cursor.visible = false;
    //     Cursor.lockState = CursorLockMode.Locked;
    // }

    // public void Update()
    // {
    //     Cursor.visible = true;
    //     Cursor.lockState = CursorLockMode.Locked;
    // }

    private void OnEnable()
    {
        if (_gameState == null)
            return;

        _gameState.OnStateChanged += HandleGameStateChanged;
        HandleGameStateChanged(_gameState.CurrentState);
    }

    private void OnDisable()
    {
        if (_gameState != null)
            _gameState.OnStateChanged -= HandleGameStateChanged;
    }

    private static void HandleGameStateChanged(GameState state)
    {
        bool isPaused = state == GameState.Paused;

        Cursor.visible = isPaused;
        Cursor.lockState = isPaused
            ? CursorLockMode.None
            : CursorLockMode.Locked;
    }
}
