using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Animator))]
public class CharacterController : MonoBehaviourPunCallbacks, ITarget
{
    // 센서
    [Header("센서")]
    [SerializeField] Sensor groundSensor;
    [SerializeField] Sensor gripSensorR;
    [SerializeField] Sensor gripEmptySensorR;
    [SerializeField] Sensor wallSensorR;
    [SerializeField] Sensor gripSensorL;
    [SerializeField] Sensor gripEmptySensorL;
    [SerializeField] Sensor wallSensorL;

    // MVC
    private CharacterView characterView;
    private CharacterModel characterModel;

    // 변수
    private Rigidbody2D rb;
    private Animator animator;
    private Coroutine RollCoroutine;
    private PhotonView PV;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        // GetComponent
        PV = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        characterModel = GetComponent<CharacterModel>();

        // 캐릭터 모델 이벤트 등록
        characterModel.RollingEvent += () =>
        {
            if (characterModel.Rolling)
            {
                if (RollCoroutine != null)
                    StopCoroutine(RollCoroutine);
                RollCoroutine = StartCoroutine(RollUpdate());   // 코루틴으로 구르기 시작
            }
        };
        characterModel.GrabbingChangeEvent += (bool isGrab) =>
        {
            if (isGrab)
            {
                Debug.Log("그랩");
                PV.RPC("RpcSetTrigger", RpcTarget.All, "WallGrab");
                PV.RPC("RpcSetYFreeze", RpcTarget.AllBuffered, true);
            }
        };

        // 기본 이동 속도 셋팅
        characterModel.MoveSpeed = PlayerSettings.PLAYER_SPEED_DEFAULT;

        // 센서 이벤트 등록
        groundSensor.TriggerEnterAction += () => characterModel.Grabbing = false;
        groundSensor.TriggerEnterAction += () => animator.SetBool("Ground", true);
        groundSensor.TriggerExitAction += () => animator.SetBool("Ground", false);
    }

    public void Init(CharacterView view)
    {
        if (!PV.IsMine)
            return;

        // 캐릭터 뷰 설정
        characterView = view;
        characterView.SetMaxHealth(characterModel.MaxHelath);

        // 캐릭터 모델 뷰 관련 이벤트 등록
        characterModel.HealthChanged += characterView.OnHealthChanged;
    }

    private void Update()
    {
        if (!PV.IsMine)
            return;
        // YVelocity 체크
        animator.SetFloat("YVelocity", rb.velocity.y);

        // 공격 시간 체크
        characterModel.AttackTimer += Time.deltaTime;

        // 벽 모서리 감지한 경우 벽에 매달림
        if (!characterModel.Grabbing && ((gripSensorR.IsCollided && !gripEmptySensorR.IsCollided) || (gripSensorL.IsCollided && !gripEmptySensorL.IsCollided)))
        {
            characterModel.Grabbing = true;
        }

        // 두 센서 다 감지된 경우 슬라이딩
        animator.SetBool("WallSlide", (wallSensorR.IsCollided && gripSensorR.IsCollided) || (wallSensorL.IsCollided && gripSensorL.IsCollided));
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
            PV.RPC("RpcFlipX", RpcTarget.AllBuffered, direction);
        }
    }

    public void Jump()
    {
        if (!ActionPreTest() || animator.GetBool("Jump") || (!characterModel.Grabbing && !groundSensor.IsCollided)) // 그랩중 또는 땅에 있는 경우 점프 가능
            return;

        PV.RPC("RpcSetYFreeze", RpcTarget.AllBuffered, false);
        rb.AddForce(Vector3.up * characterModel.JumpForce, ForceMode2D.Impulse);
        PV.RPC("RpcSetTrigger", RpcTarget.All, "Jump");
    }

    public void Attack()
    {
        if (!ActionPreTest() || characterModel.AttackTimer < characterModel.AttackDelay || !groundSensor.IsCollided || characterModel.Grabbing)  // 여러 조건을 검사하여 공격 (공격 딜레이, 공중 공격 불가, 그랩 중 불가 등)
            return;

        characterModel.AttackCount++;

        PV.RPC("RpcSetTrigger", RpcTarget.All, "Attack" + characterModel.AttackCount);

        characterModel.AttackTimer = 0.0f;
    }

    public void BlockOn()
    {
        if (!ActionPreTest() || !groundSensor.IsCollided || characterModel.Grabbing)
            return;

        rb.velocity = Vector2.zero;
        characterModel.Parrying = true;
        PV.RPC("RpcSetTrigger", RpcTarget.All, "Parry");      // 패링 애니메이션
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

        PV.RPC("RpcSetTrigger", RpcTarget.All, "Roll");
        characterModel.Rolling = true;
    }

    public IEnumerator RollUpdate()
    {
        while (characterModel.Rolling) // 애니메이션이 끝날때까지
        {
            rb.velocity = new Vector2(characterModel.CurrnetDirection.x * characterModel.RollSpeed, rb.velocity.y);
            yield return null;
        }
        characterModel.MoveSpeed = PlayerSettings.PLAYER_SPEED_DEFAULT;
        yield break;
    }

    public void GrabOff()
    {
        rb.AddForce(-characterModel.CurrnetDirection * 0.5f, ForceMode2D.Impulse);
        PV.RPC("RpcSetYFreeze", RpcTarget.AllBuffered, false);
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
        if (!characterModel.Rolling && !characterModel.Blocking && !characterModel.Grabbing && !characterModel.Parrying)
            PV.RPC("RpcSetTrigger", RpcTarget.All, "Hurt");
        Debug.Log($"으악 데미지 {damage}만큼 받았다!");
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
        spriteRenderer.flipX = Vector2.right != direction;
        characterModel.CurrnetDirection = direction;
    }
    [PunRPC]
    private void RpcSetTrigger(string s)
    {
        animator.SetTrigger(s);
    }
    [PunRPC]
    private void RpcSetYFreeze(bool freeze)
    {
        rb.constraints = freeze
    ? rb.constraints | RigidbodyConstraints2D.FreezePositionY
    : rb.constraints & ~RigidbodyConstraints2D.FreezePositionY;
    }

    // 애니메이션 이벤트
    private void AE_SetMoveSpeedDefault() => characterModel.MoveSpeed = PlayerSettings.PLAYER_SPEED_DEFAULT;
    private void AE_SetMoveSpeedSlow() => characterModel.MoveSpeed = PlayerSettings.PLAYER_SPEED_SLOW;
    private void AE_SetRollingEnd() => characterModel.Rolling = false;
    private void AE_SetParryingEnd() => characterModel.Parrying = false;
    private void AE_Attack(int combo)
    {
        Vector2 rayPosition = (Vector2)transform.position + Vector2.up;
        Debug.DrawRay(rayPosition, characterModel.CurrnetDirection * characterModel.AttackDistance, Color.red, 3f);
        int layerMask = 1 << LayerMask.NameToLayer("Enemy");
        RaycastHit2D hit = Physics2D.Raycast(rayPosition, characterModel.CurrnetDirection, characterModel.AttackDistance, layerMask);
        if (hit.collider != null)
        {
            hit.collider.GetComponent<ITarget>().Damaged(characterModel.ComboDamages[combo]);
        }
    }
}
