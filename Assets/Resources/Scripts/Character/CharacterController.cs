using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Animator))]
public class CharacterController : MonoBehaviour, ITarget
{
    // 수치 조절
    [Header("스텟")]
    [SerializeField] int maxHealth = 100;
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

    // MVC
    private CharacterView characterView;
    private CharacterModel characterModel;

    // 변수
    private Rigidbody2D rb;
    private Animator animator;
    private Coroutine RollCoroutine;

    public void Init(CharacterView view)
    {
        // GetComponent
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        // 캐릭터 뷰 설정
        characterView = view;
        characterView.maxHealth = maxHealth;

        // 캐릭터 모델 생성
        characterModel = new CharacterModel(maxHealth, FullCombo, ComboThreshold);
        characterModel.HealthChanged += characterView.OnHealthChanged;
        characterModel.StartRolling += () =>
        {
            if (RollCoroutine != null)
                StopCoroutine(RollCoroutine);
            RollCoroutine = StartCoroutine(RollUpdate());   // 코루틴으로 구르기 시작
        };

        // 기본 이동 속도 셋팅
        characterModel.MoveSpeed = PlayerSettings.PLAYER_SPEED_DEFAULT;

        // 센서 이벤트 등록
        groundSensor.TriggerEnterAction += () => characterModel.Grabbing = false;
        groundSensor.TriggerEnterAction += () => animator.SetBool("Ground", true);
        groundSensor.TriggerExitAction += () => animator.SetBool("Ground", false);
    }

    private void Update()
    {
        // YVelocity 체크
        animator.SetFloat("YVelocity", rb.velocity.y);

        // 공격 시간 체크
        characterModel.AttackTimer += Time.deltaTime;

        // 벽 모서리 감지한 경우 벽에 매달림
        if (!characterModel.Grabbing && gripSensor.IsCollided && !gripEmptySensor.IsCollided)
        {
            Debug.Log("그랩");
            characterModel.Grabbing = true;
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
        if (characterModel.Rolling || characterModel.Parrying) return false;
        if (characterModel.Blocking) BlockOff();
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

        if (characterModel.Grabbing && direction != characterModel.CurrnetDirection) GrabOff(); // 그랩중 반대 누르면 떨어짐

        animator.SetBool("Run", true);
        rb.velocity = new Vector2(direction.x * characterModel.MoveSpeed, rb.velocity.y);
        if (!Utils.VectorsApproximatelyEqual(characterModel.CurrnetDirection, direction))
        {
            // 방향 전환
            Vector3 newScale = transform.localScale;
            newScale.x = -newScale.x;
            transform.localScale = newScale;
            characterModel.CurrnetDirection = direction;
        }
    }

    public void Jump()
    {
        if (!ActionPreTest() || animator.GetBool("Jump") || (!characterModel.Grabbing && !groundSensor.IsCollided)) // 그랩중 또는 땅에 있는 경우 점프 가능
            return;

        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.AddForce(Vector3.up * jumpForce, ForceMode2D.Impulse);
        animator.SetTrigger("Jump");
    }

    public void Attack()
    {
        if (!ActionPreTest() || characterModel.AttackTimer < AttackDelay || !groundSensor.IsCollided || characterModel.Grabbing)  // 여러 조건을 검사하여 공격 (공격 딜레이, 공중 공격 불가, 그랩 중 불가 등)
            return;

        characterModel.AttackCount++;

        animator.SetTrigger("Attack" + characterModel.AttackCount);

        characterModel.AttackTimer = 0.0f;
    }

    public void BlockOn()
    {
        if (!ActionPreTest() || !groundSensor.IsCollided || characterModel.Grabbing)
            return;

        rb.velocity = Vector2.zero;
        characterModel.Parrying = true;
        animator.SetTrigger("Parry");       // 패링 애니메이션
        characterModel.Blocking = true;
        animator.SetBool("Blocking", characterModel.Blocking); // IdelBlock제어
    }

    public void BlockOff()
    {
        characterModel.Blocking = false;
        animator.SetBool("Blocking", characterModel.Blocking); // IdelBlock제어
    }

    public void Roll()
    {
        if (!ActionPreTest() || !groundSensor.IsCollided)
            return;

        animator.SetTrigger("Roll");
        characterModel.Rolling = true;
    }

    public IEnumerator RollUpdate()
    {
        while (characterModel.Rolling) // 애니메이션이 끝날때까지
        {
            rb.velocity = new Vector2(characterModel.CurrnetDirection.x * rollSpeed, rb.velocity.y);
            yield return null;
        }
        yield break;
    }

    public void GrabOff()
    {
        rb.AddForce(-characterModel.CurrnetDirection * 0.5f, ForceMode2D.Impulse);
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
        characterModel.Health -= damage;
        Debug.Log($"으악 데미지 {damage}만큼 받았다!");
    }

    public float Distance(Vector3 from)
    {
        Vector3 distance = transform.position - from;
        return distance.x;
    }

    // 애니메이션 이벤트
    private void AE_SetMoveSpeedDefault() => characterModel.MoveSpeed = PlayerSettings.PLAYER_SPEED_DEFAULT;
    private void AE_SetMoveSpeedSlow() => characterModel.MoveSpeed = PlayerSettings.PLAYER_SPEED_SLOW;
    private void AE_SetRollingEnd() => characterModel.Rolling = false;
    private void AE_SetParryingEnd() => characterModel.Parrying = false;
}
