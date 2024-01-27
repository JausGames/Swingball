using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSelectorUi : MonoBehaviour
{

    [SerializeField] List<GameObject> prefabs = new List<GameObject>();
    [SerializeField] Transform gridUi;
    [SerializeField] GameObject buttonPrefab;
    [SerializeField] Camera dummyCamera;

    //public GameObject CurrentCharacter { get; set; }

    // Start is called before the first frame update
    void Start()
    {
        prefabs.ForEach(p =>
        {
            var obj = Instantiate<GameObject>(buttonPrefab, gridUi);
            var btn = obj.GetComponent<CharacterSelectorButton>();
            btn.Prefab = p;
            var prefab = btn.Prefab;
            btn.Button.onClick.AddListener(delegate
            {
                if (PlayerSettings.Character != prefab)
                {
                    PlayerSettings.Character = prefab;

                    if (dummyCamera.transform.childCount > 0)
                        for(int i = 0; i < dummyCamera.transform.childCount; i++) 
                            Destroy(dummyCamera.transform.GetChild(0).gameObject);

                    var player = prefab.GetComponent<Player>();

                    var obj = Instantiate(player.DummySelector, dummyCamera.transform);
                }
            });
        });


    }
}
