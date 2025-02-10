
using System;
using UnityEngine;

public class BossModel
{
    public event Action<int> HealthChanged;
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

    private bool attacking = false;
    public bool Attacking { get => attacking; set => attacking = value; }

    private float attackCoolTime = 0f;
    public float AttackCoolTime { get => attackCoolTime; set => attackCoolTime = value; }

    private Vector2 currnetDirection = Vector2.left;   // 현재 캐릭터 방향
    public Vector2 CurrnetDirection { get => currnetDirection; set => currnetDirection = value; }

    private bool moving = false;
    public bool Moving { get => moving; set => moving = value; }

    public BossModel(int maxHealth)
    {
        Health = maxHealth;
    }
}
