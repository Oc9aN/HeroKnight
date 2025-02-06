using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameController : MonoBehaviour
{
    [Header("캐릭터")]
    public Transform spawnPoint;
    public CharacterController characterControllerObj;
    public CharacterView characterView;
    [Header("보스")]
    public Transform bossSpawnPoint;
    public BossController bossControllerObj;

    private InputHandler inputHandler;
    private void Awake()
    {
        CharacterController characterController = Instantiate(characterControllerObj, spawnPoint.position, Quaternion.identity);
        characterController.Init(characterView);
        inputHandler = new InputHandler(characterController);

        BossController bossController = Instantiate(bossControllerObj, bossSpawnPoint.position, Quaternion.identity);
        bossController.SetTarget(characterController);
    }
    private void Update()
    {
        UnityAction inputAction = inputHandler.HandleInput();
        inputAction?.Invoke();
    }
}
