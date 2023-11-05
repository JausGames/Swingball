using System.Text;
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

    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private UNetTransport transport;
    [SerializeField] bool startServerAuto = false;

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
        var agones = GetComponent<Agones.AgonesSdk>();
        Debug.Log("Agones: TryConnectToAgonesAsync");
        bool connected = await agones.Connect();
        if (!connected)
        {
            Debug.Log("Agones: Connect() failed");
            return;
        }

        Debug.Log("Agones: .. connected");

        Debug.Log("Agones: Marking as ready...");
        bool readied = await agones.Ready();
    }

    private void OnDestroy()
    {
        // Prevent error in the editor
        if (NetworkManager.Singleton == null) { return; }

        NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;

    }

    void Host()
    {

        //if (inputName.text == "") return;
        // Hook up password approval check
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes("player01");
        NetworkManager.Singleton.StartHost();
    }
    void Server()
    {
        //if (inputName.text == "") return;
        // Hook up password approval check
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes("player01");
        NetworkManager.Singleton.StartServer();
    }
    void Client()
    {
        NetworkManager.Singleton.StartClient();
    }

    private void HandleClientConnected(ulong clientId)
    {
        Debug.Log("MainMenu, HandleClientConnected : clientid = " + clientId);
        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
        {

        }

        // Are we the client that is connecting?
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            canvas.SetActive(false);
        }
    }

    private void HandleClientDisconnect(ulong clientId)
    {
        // Are we the client that is disconnecting?
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            canvas.SetActive(false);
            //passwordEntryUI.SetActive(true);
            //leaveButton.SetActive(false);
        }
    }
    private void HandleServerStarted()
    {
        TryConnectToAgonesAsync();
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
        var playerName = Encoding.Default.GetString(connectionData);

        // Your approval logic determines the following values
        response.Approved = true;
        response.CreatePlayerObject = false;

        // The prefab hash value of the NetworkPrefab, if null the default NetworkManager player prefab is used
        response.PlayerPrefabHash = null;

        // Position to spawn the player object (if null it uses default of Vector3.zero)
        response.Position = Vector3.zero;

        // Rotation to spawn the player object (if null it uses the default of Quaternion.identity)
        response.Rotation = Quaternion.identity;

        // If additional approval steps are needed, set this to true until the additional steps are complete
        // once it transitions from true to false the connection approval response will be processed.
        response.Pending = false;
        Debug.Log("connection approval : name = " + playerName + ", id = " + clientId);
        Debug.Log("connection approval : matchManager.Players count = " + matchManager.Players.Length);


        var pos = matchManager.Players[0] == null ? matchManager.Spawns[0] : matchManager.Spawns[1];

        GameObject go = Instantiate(playerPrefab, pos.position, Quaternion.identity);
        var networkObject = go.GetComponent<NetworkObject>();
        networkObject.SpawnWithOwnership(clientId, false);

        matchManager.AddPlayer(go.GetComponent<OnlinePlayer>());
    }


    static void StatusLabels()
    {
        var mode = NetworkManager.Singleton.IsHost ?
            "Host" : NetworkManager.Singleton.IsServer ? "Server" : "Client";

        GUILayout.Label("Mode: " + mode);
        if(mode == "Client")
            GUILayout.Label("Ping: " + NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.ServerClientId));
    }


}

