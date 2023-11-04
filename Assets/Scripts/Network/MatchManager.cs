using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;


public class MatchManager : NetworkBehaviour
{
    [Header("Spawn")]
    [SerializeField] List<Transform> spawns = new List<Transform>();
    [SerializeField] Transform ballSpawn;
    [SerializeField] Collider floorCollider;
    [Header("Player")]
    [SerializeField] OnlinePlayer[] players = new OnlinePlayer[2] { null, null};

    [Header("UI")]
    [SerializeField] Canvas lifeStockCanvas;
    [SerializeField] Canvas restartCanvas;
    [SerializeField] CountDownUi countDownUi;

    [Header("UI - Health bars")]
    [SerializeField] HealthBar[] healthbars = new HealthBar[2];

    [Header("UI - Life stock")]
    [SerializeField] int[] lifestocks = new int[2];
    [SerializeField] LifestockUi[] lifestockUis = new LifestockUi[2];

    [Header("Ball")]
    [SerializeField] GameObject ballPrefab;
    [SerializeField] Ball ball;

    public OnlinePlayer[] Players { get => players; set => players = value; }
    public List<Transform> Spawns { get => spawns; set => spawns = value; }
    public Collider FloorCollider { get => floorCollider; set => floorCollider = value; }

    #region Set game up
    public IEnumerator AskSetUpGame()
    {
        if (IsServer)
        {
            yield return new WaitForSeconds(2f);

            for (int i = 0; i < players.Length; i++)
            {
                SetUpPlayerClientRpc(i, players[i].NetworkObjectId);
                Debug.Log("MatchManager, AskSetUpGame : SetUpPlayerClientRpc, i = " + i);
                /*if (players[i].IsOwner && IsHost)
                {
                    players[i].ResurectEvent.AddListener(delegate { TryInstantiateBall(i); });
                    Debug.Log("MatchManager, AskSetUpGame : TryInstantiateBall, i = " + i);
                }*/
            }


            SetUi();
            SetUpUiClientRpc();

            yield return new WaitForSeconds(1f);

            Debug.Log("MatchManager, AskSetUpGame : Players.Length  = " + Players.Length);

            var rnd = UnityEngine.Random.Range(0, 1);
            TryInstantiateBall(rnd);
            //ball.ChangeOwner(0);
            //ball.ChangeOwnerClientRpc(0);

        }

    }



    [ClientRpc]
    private void SetUpUiClientRpc()
    {

        SetUi();
    }

    [ClientRpc]
    private void SetUpPlayerClientRpc(int nb, ulong networkObjectId)
    {
        if (!IsServer) 
            players[nb] = GetNetworkObject(networkObjectId).GetComponent<OnlinePlayer>();

        if (players[nb].IsOwner)
        {
            players[nb].ResurectEvent.AddListener(delegate { TryInstantiateBallServerRpc(nb == 0 ? 0 : 1); });
            Debug.Log("MatchManager, SetUpPlayerClientRpc : nb = " + nb);
        }
    }

    private void SetUiServer()
    {
        lifeStockCanvas.gameObject.SetActive(true);
        for (int i = 0; i < players.Length; i++)
        {
            players[i].SetHealthBar(healthbars[i]);
        }
    }
    private void SetUiClient()
    {
        lifeStockCanvas.gameObject.SetActive(true);
        foreach (var pl in players)
        {
            if (pl.IsOwner)
                pl.SetHealthBar(healthbars[0]);
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


    public void AddPlayer(OnlinePlayer player)
    {
        var index = -1;
        if(players[0] == null)
            index = 0;

        else if (players[1] == null)
            index = 1;

        if (index == -1) return;

        players[index] = player;

        player.DieEvent.AddListener(delegate { ProcessDeath(index); });

        if (Players[0] != null && Players[1] != null)
            StartCoroutine(AskSetUpGame());
    }
    #endregion
    #region Instanciate ball
    //[ServerRpc(RequireOwnership = false)]
    internal void TryInstantiateBall(int target = 0, bool quickRestart = false, int increment = 0)
    {
        Debug.Log("MatchManager, TryInstantiateBall : target = " + target);
        StartCoroutine(quickRestart ? QuickRestartBall(target, increment) : RestartBall(target));
        StartCountdownClientRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    internal void TryInstantiateBallServerRpc(int target = 0, bool quickRestart = false, int increment = 0)
    {
        TryInstantiateBall(target, quickRestart, increment);
    }

    private void ReplacePlayers()
    {
        for (int i = 0; i < Players.Length; i++)
        {
            //Players[i].transform.position = spawns[i].position;
            Players[i].ReplacePlayerClientRpc(spawns[i].position, spawns[i].rotation);
            if (Players[i].isDead.Value) Players[i].IsResurecting = true;
        }
    }

    internal void InstantiateBall(int target, int increment = 0)
    {
        Debug.Log("MatchManager, InstantiateBall");
        if (ball && ball.GetComponent<NetworkObject>() && ball.GetComponent<NetworkObject>().IsSpawned)
            ball.GetComponent<NetworkObject>().Despawn();

        ball = Instantiate(ballPrefab, ballSpawn.position, Quaternion.identity, null).GetComponent<Ball>();
        ball.GetComponent<NetworkObject>().Spawn();
        ball.SetUpBall(this, target, increment);
    }
    [ClientRpc]
    private void StartCountdownClientRpc()
    {
        StartCoroutine(CountDown());
    }
    private IEnumerator CountDown()
    {
        foreach(var pl in players)
        {
            if(pl.IsOwner)
            {
                pl.SetControls(false);
            }
        }
        countDownUi.gameObject.SetActive(true);

        countDownUi.SetCount(3);
        yield return new WaitForSeconds(1f);
        countDownUi.SetCount(2);
        yield return new WaitForSeconds(1f);
        countDownUi.SetCount(1);
        yield return new WaitForSeconds(1f);
        countDownUi.SetCount(0);
        foreach (var pl in players)
        {
            if (pl.IsOwner)
            {
                pl.SetControls(true);
                pl.DeadFromFalling = false;
            }
        }
        yield return new WaitForSeconds(1f);


        countDownUi.gameObject.SetActive(false);

    }

    private IEnumerator RestartBall(int target = 0)
    {
        ReplacePlayers();
        ResetHealthPlayers();
        Debug.Log("MatchManager, RestartBall : target = " + target);

        yield return new WaitForSeconds(3f);

        InstantiateBall(target);
    }
    private IEnumerator QuickRestartBall(int target = 0, int increment = 0)
    {
        ReplacePlayers();
        //ResetHealthPlayers();
        Debug.Log("MatchManager, RestartBall : target = " + target);

        yield return new WaitForSeconds(3f);

        InstantiateBall(target, increment);
    }
    #endregion
    #region Death
    private void ProcessDeath(int i)
    {
        if (ball && ball.GetComponent<NetworkObject>() && ball.GetComponent<NetworkObject>().IsSpawned)
            ball.GetComponent<NetworkObject>().Despawn();

        Debug.Log("MatchManager, ProcessDeath : lifestocks = " + i);
        Debug.Log("MatchManager, ProcessDeath : stock = " + lifestocks[i]);
        lifestocks[i]--;

        if (lifestocks[i] == 0)
            GameOver(i);
        else
            GetNetworkObject(Players[i].NetworkObjectId).GetComponent<OnlinePlayer>().ResurectPlayerClientRpc(true);

        SubmitLifeLostClientRpc(i, lifestocks[i]);
    }

    [ClientRpc]
    private void SubmitLifeLostClientRpc(int i, int value)
    {
        if (players[i].IsOwner)
        {
            Debug.Log("MatchManager, SubmitLifeLostClientRpc : IsOwner, player = " + players[i]);
            lifestocks[i] = value;
            lifestockUis[0].SetLifeLeft(lifestocks[i]);
        }
        else
        {
            Debug.Log("MatchManager, SubmitLifeLostClientRpc : IsNotOwner, player = " + players[i]);
            lifestocks[i] = value;
            lifestockUis[1].SetLifeLeft(lifestocks[i]);
        }
    }
    #endregion
    #region Gameover & restart

    private void GameOver(int loserNb)
    {
        var winnerId = loserNb == 0 ? 1 : 0;
        ShowRestartClientRpc(winnerId);
    }

    [ClientRpc]
    private void ShowRestartClientRpc(int winnerId)
    {
        if(players[winnerId].IsOwner)
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

        SubmitResetLifeClientRpc();
        ResetLifeStocks();
        ResetHealthPlayers();
        ResurectPlayers();

        var rnd = UnityEngine.Random.Range(0, 1);
        Debug.Log("MatchManager, RestartGame : rnd = " + rnd);

        TryInstantiateBall(rnd);

    }

    [ClientRpc]
    private void SubmitResetLifeClientRpc()
    {
        ResetLifeStocks();
    }

    private void ResetLifeStocks()
    {
        for (int i = 0; i < Players.Length; i++)
        {
            lifestocks[i] = 5;
            Debug.Log("MatchManager, SubmitResetLifeClientRpc : id = " + i);
            if (players[i].IsOwner)
            {
                lifestockUis[0].SetLifeLeft(5);
            }
            else
            {
                lifestockUis[1].SetLifeLeft(5);
            }
        }
    }

    private void ResetHealthPlayers()
    {
        for (int i = 0; i < Players.Length; i++)
        {
            Players[i].health.Value = Players[i].MaxHealth;
        }
    }
    private void ResurectPlayers()
    {
        foreach(var pl in players)
        {
            if(pl.isDead.Value) 
                pl.ResurectPlayerClientRpc(false);
        }
    }
    #endregion
}
