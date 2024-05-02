using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
using Cinemachine;
using UnityEngine.Events;
using System;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class MainMenuUi : MonoBehaviour
{
    [Header("Ui - Opening menu")]
    [SerializeField] GameObject OpeningMenu;

    [Header("Ui - Main menu")]
    [SerializeField] GameObject MainMenu;
    [SerializeField] Button PlayMenuButton;
    [SerializeField] Button SettingsButton;
    [SerializeField] Button ExitGameButton;

    [Header("Ui - Play menu")]
    [SerializeField] GameObject PlayMenu;
    [SerializeField] Button QuickGameButton;
    [SerializeField] Button TrainingGameButton;

    [Header("Ui - Play menu - Quick game menu")]
    [SerializeField] GameObject QuickGameMenu;
    [SerializeField] CharacterSelectorUi selector;
    [SerializeField] Button QuickMenu_Play;

    [Header("Ui - Play menu - Training game menu")]
    [SerializeField] GameObject TrainingMenu;
    [SerializeField] Button TrainingMenu_Play;

    [Header("Ui - Settings menu")]
    [SerializeField] GameObject SettingsMenu;
    [SerializeField] Button Settings_Back;

    [Header("Cinemachine")]
    [SerializeField] CinemachineDollyCart cart;
    float nextPos = 0f;
    [SerializeField] float cartSpeed = 1f;
    bool arrived = false;
    UnityEvent arrivedEvent = new UnityEvent();

    void MoveCart(float pos)
    {
        nextPos = pos;
        arrived = false;
    }

    void Start()
    {
        DontDestroyOnLoad(gameObject);
        MoveToOpeningMenu();
        // Main menu
        PlayMenuButton.onClick.AddListener(MoveToPlayMenu);
        SettingsButton.onClick.AddListener(MoveToSettingsMenu);
        ExitGameButton.onClick.AddListener(Application.Quit);
        // Play menu
        QuickGameButton.onClick.AddListener(MoveToQuickMenu);
        TrainingGameButton.onClick.AddListener(MoveToTrainingMenu);
        TrainingMenu_Play.onClick.AddListener(delegate { SceneManager.LoadScene("TrainingScene"); SceneManager.sceneLoaded += OnSceneLoaded; ; });
        // Settings menu
        Settings_Back.onClick.AddListener(Back);

        OnlineInputManager.Controls.Menu.Back.performed += _ => Back();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("OnSceneLoaded: " + scene.name);
        Debug.Log(mode);

        FindObjectOfType<ConnectionManager>().Host();
    }

    private void Back()
    {
        if (OpeningMenu.activeSelf)
            Application.Quit();
        else if (MainMenu.activeSelf)
            MoveToOpeningMenu();
        else if (PlayMenu.activeSelf)
            MoveToMainMenu();
        else if (QuickGameMenu.activeSelf)
            MoveToPlayMenu();
        else if (TrainingMenu.activeSelf)
            MoveToPlayMenu();
        else if (SettingsMenu.activeSelf)
            MoveToMainMenu();
    }

    void MoveToOpeningMenu()
    {
        MainMenu.SetActive(false);
        PlayMenu.SetActive(false);
        QuickGameMenu.SetActive(false);
        selector.gameObject.SetActive(false);
        SettingsMenu.SetActive(false);
        var action = new UnityAction(() =>
        {
            OpeningMenu.SetActive(true);
        });
        arrivedEvent.AddListener(action);
        arrivedEvent.AddListener(delegate { arrivedEvent.RemoveListener(action); });
        MoveCart(0f);
    }
    void MoveToMainMenu()
    {
        OpeningMenu.SetActive(false);
        PlayMenu.SetActive(false);
        QuickGameMenu.SetActive(false);
        selector.gameObject.SetActive(false);
        SettingsMenu.SetActive(false);
        var action = new UnityAction(() =>
        {
            MainMenu.SetActive(true);
        });
        arrivedEvent.AddListener(action);
        arrivedEvent.AddListener(delegate { arrivedEvent.RemoveListener(action); });
        MoveCart(1f);
    }
    void MoveToPlayMenu()
    {
        OpeningMenu.SetActive(false);
        MainMenu.SetActive(false);
        QuickGameMenu.SetActive(false);
        TrainingMenu.SetActive(false);
        selector.gameObject.SetActive(false);
        SettingsMenu.SetActive(false);
        var action = new UnityAction(() =>
        {
            PlayMenu.SetActive(true);
        });
        arrivedEvent.AddListener(action);
        arrivedEvent.AddListener(delegate { arrivedEvent.RemoveListener(action); });
        MoveCart(1f);
    }
    void MoveToSettingsMenu()
    {
        OpeningMenu.SetActive(false);
        MainMenu.SetActive(false);
        QuickGameMenu.SetActive(false);
        selector.gameObject.SetActive(false);
        PlayMenu.SetActive(false);
        var action = new UnityAction(() =>
        {
            SettingsMenu.SetActive(true);
        });
        arrivedEvent.AddListener(action);
        arrivedEvent.AddListener(delegate { arrivedEvent.RemoveListener(action); });
        MoveCart(1f);
    }
    void MoveToQuickMenu()
    {
        OpeningMenu.SetActive(false);
        PlayMenu.SetActive(false);
        MainMenu.SetActive(false);
        SettingsMenu.SetActive(false);

        var action = new UnityAction(() =>
        {
            QuickGameMenu.SetActive(true);
            selector.gameObject.SetActive(true);
        });
        arrivedEvent.AddListener(action);
        arrivedEvent.AddListener(delegate { arrivedEvent.RemoveListener(action); });
        MoveCart(2f);
    }
    void MoveToTrainingMenu()
    {
        OpeningMenu.SetActive(false);
        PlayMenu.SetActive(false);
        MainMenu.SetActive(false);
        SettingsMenu.SetActive(false);

        var action = new UnityAction(() =>
        {
            TrainingMenu.SetActive(true);
            selector.gameObject.SetActive(true);
        });
        arrivedEvent.AddListener(action);
        arrivedEvent.AddListener(delegate { arrivedEvent.RemoveListener(action); });
        MoveCart(2f);
    }
    private void Update()
    {
        if (OpeningMenu.activeSelf == true && ((Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.allControls.Any(x => x is ButtonControl button && x.IsPressed() && !x.synthetic))))
        {
            MoveToMainMenu();
        }

        if (nextPos != cart.m_Position && !arrived)
            cart.m_Position = Mathf.MoveTowards(cart.m_Position, nextPos, cartSpeed * Time.deltaTime);

        else if (!arrived)
        {
            arrived = true;
            arrivedEvent.Invoke();
        }


    }

}
