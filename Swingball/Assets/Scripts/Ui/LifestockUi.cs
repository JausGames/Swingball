using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LifestockUi : MonoBehaviour
{
    [SerializeField] List<Image> lifes = new List<Image>();

    public void SetLifeLeft(int nb)
    {
        for (int i = 0; i < lifes.Count; i++)
            if (i < nb) lifes[i].enabled = true;
            else lifes[i].enabled = false;
    }
}