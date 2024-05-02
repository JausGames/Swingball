using Agones;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UNET;
using UnityEngine;
using UnityEngine.UI;

public class ConnectionManager : MonoBehaviour
{
    [SerializeField] Button button_Host;
    [SerializeField] Button button_Server;
    [SerializeField] Button button_Client;
    [SerializeField] TMPro.TMP_InputField ip_address;

    [SerializeField] GameObject canvas;
    [SerializeField] MatchManager matchManager;
    [SerializeField] PlayerManager playerManager;

    [SerializeField]
    private ConnectionString ConnectionString
    {
        get
        {
            var co = new ConnectionString { Id = new Guid(), Name = PlayerSettings.Name, CharacterId = PlayerSettings.CharacterId, Licensed = true};
            Debug.Log("Connection string: " + (JsonUtility.ToJson(co)));
            return co;
        }
    }
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private UNetTransport transport;
    [SerializeField] bool startServerAuto = false;
    [SerializeField] bool startHostAuto = false;
    [SerializeField] private CharacterSelectorUi characterSelector;
    private AgonesAlphaSdk agones;


    Ping ping;
    List<int> pingsArray = new List<int>();

    [SerializeField] private bool localTest = false;


    Dictionary<ulong, NetworkObject> dict = new Dictionary<ulong, NetworkObject>();

    public int Ping
    {
        get
        {
            if (pingsArray.Count == 0)
                return 0;
            else
                return pingsArray.Sum() / pingsArray.Count;
        }
    }

    public PlayerManager PlayerManager { get { if (playerManager == null) playerManager = FindObjectOfType<PlayerManager>(); return playerManager; } set => playerManager = value; }
    public MatchManager MatchManager { get { if (matchManager == null) matchManager = FindObjectOfType<MatchManager>(); return matchManager; } set => matchManager = value; }

    private void Start()
    {
        ip_address.onSubmit.AddListener(delegate { transport.ConnectAddress = ip_address.text; });

#if UNITY_EDITOR
        Debug.unityLogger.logEnabled = true;
#else
  Debug.unityLogger.logEnabled = false;
#endif


        button_Host.onClick.AddListener(Host);
        button_Server.onClick.AddListener(Server);
        button_Client.onClick.AddListener(Client);

        NetworkManager.Singleton.OnServerStarted += HandleServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;

        if (startServerAuto)
            Server();
        if (startHostAuto)
            Host();
        else
            PlayerSettings.CharacterId = -1;
    }
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 70, 300, 300));
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
            StatusLabels();

        GUILayout.EndArea();
    }

    async void TryConnectToAgonesAsync()
    {

        agones = GetComponent<Agones.AgonesAlphaSdk>();
        agones.enabled = true;
        bool ok = await agones.Connect();
        if (ok)
        {
            Debug.Log(("Server - Connected"));
        }
        else
        {
            Debug.Log(("Server - Failed to connect, exiting"));
            Application.Quit(1);
        }
        try{

            ok = await agones.Ready();
            if (ok)
            {
                Debug.Log($"Server - Ready");
                //agones.SetPlayerCapacity(2);
            }
            else
            {
                Debug.Log($"Server - Ready failed");
            }
        }
        catch{

        }
    }

    private void OnDestroy()
    {
        // Prevent error in the editor
        if (NetworkManager.Singleton == null) { return; }

        NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;

    }
    private void Update()
    {
        if (NetworkManager.Singleton.IsClient && ping != null)
        {
            if (ping.isDone)
            {
                pingsArray.Add(ping.time);
                ping = new Ping(transport.ConnectAddress);
            }
        }
    }

    public void Host()
    {

        //if (inputName.text == "") return;
        // Hook up password approval check
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(JsonUtility.ToJson(ConnectionString));
        NetworkManager.Singleton.StartHost();
    }
    void Server()
    {
        TryConnectToAgonesAsync();

        //if (inputName.text == "") return;
        // Hook up password approval check
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(JsonUtility.ToJson(ConnectionString));
        NetworkManager.Singleton.StartServer();
    }
    void Client()
    {
        if (!localTest)
            CheckForServers();
        else
        {
            // Set password ready to send to the server to validate
            transport.ConnectAddress = "127.0.0.1";

            NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(JsonUtility.ToJson(ConnectionString));
            NetworkManager.Singleton.StartClient();
        }
    }

    // Update is called once per frame
    void CheckForServers()
    {
        PlayerGameInfo playerGameInfo = new PlayerGameInfo() { id = new System.Guid(), elo = 200, gameVersion = Application.version, name = "bogoss" };
        var http = gameObject.AddComponent<HttpRequestHelper>();
        CoroutineWithData cd = new CoroutineWithData(this, http.GetServer(playerGameInfo));
        StartCoroutine(WaitForGameServers(cd));
    }
    private IEnumerator WaitForGameServers(CoroutineWithData corout)
    {
        //wait
        while (!(corout.result is string) || corout.result == null)
        {
            Debug.Log("EditorUI, WaitForGameServers : data is null");
            yield return false;
        }
        //do stuff
        var gs = JsonUtility.FromJson<GameServer>((string)corout.result);
        //unetTransport.ConnectAddress = 

        Debug.Log(gs);

        transport.ConnectAddress = gs.ip;
        transport.ConnectPort = gs.port;

        NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(JsonUtility.ToJson(ConnectionString));
        NetworkManager.Singleton.StartClient();
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
        {
            _ = AddPlayerAndAllocate(clientId);
        }
        else
            characterSelector.gameObject.SetActive(false);

        // Are we the client that is connecting?
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            ping = new Ping(transport.ConnectAddress);
            canvas.SetActive(false);
        }
    }

    private async Task AddPlayerAndAllocate(ulong clientId)
    {
        var nb = await agones.GetPlayerCount();

        if (nb == 0) _ = agones.Allocate();

        _ = agones.PlayerConnect(clientId.ToString());
    }

    private void HandleClientDisconnect(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
        {
            _ = AgonesDisconectPlayerAsync(clientId);
        }

        // Are we the client that is disconnecting?
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            canvas.SetActive(false);
            //passwordEntryUI.SetActive(true);
            //leaveButton.SetActive(false);
        }
    }

    private async Task AgonesDisconectPlayerAsync(ulong clientId)
    {
        var ok = await agones.PlayerDisconnect(clientId.ToString());
        var nb = await agones.GetPlayerCount();
        if (nb == 0)
            await agones.Shutdown();
    }

    private void HandleServerStarted()
    {
        canvas.SetActive(false);
        // Temporary workaround to treat host as client
        if (NetworkManager.Singleton.IsHost)
        {
            //HandleClientConnected(NetworkManager.ServerClientId);
        }
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        // The client identifier to be authenticated
        var clientId = request.ClientNetworkId;

        // Additional connection data defined by user code
        var connectionData = request.Payload;


        var connectionString = JsonUtility.FromJson<ConnectionString>(Encoding.Default.GetString(connectionData));
        var gameReturnStatus = GetConnectStatus(connectionString);

        // Your approval logic determines the following values
        response.Approved = gameReturnStatus == ConnectStatus.Success ? true : false;

        response.CreatePlayerObject = false;

        // The prefab hash value of the NetworkPrefab, if null the default NetworkManager player prefab is used
        response.PlayerPrefabHash = null;

        // Position to spawn the player object (if null it uses default of Vector3.zero)
        response.Position = Vector3.zero;

        // Rotation to spawn the player object (if null it uses the default of Quaternion.identity)
        response.Rotation = Quaternion.identity;

        response.Reason = gameReturnStatus.ToString();

        // If additional approval steps are needed, set this to true until the additional steps are complete
        // once it transitions from true to false the connection approval response will be processed.
        response.Pending = false;
        if (response.Approved)
        {
            var pos = PlayerManager.Players.Count == 0 ? PlayerManager.Spawns[0] : PlayerManager.Spawns[1];
            characterSelector.gameObject.SetActive(false);
            var character = characterSelector.Prefabs[connectionString.CharacterId];
            GameObject go = Instantiate(character, pos.position, Quaternion.identity);
            var networkObject = go.GetComponent<NetworkObject>();
            networkObject.SpawnWithOwnership(clientId, false);

            PlayerManager.AddPlayer(go.GetComponent<Player>());

            dict.Add(clientId, networkObject);

        }
    }
    public async Task ShutdownServer()
    {
        SetCanvasActiveClientRpc();

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            dict.TryGetValue(client.ClientId, out NetworkObject obj);
            obj.Despawn();
            NetworkManager.Singleton.DisconnectClient(client.ClientId);
        }

        var shut = await agones.Shutdown();
    }

    [ClientRpc]
    private void SetCanvasActiveClientRpc()
    {
        canvas.SetActive(true);
    }

    enum ConnectStatus
    {
        Success,
        LoggedInAgain,
        ServerFull,
        IncompatibleBuildType
    }
    ConnectStatus GetConnectStatus(ConnectionString connectionPayload)
    {
        if (NetworkManager.Singleton.ConnectedClientsIds.Count >= 2)
        {
            return ConnectStatus.ServerFull;
        }
        if (!connectionPayload.Licensed)
            return ConnectStatus.IncompatibleBuildType;

        /*if (connectionPayload.isDebug != Debug.isDebugBuild)
        {
            return ConnectStatus.IncompatibleBuildType;
        }*/

        /*return SessionManager<SessionPlayerData>.Instance.IsDuplicateConnection(connectionPayload.playerId) ?
            ConnectStatus.LoggedInAgain : ConnectStatus.Success;*/
        return ConnectStatus.Success;
    }

    static void StatusLabels()
    {
        var mode = NetworkManager.Singleton.IsHost ?
            "Host" : NetworkManager.Singleton.IsServer ? "Server" : "Client";

        GUILayout.Label("Mode: " + mode);
        if (mode == "Client")
            GUILayout.Label("Ping: " + NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.ServerClientId));
    }


}

