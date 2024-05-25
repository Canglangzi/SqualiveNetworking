using CLz_SqualiveNetworkingHelper;
using SqualiveNetworking;
using SqualiveNetworking.Message.Processor;
using SqualiveNetworking.Tick;
using SqualiveNetworking.Utils;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum NetworkState
{
    Disconnected,
    Server,
    Client,
    Host
}

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }


    public NetworkState CurrentState { get; protected set; }

    [SerializeField] bool useMessageLayer = false;
    [SerializeField] private int maxPlayer = 40;
    [SerializeField] private ushort port = 27015;

    [SerializeField] private float Tick = 60f;

    [SerializeField] private int connectionTimeoutMS = 1000;
    [SerializeField] private int maxConnectAttempts = 5;

    [SerializeField] private string ipAddress = "127.0.0.1"; 

    private MessageProcessor ClientTickProcessor;
    private MessageProcessor ServerTickProcessor;

    private bool isInitialized = false;
    private bool isConnected = false;

    [Header("Scenes")] public string offlineScene = "OfflineScene";
    public string onlineScene = "OnlineScene";

    [Header("Prefabs")] public GameObject playerPrefab;

    [Header("Options")] public bool loadScenes = true;
    public bool spawnPrefabs = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning("Multiple instances of NetworkManager found. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Debug.Log("NetworkManager Initialized.");
    }

    private void OnEnable()
    {
        NetworkServerEvent.ClientConnected += OnClientConnected;
        NetworkServerEvent.ServerStarted += OnServerStarted;
        NetworkServerEvent.ServerStopped += OnServerStopped;
        NetworkClientEvent.ClientDisconnected += OnClientDisconnected;
        NetworkClientEvent.ClientConnected += OnClientConnected;
    }

    private void OnDisable()
    {
        NetworkServerEvent.ClientConnected -= OnClientConnected;
        NetworkServerEvent.ServerStarted -= OnServerStarted;
        NetworkServerEvent.ServerStopped -= OnServerStopped;
        NetworkClientEvent.ClientDisconnected -= OnClientDisconnected;
        NetworkClientEvent.ClientConnected -= OnClientConnected;
    }

    private void OnServerStopped()
    {
        Debug.Log("[Server] Server stopped.");
        if (loadScenes)
        {
            SceneManager.LoadScene(offlineScene);
        }

        CurrentState = NetworkState.Disconnected;
    }

    private void OnServerStarted(NetworkDriver driver)
    {
        Debug.Log("[Server] Server started.");
        NetworkServerGen.AddMessageHandlers();
        if (loadScenes)
        {
            SceneManager.LoadScene(onlineScene);
        }

        CurrentState = NetworkState.Server;
    }

    private void OnClientConnected(ref ClientConnectedArgs args)
    {
        NetworkClientGen.AddMessageHandlers();

        Debug.Log("[Client] Connected to server. ID: " + args.ClientID + ", IsLocal: " + args.IsLocal);
        isConnected = true;
        CurrentState = NetworkState.Client;
        if (loadScenes)
        {
            SceneManager.LoadScene(onlineScene);
        }
    }

    private void OnClientDisconnected(ref ClientDisconnectedArgs args)
    {
        Debug.LogWarning("[Client] Disconnected from server. Reason: " + args.Reason);
        isConnected = false;
        CurrentState = NetworkState.Disconnected;
        if (loadScenes)
        {
            SceneManager.LoadScene(offlineScene);
        }
    }

    protected virtual void OnClientConnected(ref ServerClientConnectedArgs args)
    {
        Debug.Log("[Server] Client connected to server. ID: " + args.ClientID);
        if (spawnPrefabs)
        {
            SpawnPlayer(args.ClientID);
        }
    }

    public void ConnectClient()
    {
        Initialize();
        bool connectionResult = NetworkClient.Connect(ipAddress, port);
        if (connectionResult)
        {
            CurrentState = NetworkState.Client;
            Debug.Log("Client connected.");
        }
        else
        {
            Debug.LogError("Failed to start connecting.");
            DisconnectClient();
        }
    }

    public void DisconnectClient()
    {
        NetworkClient.Disconnect();
        DeInitialize();
        CurrentState = NetworkState.Disconnected;
        isConnected = false;
        Debug.Log("Client disconnected.");
        if (loadScenes)
        {
            SceneManager.LoadScene(offlineScene);
        }
    }

    public void StartServer()
    {
        Initialize();
        NetworkServer.Start(maxPlayer, port);
        CurrentState = NetworkState.Server;
        Debug.Log("Server started.");
        if (loadScenes)
        {
            SceneManager.LoadScene(onlineScene);
        }
    }

    public void StopServer()
    {
        NetworkServer.Stop();
        DeInitialize();
        CurrentState = NetworkState.Disconnected;
        Debug.Log("Server stopped.");
        if (loadScenes)
        {
            SceneManager.LoadScene(offlineScene);
        }
    }

    public void StartHost()
    {
        if (CurrentState != NetworkState.Disconnected)
        {
            Debug.LogWarning("Cannot start host: network is already in a connected state.");
            return;
        }

        Initialize();
        NetworkServer.Start(maxPlayer, port);
        bool connectionResult = NetworkClient.Connect(ipAddress, port);
        if (connectionResult)
        {
            CurrentState = NetworkState.Host;
            Debug.Log("Host started.");
            if (loadScenes)
            {
                SceneManager.LoadScene(onlineScene);
            }
        }
        else
        {
            Debug.LogError("Failed to start host: unable to connect to server.");
            // 不立即断开连接，而是记录错误并允许用户手动断开连接
        }
    }

    public void StopHost()
    {
        if (CurrentState == NetworkState.Host)
        {
            NetworkServer.Stop();
            DisconnectClient();
            Debug.Log("Host stopped.");
        }
        else
        {
            Debug.LogWarning("Cannot stop host: not currently hosting.");
        }
    }

    private void Initialize()
    {
        if (!isInitialized)
        {
      
            SqualiveLogger.Initialize(Debug.Log);
            NetworkClient.Initialize(new TickSystem(true, (uint)Tick), new NetworkSettings(), connectionTimeoutMS, maxConnectAttempts);
            ClientTickProcessor = NetworkClient.CreateProcessor(new IMessageProcessorStage[] { new TickProcessorStage() });
            if (useMessageLayer)
            {
                AddLayers();
            }
            else
            {
                AddMessageHandlers();
            }

            isInitialized = true;
        }
    }

    private void DeInitialize()
    {
        if (isInitialized)
        {
            // 反初始化网络客户端
            SqualiveLogger.DeInitialize();
            NetworkClient.DeInitialize();
            isInitialized = false;
        }
    }

    private void AddLayers()
    {
   
    }

    private void AddMessageHandlers()
    {
  
    }

    private void FixedUpdate()
    {

        if (CurrentState == NetworkState.Server || CurrentState == NetworkState.Host)
        {
            NetworkServer.Tick(Time.fixedDeltaTime);
        }

        if (CurrentState == NetworkState.Client || CurrentState == NetworkState.Host)
        {
            NetworkClient.Tick(Time.fixedDeltaTime);
        }
    }
    private void OnGUI()
    {
        GUIStyle fontStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
        };

        GUILayout.Label($"Network State: {CurrentState}", fontStyle);
        GUILayout.Label($"Server Tick: {NetworkServer.CurrentTick}", fontStyle);
        GUILayout.Label($"Client Tick: {NetworkClient.CurrentTick}", fontStyle);

        if (GUILayout.Button("Close GUI"))
        {
            gameObject.SetActive(false);
            Debug.Log("GUI closed.");
        }
    }

    public void SpawnPlayer(ushort clientId)
    {
        // 生成玩家的方法
        if (playerPrefab != null)
        {
            GameObject player = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
            player.name = $"Player_{clientId}";
            Debug.Log($"Spawned player with ID: {clientId}");
            PlayerSpawnMessage message = new PlayerSpawnMessage
            {
                Position = player.transform.position,
                Rotation = player.transform.rotation
            };
            message.Position = new Vector3(1.0f, 2.0f, 3.0f);
            message.Rotation = Quaternion.identity;
            message.IsOwner = true;

            MessageHelper.SendToAll(message);
            Debug.Log($"Spawned player  Message Send");
        }
        else
        {
            Debug.LogError("Player prefab is not assigned.");
        }
    }

    [ClientMessageHandler((ushort)ServerToClientID.SpawnPlayer)]
    public static void OnSpawnPlayerMessage(ref MessageReceivedArgs args)
    {
    
        PlayerSpawnMessage message = new PlayerSpawnMessage();
        message.Deserialize(ref args.Stream);

        GameObject player = Instantiate(NetworkManager.Instance.playerPrefab, message.Position, message.Rotation);
        //player.name = $"Player_{args.SenderID}"; // 使用发送者的ID命名玩家

        // NetworkManager.Instance.AddPlayer(player);
    }

    public struct PlayerSpawnMessage : INetMessage
    {
       
        public Vector3 Position;
        public Quaternion Rotation;
        public bool IsOwner;

        public void Deserialize(ref DataStreamReader reader)
        {
           
            Position = reader.ReadVector3();
            Rotation = reader.ReadQuaternion();
            IsOwner = reader.ReadBool();
        }

        public ushort MessageID() => (ushort)ServerToClientID.SpawnPlayer;

        public void Serialize(ref DataStreamWriter writer)
        {
          
            writer.WriteVector3(Position);
            writer.WriteQuaternion(Rotation);
            writer.WriteBool(IsOwner);
        }
    }

    public enum ServerToClientID : ushort
    {

        SpawnPlayer = 1,
    }
}
