
using System;
using Photon.Pun;
using UnityEngine;

public class BossModel : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("체력")]
    [SerializeField] int maxHealth = 100;
    public int MaxHealth { get => maxHealth; }
    [Header("이동 속도")]
    [SerializeField] float speed = 2f;
    public float Speed { get => speed; }
    [Header("공격 사거리")]
    [SerializeField] float attackRange = 3f;
    public float AttackRange { get => attackRange; }
    [Header("추격 최소 거리")]
    [SerializeField] float traceDistance = 2f;
    public float TraceDistance { get => traceDistance; }
    [Header("공격 데미지")]
    [SerializeField] int defaultDamage = 5;
    public int DefaultDamage { get => defaultDamage; }
    [SerializeField] int skillDamage = 10;
    public int SkillDamage { get => skillDamage; }
    [Header("스킬 발동 확률")]
    [SerializeField] int skillChance = 30;
    public int SkillChance { get => skillChance; }
    [Header("공격 쿨타임")]
    [SerializeField] float attackCoolTimeMin = 3f;
    public float AttackCoolTimeMin { get => attackCoolTimeMin; }
    [SerializeField] float attackCoolTimeMax = 5f;
    public float AttackCoolTimeMax { get => attackCoolTimeMax; }
    [Header("스킬 생성 갯수")]
    [SerializeField] int skillCountMin = 2;
    public int SkillCountMin { get => skillCountMin; }
    [SerializeField] int skillCountMax = 5;
    public int SkillCountMax { get => skillCountMax; }
    [Header("스킬 간격")]
    [SerializeField] float skillSpace = 3f;
    public float SkillSpace { get => skillSpace; }

    public event Action<int> HealthChanged;
    public event Action<bool> SkillEvent;
    public event Action<bool> AttackEvent;
    public event Action<bool> MoveEvent;
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

    private bool skillUsing = false;
    public bool SkillUsing
    {
        get => skillUsing;
        set
        {
            skillUsing = value;
            SkillEvent?.Invoke(SkillUsing);
        }
    }
    private bool attacking = false;
    public bool Attacking
    {
        get => attacking || skillUsing;
        set
        {
            attacking = value;
            AttackEvent?.Invoke(attacking);
        }
    }

    private float attackCoolTime = 0f;
    public float AttackCoolTime { get => attackCoolTime; set => attackCoolTime = value; }

    private Vector2 currnetDirection = Vector2.left;   // 현재 캐릭터 방향
    public Vector2 CurrnetDirection { get => currnetDirection; set => currnetDirection = value; }

    private bool moving = false;
    public bool Moving
    {
        get => moving;
        set
        {
            moving = value;
            MoveEvent?.Invoke(moving);
        }
    }

    private GameObject targetObj = null;
    public GameObject TargetObj
    {
        set
        {
            if (targetObj != value)
                target = value.GetComponent<ITarget>();
            targetObj = value;
        }
    }
    private ITarget target;
    public ITarget Target
    {
        get
        {
            if (target == null)
                target = targetObj?.GetComponent<ITarget>();
            return target;
        }
    }

    public BossModel(int maxHealth)
    {
        Health = maxHealth;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(Attacking);
            stream.SendNext(AttackCoolTime);
            stream.SendNext(Moving);
            stream.SendNext(Target.GetTargetViewId());
        }
        else
        {
            Attacking = (bool)stream.ReceiveNext();
            AttackCoolTime = (float)stream.ReceiveNext();
            Moving = (bool)stream.ReceiveNext();
            int targetViewID = (int)stream.ReceiveNext();
            PhotonView targetView = PhotonView.Find(targetViewID);
            if (targetView != null)
            {
                TargetObj = targetView.gameObject;
            }
        }
    }
}
