using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Animator))]
public class PlayerSettings
{
    public static readonly float PLAYER_SPEED_DEFAULT = 3.0f;
    public static readonly float PLAYER_SPEED_SLOW = 1.0f;
    public static readonly float PLAYER_HITBOX_DEFAULT = 1.3f;
    public static readonly float PLAYER_HITBOX_SMALL = 1.0f;
    public static readonly float PLAYER_GRAVITY_SLOW = 0.5f;
    public static readonly float PLAYER_GRAVITY_DEFAULT = 2.0f;
}
public class CharacterController : MonoBehaviour, ITarget
{
    // 수치 조절
    [Header("스텟")]
    [SerializeField] float jumpForce = 5f;
    [SerializeField] float rollSpeed = 8f;
    [SerializeField] int FullCombo = 3;
    [Tooltip("콤보가 이어지는 시간")]
    [SerializeField] float ComboThreshold = 1.0f;
    [Tooltip("다음 공격까지 대기 시간")]
    [SerializeField] float AttackDelay = 0.25f;

    // 센서
    [Header("센서")]
    [SerializeField] Sensor groundSensor;
    [SerializeField] Sensor gripSensor;
    [SerializeField] Sensor gripEmptySensor;
    [SerializeField] Sensor wallSensor;

    // 변수
    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 currnetDirection = Vector2.right;   // 현재 캐릭터 방향
    private Coroutine RollCoroutine;

    // 제어 변수
    private int attackCount = 0;    // 현재 공격 단계
    private float attackTimer = 0f;
    private float moveSpeed { get { return animator.GetFloat("MoveSpeed"); } }
    private bool rolling { get { return animator.GetBool("Rolling"); } }
    private bool block { get { return animator.GetBool("Blocking"); } }
    private bool parrying { get { return animator.GetBool("Parrying"); } }
    private bool grabbing { get { return animator.GetBool("WallGrabbing"); } }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        animator.SetFloat("MoveSpeed", PlayerSettings.PLAYER_SPEED_DEFAULT);

        groundSensor.TriggerEnterAction += () => animator.SetBool("WallGrabbing", false);
        groundSensor.TriggerEnterAction += () => animator.SetBool("Ground", true);
        groundSensor.TriggerExitAction += () => animator.SetBool("Ground", false);
    }

    private void Update()
    {
        // YVelocity 체크
        animator.SetFloat("YVelocity", rb.velocity.y);

        // 공격 시간 체크
        attackTimer += Time.deltaTime;

        // 벽 모서리 감지한 경우 벽에 매달림
        if (!grabbing && gripSensor.IsCollided && !gripEmptySensor.IsCollided)
        {
            Debug.Log("그랩");
            animator.SetBool("WallGrabbing", true);
            animator.SetTrigger("WallGrab");
            rb.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
        }

        // 두 센서 다 감지된 경우 슬라이딩
        animator.SetBool("WallSlide", wallSensor.IsCollided && gripSensor.IsCollided);
    }

    /// <summary>
    /// 동작 전 사전 공통 체크사항
    /// </summary>
    /// <returns>false: 동작 수행 불가, true: 동작 수행 가능</returns>
    public bool ActionPreTest()
    {
        if (rolling || parrying) return false;
        if (block) BlockOff();
        return true;
    }

    public void Idle()
    {
        rb.velocity = new Vector2(0f, rb.velocity.y);
        animator.SetBool("Run", false);
    }

    public void Move(Vector2 direction)
    {
        if (!ActionPreTest())
            return;

        if (grabbing && direction != currnetDirection) GrabOff(); // 그랩중 반대 누르면 떨어짐

        animator.SetBool("Run", true);
        rb.velocity = new Vector2(direction.x * moveSpeed, rb.velocity.y);
        if (!Utils.VectorsApproximatelyEqual(currnetDirection, direction))
        {
            // 방향 전환
            Vector3 newScale = transform.localScale;
            newScale.x = -newScale.x;
            transform.localScale = newScale;
            currnetDirection = direction;
        }
    }

    public void Jump()
    {
        if (!ActionPreTest() || animator.GetBool("Jump") || (!grabbing && !groundSensor.IsCollided)) // 그랩중 또는 땅에 있는 경우 점프 가능
            return;

        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.AddForce(Vector3.up * jumpForce, ForceMode2D.Impulse);
        animator.SetTrigger("Jump");
    }

    public void Attack()
    {
        if (!ActionPreTest() || attackTimer < AttackDelay || !groundSensor.IsCollided || grabbing)  // 여러 조건을 검사하여 공격 (공격 딜레이, 공중 공격 불가, 그랩 중 불가 등)
            return;

        attackCount++;

        if (attackCount > FullCombo || attackTimer > ComboThreshold)
        {
            attackCount = 1;
        }

        animator.SetTrigger("Attack" + attackCount);
        animator.SetFloat("MoveSpeed", PlayerSettings.PLAYER_SPEED_SLOW);

        attackTimer = 0.0f;
    }

    public void BlockOn()
    {
        if (!ActionPreTest() || !groundSensor.IsCollided || grabbing)
            return;

        rb.velocity = Vector2.zero;
        animator.SetBool("Parrying", true);
        animator.SetTrigger("Parry");       // 패링 애니메이션
        animator.SetBool("Blocking", true); // IdelBlock제어
    }

    public void BlockOff()
    {
        animator.SetBool("Blocking", false); // IdelBlock제어
    }

    public void Roll()
    {
        if (!ActionPreTest() || !groundSensor.IsCollided)
            return;

        animator.SetTrigger("Roll");
        animator.SetBool("Rolling", true);
        if (RollCoroutine != null)
            StopCoroutine(RollCoroutine);
        RollCoroutine = StartCoroutine(RollUpdate());   // 코루틴으로 구르기 시작
    }

    public IEnumerator RollUpdate()
    {
        while (rolling) // 애니메이션이 끝날때까지
        {
            rb.velocity = new Vector2(currnetDirection.x * rollSpeed, rb.velocity.y);
            yield return null;
        }
        yield break;
    }

    public void GrabOff()
    {
        rb.AddForce(-currnetDirection * 0.5f, ForceMode2D.Impulse);
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    // 낙하 속도 조절
    public void GravitySlow()
    {
        rb.gravityScale = PlayerSettings.PLAYER_GRAVITY_SLOW;
    }

    public void GravityDefault()
    {
        rb.gravityScale = PlayerSettings.PLAYER_GRAVITY_DEFAULT;
    }

    public void Damaged(int damage)
    {
        Debug.Log($"으악 데미지 {damage}만큼 받았다!");
    }

    public float Distance(Vector3 from)
    {
        Vector3 distance = transform.position - from;
        return distance.x;
    }
}
