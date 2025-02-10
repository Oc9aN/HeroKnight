using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameController : MonoBehaviour
{
    [Header("캐릭터")]
    [SerializeField] Transform spawnPoint;
    [SerializeField] CharacterController characterControllerObj;
    [SerializeField] CharacterView characterView;
    [Header("보스")]
    [SerializeField] Transform bossSpawnPoint;
    [SerializeField] BossController bossControllerObj;
    [SerializeField] BossView bossView;

    private InputHandler inputHandler;
    private void Awake()
    {
        CharacterController characterController = Instantiate(characterControllerObj, spawnPoint.position, Quaternion.identity);
        characterController.Init(characterView);
        inputHandler = new InputHandler(characterController);

        BossController bossController = Instantiate(bossControllerObj, bossSpawnPoint.position, Quaternion.identity);
        bossController.Init(bossView);
        bossController.SetTarget(characterController);
    }
    private void Update()
    {
        UnityAction inputAction = inputHandler.HandleInput();
        inputAction?.Invoke();
    }
}
