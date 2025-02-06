using System;
using System.Collections;
using System.Collections.Generic;
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
public class BossController : MonoBehaviour
{
    [Header("이동 속도")]
    [SerializeField] float speed = 2f;
    [Header("공격 사거리")]
    [SerializeField] float attackRange = 3f;
    [Header("추격 최소 거리")]
    [SerializeField] float traceDistance = 2f;
    [Header("공격 데미지")]
    [SerializeField] int defaultDamage = 5;
    [SerializeField] int skillDamage = 10;
    [Header("스킬 발동 확률")]
    [SerializeField] int skillChance = 30;
    [Header("공격 쿨타임")]
    [SerializeField] float attackCoolTimeMin = 3f;
    [SerializeField] float attackCoolTimeMax = 5f;
    [Header("스킬")]
    [SerializeField] BossSpell skill;
    [Header("스킬 생성 갯수")]
    [SerializeField] int skillCountMin = 2;
    [SerializeField] int skillCountMax = 5;
    [Header("스킬 간격")]
    [SerializeField] float skillSpace = 3f;
    private SelectorNode rootNode;  // 루트 노드

    private Animator animator;
    private ITarget target;         // 타겟
    private bool attacking = false; // 공격 애니메이션 중
    private Vector2 currnetDirection = Vector2.left;   // 현재 캐릭터 방향
    private float attackCoolTime = 0f;

    public void SetTarget(ITarget target)
    {
        this.target = target;
    }

    private void Start()
    {
        animator = GetComponent<Animator>();

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
        if (target != null)
            rootNode.Evaluate();

        attackCoolTime += Time.deltaTime;
    }

    private INode.BTState SkillAttackAction()
    {
        if (target == null)
            return INode.BTState.FAILED;
        // 확률적으로 스킬 발동
        int percent = UnityEngine.Random.Range(0, 100);
        if (!attacking && percent < skillChance)
        {
            Debug.Log("보스 스킬 발동");
            animator.SetBool("Move", false);
            animator.SetTrigger("Cast");
            SkillOn();
            attacking = true;
            attackCoolTime = 0f;
            return INode.BTState.SUCCESS;
        }
        return INode.BTState.FAILED;
    }
    private INode.BTState DefaultAttackAction()
    {
        if (target == null)
            return INode.BTState.FAILED;
        if (!attacking && Mathf.Abs(target.Distance(transform.position)) < attackRange)
        {
            // 가까우면 바로 공격
            Debug.Log("보스 공격!!");
            animator.SetBool("Move", false);
            animator.SetTrigger("Attack");
            attacking = true;
            attackCoolTime = 0f;
            return INode.BTState.SUCCESS;
        }
        return INode.BTState.FAILED;
    }
    private INode.BTState AttackCoolTimeAction()
    {
        if (attacking)
        {
            Debug.Log("보스 공격중..");
            return INode.BTState.RUN;
        }
        float randomCoolTime = UnityEngine.Random.Range(attackCoolTimeMin, attackCoolTimeMax);
        if (attackCoolTime < randomCoolTime)
            return INode.BTState.FAILED;
        return INode.BTState.SUCCESS;
    }
    private INode.BTState TraceAction()
    {
        if (target == null || Mathf.Abs(target.Distance(transform.position)) < traceDistance || attacking)   // 추적 거리보다 가까워지면 종료
            return INode.BTState.FAILED;
        Debug.Log("보스 이동중..");
        animator.SetBool("Move", true);
        Vector3 direction = new Vector3(target.Distance(transform.position), 0f, 0f);
        direction.Normalize();
        transform.position += direction * speed * Time.deltaTime;
        if (!Utils.VectorsApproximatelyEqual(currnetDirection, direction))
        {
            // 방향 전환
            ChangeDirection(direction);
        }
        return INode.BTState.RUN;
    }
    private INode.BTState IdleAction()
    {
        // 대기 액션, 공격 딜레이중 또는 일정 확률로 대기
        Debug.Log("보스 대기중..");
        animator.SetBool("Move", false);
        Vector3 direction = new Vector3(target.Distance(transform.position), 0f, 0f);
        direction.Normalize();
        if (!Utils.VectorsApproximatelyEqual(currnetDirection, direction))
        {
            // 방향 전환
            ChangeDirection(direction);
        }
        return INode.BTState.RUN;
    }

    private void ChangeDirection(Vector3 direction)
    {
        Vector3 newScale = transform.localScale;
        newScale.x = -newScale.x;
        transform.localScale = newScale;
        currnetDirection = direction;
    }

    private void SkillOn()
    {
        int count = UnityEngine.Random.Range(skillCountMin, skillCountMax);
        for (int skillCount = 0; skillCount < count; skillCount++)
        {
            float xPosition = transform.position.x + target.Distance(transform.position);
            xPosition += skillSpace * skillCount * Mathf.Sign(target.Distance(transform.position));
            Vector2 skillPosition = new Vector2(xPosition, transform.position.y);
            BossSpell spell = Instantiate(skill);
            spell.Init(skillPosition, skillDamage);
            spell.AttackEndAction += AE_AttackEnd;
        }
    }

    private void AE_AttackEnd() => attacking = false;
    private void AE_AttackDefault()
    {
        if (Utils.FloatSignEqual(target.Distance(transform.position), currnetDirection.x)
        && Mathf.Abs(target.Distance(transform.position)) < attackRange)   // 데미지 줄때 방향과 사거리 체크
            target.Damaged(defaultDamage);
    }
}
