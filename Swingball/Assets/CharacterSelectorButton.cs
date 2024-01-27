using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectorButton : MonoBehaviour
{
    [SerializeField] new TMPro.TextMeshProUGUI name;
    [SerializeField] Image image;
    [SerializeField] GameObject prefab;
    [SerializeField] GameObject dummySelector;
    private Button button;

    public string Name { get => name.text; set => name.text = value; }
    public Sprite Image { get => image.sprite; set => image.sprite = value; }
    public Button Button { get => button; set => button = value; }
    public GameObject DummySelector { get => dummySelector; set => dummySelector = value; }

    public GameObject Prefab { get => prefab; set 
        {
            prefab = value;
            var player = prefab.GetComponent<Player>();
            DummySelector = player.DummySelector;
            button = gameObject.GetComponent<Button>();

            if (player.Image != null)
                Image = player.Image;
            else
                Name = prefab.name;
        } 
    }
}
