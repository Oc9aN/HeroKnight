using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public interface INode
{
    public enum BTState
    { RUN, SUCCESS, FAILED }

    public BTState Evaluate(); // 판단하여 상태 리턴
}

public class ActionNode : INode
{
    public Func<INode.BTState> action;

    public ActionNode(Func<INode.BTState> action) // 실행할 동작
    {
        this.action = action;
    }

    public INode.BTState Evaluate()
    {
        // 대리자가 null 이 아닐 때 호출, null 인 경우 Failed 반환
        return action?.Invoke() ?? INode.BTState.FAILED;
    }
}

public class SelectorNode : INode
{
    List<INode> children; // 여러 노드를 가질 수 있도록 리스트 생성

    public SelectorNode() { children = new List<INode>(); }

    public void Add(INode node) { children.Add(node); } // 셀렉터에 자식노드를 추가하는 메서드

    public INode.BTState Evaluate()
    {
        // 리스트 내의 노드들을 왼쪽부터(넣은 순으로) 검사
        foreach (INode child in children)
        {

            INode.BTState state = child.Evaluate();
            // child 노드의 state 가 하나라도 SUCCESS 이면 성공을 반환
            // 실행 중인 경우 RUN 반환
            switch (state)
            {
                case INode.BTState.SUCCESS:
                    return INode.BTState.SUCCESS;
                case INode.BTState.RUN:
                    return INode.BTState.RUN;
            }
        }
        // 반복문이 끝났다면 해당 셀렉터의 자식노드들은 전부 FAILED 상태이므로 셀렉터는 FAILED 반환
        return INode.BTState.FAILED;
    }
}

public class SequenceNode : INode
{
    List<INode> children; // 자식 노드들을 담을 수 있는 리스트

    public SequenceNode() { children = new List<INode>(); }

    public void Add(INode node) { children.Add(node); }

    public INode.BTState Evaluate()
    {
        // 자식 노드의 수가 0 이하라면 실패
        if (children.Count <= 0)
            return INode.BTState.FAILED;

        foreach (INode child in children)
        {
            // 자식 노드들중 하나라도 FAILED 라면 시퀀스는 FAILED
            switch (child.Evaluate())
            {
                case INode.BTState.RUN:
                    return INode.BTState.RUN;
                case INode.BTState.SUCCESS:
                    continue;   // 다음 노드 실행
                case INode.BTState.FAILED:
                    return INode.BTState.FAILED;
            }
        }
        // FAILED 에 걸리지 않고 반복문을 빠져나왔으므로 시퀀스는 SUCCESS
        return INode.BTState.SUCCESS;
    }
}

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(BossModel))]
public class BossController : MonoBehaviourPunCallbacks, ITarget
{
    [Header("스킬")]
    [SerializeField] BossSpell skill;
    private SelectorNode rootNode;  // 루트 노드

    // MVC
    private BossView bossView;
    private BossModel bossModel;

    private PhotonView PV;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    public void SetTarget(GameObject target)
    {
        bossModel.TargetObj = target;
    }

    private void Awake()
    {
        bossModel = GetComponent<BossModel>();
        PV = GetComponent<PhotonView>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    public void Init(BossView view)
    {
        // 뷰
        bossView = view;
        bossView.SetMaxHealth(bossModel.MaxHealth);

        // 모델 이벤트 등록
        bossModel.HealthChanged += bossView.OnHealthChanged;
        bossModel.SkillEvent += Skill;
        bossModel.AttackEvent += Attack;
        bossModel.MoveEvent += Move;

        // 각 행동 트리 노드를 초기화
        // 공격 셀렉터
        SelectorNode attackSelector = new SelectorNode();
        attackSelector.Add(new ActionNode(SkillAttackAction));
        attackSelector.Add(new ActionNode(DefaultAttackAction));
        // 공격 시퀀스
        SequenceNode attackSequence = new SequenceNode();
        attackSequence.Add(new ActionNode(AttackCoolTimeAction));
        attackSequence.Add(attackSelector);

        // 루트 노드
        rootNode = new SelectorNode();
        rootNode.Add(attackSequence);
        rootNode.Add(new ActionNode(TraceAction));
        rootNode.Add(new ActionNode(IdleAction));
    }

    // 루트 노드 실행
    private void Update()
    {
        if (!PhotonNetwork.IsMasterClient)  // 마스터가 아니면 return
            return;

        if (bossModel.Target != null)
            rootNode.Evaluate();

        bossModel.AttackCoolTime += Time.deltaTime;
    }

    private INode.BTState SkillAttackAction()
    {
        if (bossModel.Target == null)
            return INode.BTState.FAILED;
        // 확률적으로 스킬 발동
        int percent = UnityEngine.Random.Range(0, 100);
        if (!bossModel.Attacking && percent < bossModel.SkillChance)
        {
            Debug.Log("보스 스킬 발동");
            bossModel.SkillUsing = true;
            return INode.BTState.SUCCESS;
        }
        return INode.BTState.FAILED;
    }
    private INode.BTState DefaultAttackAction()
    {
        if (bossModel.Target == null)
            return INode.BTState.FAILED;
        if (!bossModel.Attacking && Mathf.Abs(bossModel.Target.Distance(transform.position)) < bossModel.AttackRange)
        {
            // 가까우면 바로 공격
            Debug.Log("보스 공격!!");
            bossModel.Attacking = true;
            return INode.BTState.SUCCESS;
        }
        return INode.BTState.FAILED;
    }
    private INode.BTState AttackCoolTimeAction()
    {
        if (bossModel.Attacking)
        {
            Debug.Log("보스 공격중..");
            return INode.BTState.RUN;
        }
        float randomCoolTime = UnityEngine.Random.Range(bossModel.AttackCoolTimeMin, bossModel.AttackCoolTimeMax);
        if (bossModel.AttackCoolTime < randomCoolTime)
            return INode.BTState.FAILED;
        return INode.BTState.SUCCESS;
    }
    private INode.BTState TraceAction()
    {
        if (bossModel.Target == null || Mathf.Abs(bossModel.Target.Distance(transform.position)) < bossModel.TraceDistance || bossModel.Attacking)   // 추적 거리보다 가까워지면 종료
            return INode.BTState.FAILED;
        Debug.Log("보스 이동중..");
        bossModel.Moving = true;
        return INode.BTState.RUN;
    }
    private INode.BTState IdleAction()
    {
        // 대기 액션, 공격 딜레이중 또는 일정 확률로 대기
        Debug.Log("보스 대기중..");
        bossModel.Moving = false;
        return INode.BTState.RUN;
    }

    private void Skill(bool isSkill)
    {
        if (isSkill)
        {
            SkillCreate();
            animator.SetBool("Move", false);
            PV.RPC("RpcSetTrigger", RpcTarget.All, "Cast");
            bossModel.AttackCoolTime = 0f;
        }
    }
    private void SkillCreate()
    {
        int count = UnityEngine.Random.Range(bossModel.SkillCountMin, bossModel.SkillCountMax);
        for (int skillCount = 0; skillCount < count; skillCount++)
        {
            float xPosition = transform.position.x + bossModel.Target.Distance(transform.position);
            xPosition += bossModel.SkillSpace * skillCount * Mathf.Sign(bossModel.Target.Distance(transform.position));
            Vector2 skillPosition = new Vector2(xPosition, transform.position.y);
            BossSpell spell = Instantiate(skill);
            spell.Init(skillPosition, bossModel.SkillDamage);
            spell.AttackEndAction += AE_AttackEnd;
        }
    }
    private void Attack(bool isAttack)
    {
        if (isAttack)
        {
            animator.SetBool("Move", false);
            PV.RPC("RpcSetTrigger", RpcTarget.All, "Attack");
            bossModel.AttackCoolTime = 0f;
        }
    }
    private void Move(bool isMoving)
    {
        animator.SetBool("Move", isMoving);
        Vector2 direction = new Vector2(bossModel.Target.Distance(transform.position), 0f);
        direction.Normalize();
        if (!Utils.VectorsApproximatelyEqual(bossModel.CurrnetDirection, direction))
        {
            // 방향 전환
            PV.RPC("RpcFlipX", RpcTarget.AllBuffered, direction);
        }
        if (isMoving)
            transform.position += (Vector3)direction * bossModel.Speed * Time.deltaTime;
    }

    public void Damaged(int damage)
    {
        Debug.Log($"뽀스 데미지 {damage}만큼 받았다!");
        bossModel.Health -= damage;
        if (!bossModel.Moving && !bossModel.Attacking)
            PV.RPC("RpcSetTrigger", RpcTarget.All, "Hurt");
    }

    public float Distance(Vector3 from)
    {
        Vector3 distance = transform.position - from;
        return distance.x;
    }

    // PunRPC
    [PunRPC]
    private void RpcFlipX(Vector2 direction)
    {
        spriteRenderer.flipX = Vector2.left != direction;
        bossModel.CurrnetDirection = direction;
    }
    [PunRPC]
    private void RpcSetTrigger(string s)
    {
        animator.SetTrigger(s);
    }

    private void AE_AttackEnd() => bossModel.Attacking = false;
    private void AE_AttackDefault()
    {
        if (Utils.FloatSignEqual(bossModel.Target.Distance(transform.position), bossModel.CurrnetDirection.x)
        && Mathf.Abs(bossModel.Target.Distance(transform.position)) < bossModel.AttackRange)   // 데미지 줄때 방향과 사거리 체크
            bossModel.Target.Damaged(bossModel.DefaultDamage);
    }

    public int GetTargetViewId()
    {
        throw new NotImplementedException();
    }
}
