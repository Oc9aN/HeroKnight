using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InputHandler
{
    private CharacterController characterController;
    public InputHandler(CharacterController characterController)
    {
        this.characterController = characterController;
    }
    public UnityAction HandleInput()
    {
        UnityAction action = null;
        // 이동
        if (Input.GetKey(KeyCode.A))
            action += () => characterController.Move(Vector3.left);
        if (Input.GetKey(KeyCode.D))
            action += () => characterController.Move(Vector3.right);
        // 점프
        if (Input.GetKeyDown(KeyCode.Space))
            action += characterController.Jump;
        // 공격
        else if (Input.GetMouseButtonDown(0))
            action += characterController.Attack;
        // 구르기
        else if (Input.GetKeyDown(KeyCode.LeftShift))
            action += characterController.Roll;
        // 막기
        if (Input.GetMouseButtonDown(1))
            action += characterController.BlockOn;
        if (Input.GetMouseButtonUp(1))
            action += characterController.BlockOff;
        // 내려가기
        if (Input.GetKeyDown(KeyCode.S))
            action += characterController.GrabOff;
        // 입력이 없는 경우
        if (action == null)
            action += characterController.Idle;
        return action;
    }
}
