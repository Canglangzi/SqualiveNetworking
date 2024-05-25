using SqualiveNetworking;
using SqualiveNetworking.Message.Processor;
using SqualiveNetworking.Tick;
using SqualiveNetworking.Utils;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    public int MaxPlayers = 40;
    public ushort Port = 27015;
    public string ServerIP = "127.0.0.1";
    public bool UseIPv6 = false;

    public enum NetworkState
    {
        Disconnected,
        Server,
        Client,
        Host
    }

    public NetworkState CurrentState { get; private set; } = NetworkState.Disconnected;

    private bool isServerRunning = false;
    private bool isClientConnected = false;

    private MessageProcessor ClientTickProcessor;
    private MessageProcessor ServerTickProcessor;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        SqualiveLogger.Initialize(Debug.Log);
        TickManager.Instance.InitializeTickSystem();
    }

    public void StartServer()
    {
        if (!isServerRunning)
        {
            var networkSettings = new NetworkSettings(Allocator.Temp);
            NetworkServer.Start(MaxPlayers, Port, UseIPv6, TickManager.Instance.TickSystem, networkSettings, new UDPNetworkInterface());
            isServerRunning = true;
            CurrentState = NetworkState.Server;
            Debug.Log("Server started");

            ServerTickProcessor = NetworkServer.CreateProcessor(new IMessageProcessorStage[] { new TickProcessorStage() });
        }
        else
        {
            Debug.LogWarning("Server is already running");
        }
    }

    public void StopServer()
    {
        if (isServerRunning)
        {
            NetworkServer.Stop();
            isServerRunning = false;
            if (!isClientConnected)
            {
                CurrentState = NetworkState.Disconnected;
            }
            Debug.Log("Server stopped");
        }
        else
        {
            Debug.LogWarning("Server is not running");
        }
    }

    public void ConnectClient(string ipAddress, ushort port)
    {
        if (!isClientConnected)
        {
            if (!NetworkClient.IsInitialized)
            {
                NetworkClient.Initialize(TickManager.Instance.TickSystem, new NetworkSettings(Allocator.Temp));
            }
            NetworkClient.Connect(ipAddress, port);
            isClientConnected = true;
            CurrentState = NetworkState.Client;
            Debug.Log($"Client connected to {ipAddress}:{port}");

            ClientTickProcessor = NetworkClient.CreateProcessor(new IMessageProcessorStage[] { new TickProcessorStage() });
        }
        else
        {
            Debug.LogWarning("Client is already connected");
        }
    }

    public void ConnectToLocal()
    {
        ConnectClient("127.0.0.1", Port);
    }

    public void DisconnectClient()
    {
        if (isClientConnected)
        {
            NetworkClient.Disconnect();
            isClientConnected = false;
            if (!isServerRunning)
            {
                CurrentState = NetworkState.Disconnected;
            }
            Debug.Log("Client disconnected");
        }
        else
        {
            Debug.LogWarning("Client is not connected");
        }
    }

    public void StartHost()
    {
        if (!isServerRunning)
        {
            StartServer();
        }
        if (!isClientConnected)
        {
            ConnectToLocal();
        }
        CurrentState = NetworkState.Host;
    }

    private void OnEnable()
    {
        NetworkServerEvent.ClientConnected += OnClientConnected;
    }

    private void OnDisable()
    {
        NetworkServerEvent.ClientConnected -= OnClientConnected;
    }

    private void OnClientConnected(ref ServerClientConnectedArgs args)
    {
        Debug.Log("Client connected to server");
    }

    private void OnDestroy()
    {
        SqualiveLogger.DeInitialize();
        if (isServerRunning) NetworkServer.Stop();
        if (isClientConnected) NetworkClient.DeInitialize();
    }

    private void Update()
    {
        if (isServerRunning || isClientConnected)
        {
            TickManager.Instance.Tick(Time.deltaTime);
        }
    }
}
