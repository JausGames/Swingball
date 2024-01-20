using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class PlayerManager : NetworkBehaviour
{

    [Header("Player")]
    [SerializeField] List<Player> players = new List<Player>();
    [Header("Spawn")]
    [SerializeField] List<Transform> spawns = new List<Transform>();

    [Header("UI - Life stock")]
    [SerializeField] int[] lifestocks = new int[2];
    [SerializeField] LifestockUi[] lifestockUis = new LifestockUi[2];

    public List<Player> Players { get => players; set => players = value; }
    public List<Transform> Spawns { get => spawns; set => spawns = value; }
    public int[] Lifestocks { get => lifestocks; set => lifestocks = value; }


    [SerializeField] private int MaxPlayer = 1;
    [SerializeField] private MatchManager matchManager;

    public void AddPlayer(Player player)
    {
        if (players.Count >= MaxPlayer) return;

        if(matchManager.IsTraining)
        {
            player.IsTraining = true;
        }

        players.Add(player);

        if (players.Count == MaxPlayer)
            matchManager.AskSetUpGame();
    }

    public void RestarWholeGame()
    {
        SubmitResetLifeClientRpc();
        ResetLifeStocks();
        ResetHealthPlayers();
        ResetSpecialPlayers();
        ResurectPlayers();
    }

    public void RestartBall()
    {
        ReplacePlayers();
        ResetHealthPlayers();
        ResetSpecialPlayers();
    }

    public void EnableControls(bool value)
    {
        foreach (var pl in players)
        {
            if (pl.IsOwner)
            {
                pl.SetControls(value);
                if(true)
                    pl.DeadFromFalling = false;
            }
        }
    }

    public void ReplacePlayers()
    {
        for (int i = 0; i < players.Count; i++)
        {
            //Players[i].transform.position = spawns[i].position;
            players[i].ReplacePlayerClientRpc(spawns[i].position, spawns[i].rotation);
            if (players[i].isDead.Value) players[i].IsResurecting = true;
        }
    }
    [ClientRpc]
    public void SubmitLifeLostClientRpc(int i, int value)
    {
        if (players[i].IsOwner)
        {
            lifestocks[i] = value;
            lifestockUis[0].SetLifeLeft(lifestocks[i]);
        }
        else
        {
            lifestocks[i] = value;
            lifestockUis[1].SetLifeLeft(lifestocks[i]);
        }
    }

    [ClientRpc]
    private void SubmitResetLifeClientRpc()
    {
        ResetLifeStocks();
    }

    private void ResetLifeStocks()
    {
        for (int i = 0; i < players.Count; i++)
        {
            lifestocks[i] = 5;
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
        for (int i = 0; i < players.Count; i++)
        {
            players[i].health.Value = players[i].MaxHealth;
        }
    }
    private void ResetSpecialPlayers()
    {
        for (int i = 0; i < players.Count; i++)
        {
            players[i].special.Value = matchManager.IsTraining ? 100f : 0f;
        }
    }
    private void ResurectPlayers()
    {
        foreach (var pl in players)
        {
            if (pl.isDead.Value)
                pl.ResurectPlayerClientRpc(false);
        }
    }
}
