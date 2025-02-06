using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterView : MonoBehaviour, IHealthView
{
    public Slider slider;
    public Text text;

    public int maxHealth { get; set; }

    public void OnHealthChanged(int health)
    {
        slider.value = health;
        text.text = $"{health}/{maxHealth}";
    }
}
