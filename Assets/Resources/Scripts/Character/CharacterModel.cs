using System;

public class CharacterModel
{
    public event Action<int> HealthChanged;
    private int health;
    public int Health
    {
        get => health;
        set
        {
            health = Math.Max(0, value);
            HealthChanged?.Invoke(health);
        }
    }

    public CharacterModel(int maxHealth)
    {
        Health = maxHealth;
    }
}
