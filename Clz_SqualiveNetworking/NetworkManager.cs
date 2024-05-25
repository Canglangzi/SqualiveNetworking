using SqualiveNetworking;
using SqualiveNetworking.Message.Processor;
using SqualiveNetworking.Tick;
using SqualiveNetworking.Utils;
using System;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

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

    public bool useMessageLayer = false;
    public int maxPlayer = 40;
    public ushort port = 27015;

    public float Tick = 60f;

    [SerializeField]
    private string ipAddress = "127.0.0.1"; // Default IP address

    private MessageProcessor ClientTickProcessor;
    private MessageProcessor ServerTickProcessor;

    private bool isInitialized = false;
    private bool isConnected = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
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
    }

    private void OnServerStarted(NetworkDriver driver)
    {
        Debug.Log("[Server] Server started.");
    }

    private void OnClientConnected(ref ClientConnectedArgs args)
    {
        Debug.Log("[Client] Connected to server. ID: " + args.ClientID + ", IsLocal: " + args.IsLocal);
        isConnected = true;
        CurrentState = NetworkState.Client;
    }

    private void OnClientDisconnected(ref ClientDisconnectedArgs args)
    {
        Debug.LogWarning("[Client] Disconnected from server. Reason: " + args.Reason);
        isConnected = false;
        CurrentState = NetworkState.Disconnected;
    }

    protected virtual void OnClientConnected(ref ServerClientConnectedArgs args)
    {
        Debug.Log("[Server] Client connected to server. ID: " + args.ClientID);
        
        // 在这里执行针对新连接的操作，例如初始化客户端对象
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
    }

    public void StartServer()
    {
        Initialize();
        NetworkServer.Start(maxPlayer, port);
        CurrentState = NetworkState.Server;
        Debug.Log("Server started.");
    }

    public void StopServer()
    {
        NetworkServer.Stop();
        DeInitialize();
        CurrentState = NetworkState.Disconnected;
        Debug.Log("Server stopped.");
    }

    public void StartHost()
    {
        if (CurrentState == NetworkState.Disconnected)
        {
            Initialize();
            NetworkServer.Start(maxPlayer, port);
            bool connectionResult = NetworkClient.Connect(ipAddress, port);
            if (connectionResult)
            {
                CurrentState = NetworkState.Host;
                Debug.Log("Host started.");

            }
            else
            {
                Debug.LogError("Failed to start host: unable to connect to server.");
                DisconnectClient();
            }
        }
    }

    private void Initialize()
    {
        if (!isInitialized)
        {
            SqualiveLogger.Initialize(Debug.Log);

            NetworkClient.Initialize(new TickSystem(true, (uint)Tick), new NetworkSettings(Allocator.Temp));

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
            Debug.Log("NetworkManager Initialized.");
        }
    }

    private void DeInitialize()
    {
        if (isInitialized)
        {
            SqualiveLogger.DeInitialize();
            NetworkClient.DeInitialize();
            isInitialized = false;
            Debug.Log("NetworkManager DeInitialized.");
        }
    }

    private void AddLayers()
    {
        // Add your message layers logic here if needed
        Debug.Log("Layers added.");
    }

    private void AddMessageHandlers()
    {
        // Add your message handlers logic here if needed
        Debug.Log("Message handlers added.");
    }

    private void FixedUpdate()
    {
        if (CurrentState == NetworkState.Server || CurrentState == NetworkState.Host)
        {
            NetworkServer.Tick(Time.fixedDeltaTime);
            Debug.Log("Server ticked.");
        }

        if (CurrentState == NetworkState.Client || CurrentState == NetworkState.Host)
        {
            NetworkClient.Tick(Time.fixedDeltaTime);
            Debug.Log("Client ticked.");
        }
    }

    private void OnGUI()
    {
        GUIStyle fontStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
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
}
