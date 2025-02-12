using System;
using Photon.Pun;
using UnityEngine;

public class PlayerSettings
{
    public static readonly float PLAYER_SPEED_DEFAULT = 3.0f;
    public static readonly float PLAYER_SPEED_SLOW = 1.0f;
    public static readonly float PLAYER_HITBOX_DEFAULT = 1.3f;
    public static readonly float PLAYER_HITBOX_SMALL = 1.0f;
    public static readonly float PLAYER_GRAVITY_SLOW = 0.5f;
    public static readonly float PLAYER_GRAVITY_DEFAULT = 2.0f;
}
public class CharacterModel : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("스텟")]
    [SerializeField] int maxHealth = 100;
    public int MaxHelath { get => maxHealth; }
    [SerializeField] float jumpForce = 5f;
    public float JumpForce { get => jumpForce; }
    [SerializeField] float rollSpeed = 8f;
    public float RollSpeed { get => rollSpeed; }
    [SerializeField] int fullCombo = 3;
    [SerializeField] float comboThreshold = 1.0f;
    [SerializeField] float attackDelay = 0.25f;
    public float AttackDelay { get => attackDelay; }
    [SerializeField] int[] comboDamages;
    public int[] ComboDamages { get => comboDamages; }
    [SerializeField] float attackDistance = 3f;
    public float AttackDistance { get => attackDistance; }
    // Event
    public event Action<int> HealthChanged;
    public event Action<bool> RollingEvent;
    public event Action<bool> GrabbingChangeEvent;
    public event Action<int> AttackEvent;
    public event Action<bool> ParryEvent;
    public event Action<bool> BlockEvent;
    private int health;
    public int Health
    {
        get => health;
        set
        {
            health = Mathf.Max(0, value);
            HealthChanged?.Invoke(health);
        }
    }

    private float moveSpeed;
    public float MoveSpeed { get => moveSpeed; set => moveSpeed = value; }

    private bool rolling;
    public bool Rolling
    {
        get => rolling; set
        {
            rolling = value;
            RollingEvent?.Invoke(rolling);
        }
    }

    private bool blocking;
    public bool Blocking
    {
        get => blocking;
        set
        {
            blocking = value;
            BlockEvent?.Invoke(blocking);
        }
    }

    private bool parrying;
    public bool Parrying
    {
        get => parrying;
        set
        {
            parrying = value;
            ParryEvent?.Invoke(parrying);
        }
    }

    private bool grabbing = false;
    public bool Grabbing
    {
        get => grabbing;
        set
        {
            if (value != grabbing)
                GrabbingChangeEvent?.Invoke(value);
            grabbing = value;
        }
    }

    private float attackTimer = 0f;
    public float AttackTimer { get => attackTimer; set => attackTimer = value; }

    private int attackCount = 0;    // 현재 공격 단계
    public int AttackCount
    {
        get => attackCount;
        set
        {
            if (attackCount > fullCombo || attackTimer > comboThreshold)
                attackCount = 1;
            else
                attackCount = value;
            AttackEvent?.Invoke(attackCount);
        }
    }

    private Vector2 currnetDirection = Vector2.right;   // 현재 캐릭터 방향
    public Vector2 CurrnetDirection { get => currnetDirection; set => currnetDirection = value; }

    private void Start()
    {
        Health = maxHealth;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            //stream.SendNext(MoveSpeed);
            stream.SendNext(Grabbing);
        }
        else
        {
            //MoveSpeed = (float)stream.ReceiveNext();
            Grabbing = (bool)stream.ReceiveNext();
        }
    }
}
