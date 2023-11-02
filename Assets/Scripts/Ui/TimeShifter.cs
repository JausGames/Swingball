using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimeShifter : MonoBehaviour
{
    [SerializeField] Slider slider;

    private void Awake()
    {
        slider.onValueChanged.AddListener(delegate { Time.timeScale = slider.value; });
    }
}
