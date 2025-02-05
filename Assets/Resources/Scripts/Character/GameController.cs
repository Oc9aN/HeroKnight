using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameController : MonoBehaviour
{
    public CharacterController characterController;

    private InputHandler inputHandler;
    private void Awake()
    {
        inputHandler = new InputHandler(characterController);
    }
    void Update()
    {
        UnityAction inputAction = inputHandler.HandleInput();
        inputAction?.Invoke();
    }
}
