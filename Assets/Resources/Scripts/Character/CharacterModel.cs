using System;
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
public class CharacterModel
{
    public event Action<int> HealthChanged;
    public event Action StartRolling;
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
            if (rolling)
                StartRolling?.Invoke();
        }
    }

    private bool blocking;
    public bool Blocking { get => blocking; set => blocking = value; }

    private bool parrying;
    public bool Parrying { get => parrying; set => parrying = value; }

    private bool grabbing;
    public bool Grabbing { get => grabbing; set => grabbing = value; }

    private float attackTimer = 0f;
    public float AttackTimer { get => attackTimer; set => attackTimer = value; }

    private int FullCombo;
    private float ComboThreshold;
    private int attackCount = 0;    // 현재 공격 단계
    public int AttackCount
    {
        get => attackCount; set
        {
            if (attackCount > FullCombo || attackTimer > ComboThreshold)
                attackCount = 1;
            else
                attackCount = value;
        }
    }

    private Vector2 currnetDirection = Vector2.right;   // 현재 캐릭터 방향
    public Vector2 CurrnetDirection { get => currnetDirection; set => currnetDirection = value; }

    public CharacterModel(int maxHealth, int fullCombo, float comboThreshold)
    {
        Health = maxHealth;
        FullCombo = fullCombo;
        ComboThreshold = comboThreshold;
    }
}
