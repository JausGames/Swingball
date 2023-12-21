using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{

    public Slider slider;
    public Gradient gradient;
    public Image fill;

    public float Value { get => slider.value; }

    public void SetMaxHealth(float health)
    {
        slider.maxValue = health;
        slider.value = health;

        var color = gradient.Evaluate(slider.normalizedValue);

        fill.color = color;
        var fills = fill.GetComponentsInChildren<Image>();
        foreach (var f in fills)
        {
            if (f != fill)
                f.color = new Color(color.r, color.g, color.b, 1f);
        }
    }

    public void SetHealth(float health)
    {
        slider.value = health;

        var color = gradient.Evaluate(slider.normalizedValue);

        fill.color = color;
        var fills = fill.GetComponentsInChildren<Image>();
        foreach (var f in fills)
        {
            if (f != fill)
                f.color = new Color(color.r, color.g, color.b, 1f);
        }
    }

}
