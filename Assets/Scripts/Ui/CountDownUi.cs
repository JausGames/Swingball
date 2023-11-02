using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CountDownUi : MonoBehaviour
{
    [SerializeField] TMPro.TextMeshProUGUI text;
    [SerializeField] AudioSource source;
    [SerializeField] AudioClip[] clips;

    public void SetCount(int value)
    {
        switch (value)
        {
            case 0:
                text.text = "";
                break;
            default:
                text.text = value.ToString();
                break;
        }
        source.PlayOneShot(clips[value]);

    }
}
