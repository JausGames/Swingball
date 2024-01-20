using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;


public class MatchManager : NetworkBehaviour
{
    [Header("Spawn")]
    [SerializeField] Transform ballSpawn;
    [SerializeField] Collider floorCollider;

    [Header("UI")]
    [SerializeField] Canvas lifeStockCanvas;
    [SerializeField] Canvas restartCanvas;
    [SerializeField] CountDownUi countDownUi;

    [Header("UI - Bars")]
    [SerializeField] HealthBar[] healthbars = new HealthBar[2];
    [SerializeField] HealthBar specialBar;
    [SerializeField] HealthBar moveActionBar;

    [Header("Ball")]
    [SerializeField] GameObject ballPrefab;
    [SerializeField] Ball ball;
    [SerializeField] private CustomPassVolume customPassVolume;

    [Header("Players")]
    [SerializeField] PlayerManager playerManager;
    [Header("Settings")]
    [SerializeField] bool isTraining = false;

    public List<Player> Players { get => playerManager.Players; }
    public Collider FloorCollider { get => floorCollider; set => floorCollider = value; }
    public List<FakeBall> FakeBalls { get => fakeBalls; set => fakeBalls = value; }
    public bool IsTraining { get => isTraining; set => isTraining = value; }

    private List<FakeBall> fakeBalls = new List<FakeBall>();

    #region Set game up
    /*private void Awake()
    {
        playerManager.Ready.AddListener(delegate { AskSetUpGameServerRpc(); });
    }*/

    //[ServerRpc(RequireOwnership = false)]
    public void AskSetUpGame()
    {
        StartCoroutine(SetUpGame());
    }

    IEnumerator SetUpGame()
    {
        yield return new WaitForSeconds(2f);

        var players = playerManager.Players;
        for (int i = 0; i < players.Count; i++)
        {
            Debug.Log("SetUpGame : i = " + i);
            var player = players[i];
            player.DieEvent.AddListener(delegate { ProcessDeath(player.NetworkObjectId); });
            SetUpPlayerClientRpc(i, players[i].NetworkObjectId, isTraining); 
            if (IsTraining)
                player.IsTraining = true;
        }

        SetUi();
        SetUpUiClientRpc();

        yield return new WaitForSeconds(1f);

        //var rnd = UnityEngine.Random.Range(0, 1);
        TryInstantiateBall(0);
    }

    [ClientRpc]
    private void SetUpUiClientRpc()
    {
        SetUi();
    }

    [ClientRpc]
    private void SetUpPlayerClientRpc(int nb, ulong networkObjectId, bool isTraining = false)
    {
        if (!IsServer)
        {
            if (playerManager.Players.Count <= nb)
                while (playerManager.Players.Count <= nb)
                    playerManager.Players.Add(null);

            playerManager.Players[nb] = GetNetworkObject(networkObjectId).GetComponent<Player>();
        }

        if (playerManager.Players[nb].IsOwner)
            playerManager.Players[nb].ResurectEvent.AddListener(delegate { TryInstantiateBallServerRpc(nb == 0 ? 1 : 0); });
    }

    private void SetUiServer()
    {
        lifeStockCanvas.gameObject.SetActive(true);
        for (int i = 0; i < playerManager.Players.Count; i++)
        {
            playerManager.Players[i].SetHealthBar(healthbars[i]);
        }
    }
    private void SetUiClient()
    {
        Debug.Log("Start SetUiClient");
        lifeStockCanvas.gameObject.SetActive(true);
        foreach (var pl in playerManager.Players)
        {
            if (pl.IsOwner)
            {
                Debug.Log("SetUiClient : is owner");
                pl.SetHealthBar(healthbars[0]);
                pl.SetSpecialBar(specialBar);
                pl.SetMoveActionBar(moveActionBar);
            }
            else
                pl.SetHealthBar(healthbars[1]);
        }
    }

    private void SetUi()
    {
        if (IsServer && !IsHost)
            SetUiServer();
        else
            SetUiClient();
    }

    #endregion
    #region Instanciate ball
    //[ServerRpc(RequireOwnership = false)]
    internal void TryInstantiateBall(int target = 0, bool quickRestart = false, int increment = 0)
    {
        StartCoroutine(quickRestart ? QuickRestartBall(target, increment) : RestartBall(target));
        StartCountdownClientRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    internal void TryInstantiateBallServerRpc(int target = 0, bool quickRestart = false, int increment = 0)
    {
        TryInstantiateBall(target, quickRestart, increment);
    }


    internal void InstantiateBall(int target, int increment = 0)
    {
        if (ball && ball.GetComponent<NetworkObject>() && ball.GetComponent<NetworkObject>().IsSpawned)
            ball.GetComponent<NetworkObject>().Despawn();

        FakeBalls.ForEach(
            b =>
            {
                if (b.NetworkObject.IsSpawned) b.AskForDespawnServerRpc();
            });

        FakeBalls.Clear();

        ball = Instantiate(ballPrefab, ballSpawn.position, Quaternion.identity, null).GetComponent<Ball>();
        ball.GetComponent<NetworkObject>().Spawn();
        ball.SetUpBall(this, target, increment);
        foreach (var player in playerManager.Players)
        {
            player.SetUpBallClientRpc(ball.NetworkObjectId);
        }
    }
    [ClientRpc]
    private void StartCountdownClientRpc()
    {
        StartCoroutine(CountDown());
    }
    private IEnumerator CountDown()
    {
        playerManager.EnableControls(false);
        countDownUi.gameObject.SetActive(true);

        countDownUi.SetCount(3);
        yield return new WaitForSeconds(1f);
        countDownUi.SetCount(2);
        yield return new WaitForSeconds(1f);
        countDownUi.SetCount(1);
        yield return new WaitForSeconds(1f);
        countDownUi.SetCount(0);
        yield return new WaitForSeconds(1f);

        countDownUi.gameObject.SetActive(false);
        playerManager.EnableControls(true);
    }

    private IEnumerator RestartBall(int target = 0)
    {

        playerManager.RestartBall();

        yield return new WaitForSeconds(3f);

        InstantiateBall(target);
    }
    private IEnumerator QuickRestartBall(int target = 0, int increment = 0)
    {
        playerManager.ReplacePlayers();
        //ResetHealthPlayers();

        yield return new WaitForSeconds(3f);

        InstantiateBall(target, increment);
    }
    #endregion
    #region Death
    private void ProcessDeath(ulong objectId)
    {
        var nb = -1;
        for(var i = 0; i < playerManager.Players.Count; i++)
        {
            if (playerManager.Players[i].NetworkObjectId == objectId)
                nb = i;
        }
        Debug.Log("SetUpGame : ProcessDeath : i = " + nb);

        if (nb == -1) return;

        if (ball && ball.GetComponent<NetworkObject>() && ball.GetComponent<NetworkObject>().IsSpawned)
            ball.GetComponent<NetworkObject>().Despawn();

        playerManager.Lifestocks[nb]--;

        if (playerManager.Lifestocks[nb] == 0)
            GameOver(nb);
        else
            GetNetworkObject(playerManager.Players[nb].NetworkObjectId).GetComponent<Player>().ResurectPlayerClientRpc(true);

        playerManager.SubmitLifeLostClientRpc(nb, playerManager.Lifestocks[nb]);
    }

    #endregion
    #region Gameover & restart

    private void GameOver(int loserNb)
    {
        var winnerId = loserNb == 0 ? 1 : 0;

        _ = FindObjectOfType<ConnectionManager>().ShutdownServer();

        //ShowRestartClientRpc(winnerId);
    }

    [ClientRpc]
    private void EndGameClientRpc()
    {
        lifeStockCanvas.gameObject.SetActive(false);
    }

    [ClientRpc]
    private void ShowRestartClientRpc(int winnerId)
    {
        if (playerManager.Players[winnerId].IsOwner)
        {
            restartCanvas.gameObject.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public void RestartWholeGame()
    {
        restartCanvas.gameObject.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;

        RestartGameServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RestartGameServerRpc()
    {
        playerManager.RestarWholeGame();

        //var rnd = UnityEngine.Random.Range(0, 1);

        TryInstantiateBall(0);

    }



    internal void SetSeeThroughColor(Color color)
    {
        //((SeeThrough)customPassVolume.customPasses[0]).seeThroughMaterial.SetColor("_Color", color);
        ((SeeThrough)customPassVolume.customPasses[0]).seeThroughMaterial.color = color;
    }
    #endregion
}
