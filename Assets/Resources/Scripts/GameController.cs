using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Events;

public class GameController : MonoBehaviourPunCallbacks
{
    [Header("캐릭터")]
    [SerializeField] Transform spawnPoint;
    [SerializeField] CharacterView characterView;
    [Header("보스")]
    [SerializeField] Transform bossSpawnPoint;
    [SerializeField] BossView bossView;

    private InputHandler inputHandler;

    // Start is called before the first frame update
    void Start()
    {
        Screen.SetResolution(960, 540, false);
        Application.targetFrameRate = 60;
        PhotonNetwork.SendRate = 60;
        PhotonNetwork.SerializationRate = 30;
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster() => PhotonNetwork.JoinOrCreateRoom("MyRoom", new RoomOptions { MaxPlayers = 2 }, null);

    public override void OnJoinedRoom()
    {
        CreateUnits();
    }

    private void CreateUnits()
    {
        Debug.Log("CreateUnits"); ;

        PhotonNetwork.Instantiate("Prefabs/HeroKnight", spawnPoint.position, Quaternion.identity).TryGetComponent<CharacterController>(out CharacterController characterController);
        characterController.Init(characterView);

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Instantiate("Prefabs/Bringer-of-Death", bossSpawnPoint.position, Quaternion.identity).TryGetComponent<BossController>(out BossController bossController);
            bossController.Init(bossView);
            bossController.SetTarget(characterController.gameObject);
        }
    }
}
