using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PhotonView))]
public class CharacterInput : MonoBehaviourPunCallbacks
{
    private PhotonView PV;

    private InputHandler inputHandler;

    private void Awake()
    {
        PV = GetComponent<PhotonView>();
        inputHandler = new InputHandler(GetComponent<CharacterController>());
    }

    private void Update()
    {
        if (PV.IsMine && inputHandler != null)
        {
            UnityAction inputAction = inputHandler.HandleInput();
            inputAction?.Invoke();
        }
    }
}
