using UnityEngine;
using NakeDev.Core;

public class GameManager : MonoBehaviour
{
    // [SerializeField] private GameStateSO _gameState;

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


    public void Update()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Locked;
    }


}
