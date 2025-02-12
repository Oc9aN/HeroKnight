using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterView : MonoBehaviour, IHealthView
{
    public Slider slider;
    public Text text;

    private int maxHealth;

    public void OnHealthChanged(int health)
    {
        slider.value = health;
        text.text = $"{health}/{maxHealth}";
    }

    public void SetMaxHealth(int maxHealth)
    {
        this.maxHealth = maxHealth;
        slider.maxValue = maxHealth;
        slider.value = maxHealth;
    }
}
