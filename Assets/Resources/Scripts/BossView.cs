using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BossView : MonoBehaviour, IHealthView
{
    public Slider slider;

    public void OnHealthChanged(int health)
    {
        slider.value = health;
    }

    public void SetMaxHealth(int maxHealth)
    {
        slider.maxValue = maxHealth;
        slider.value = maxHealth;
    }
}
