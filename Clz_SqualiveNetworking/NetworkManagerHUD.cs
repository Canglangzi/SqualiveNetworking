using UnityEngine;

public class NetworkManagerHUD : MonoBehaviour
{
    private NetworkManager networkManager;

    public bool showGUI = true;
    public int offsetX;
    public int offsetY;

    void Start()
    {
        networkManager = NetworkManager.Instance;
    }

    void OnGUI()
    {
        if (!showGUI || networkManager == null)
            return;

        int xpos = 10 + offsetX;
        int ypos = 40 + offsetY;
        int spacing = 24;

        switch (networkManager.CurrentState)
        {
            case NetworkManager.NetworkState.Disconnected:
                if (GUI.Button(new Rect(xpos, ypos, 200, 20), "Host (Server + Client)"))
                {
                    networkManager.StartHost();
                }
                ypos += spacing;

                if (GUI.Button(new Rect(xpos, ypos, 200, 20), "Connect as Client"))
                {
                    networkManager.ConnectToLocal();
                }
                ypos += spacing;

                if (GUI.Button(new Rect(xpos, ypos, 200, 20), "Start Server Only"))
                {
                    networkManager.StartServer();
                }
                break;

            case NetworkManager.NetworkState.Server:
                if (GUI.Button(new Rect(xpos, ypos, 200, 20), "Stop Server"))
                {
                    networkManager.StopServer();
                }
                break;

            case NetworkManager.NetworkState.Client:
                if (GUI.Button(new Rect(xpos, ypos, 200, 20), "Disconnect"))
                {
                    networkManager.DisconnectClient();
                }
                break;

            case NetworkManager.NetworkState.Host:
                if (GUI.Button(new Rect(xpos, ypos, 200, 20), "Stop Host"))
                {
                    networkManager.DisconnectClient();
                    networkManager.StopServer();
                }
                break;
        }
    }
}
