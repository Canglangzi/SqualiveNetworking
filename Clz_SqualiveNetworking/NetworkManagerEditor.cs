using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NetworkManager))]
public class NetworkManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        NetworkManager networkManager = (NetworkManager)target;
        
        // Draw default inspector
        DrawDefaultInspector();
        
        GUILayout.Space(10);
        
        // Control buttons
        switch (networkManager.CurrentState)
        {
            case NetworkManager.NetworkState.Disconnected:
              
                if (GUILayout.Button("Start Server"))
                {
                    networkManager.StartServer();
                }
                break;
            case NetworkManager.NetworkState.Server:
              
                if (GUILayout.Button("Stop Server"))
                {
                    networkManager.StopServer();
                }
                break;
            case NetworkManager.NetworkState.Client:
              
                if (GUILayout.Button("Disconnect Client"))
                {
                    networkManager.DisconnectClient();
                }
                break;
        }
        
        GUILayout.Space(10);
        
       
        if (networkManager.CurrentState == NetworkManager.NetworkState.Disconnected || networkManager.CurrentState == NetworkManager.NetworkState.Server)
        {
            if (GUILayout.Button("Connect"))
            {
                networkManager.ConnectToLocal();
            }
        }
        
        GUILayout.Space(10);
        
      
        if (networkManager.CurrentState == NetworkManager.NetworkState.Disconnected)
        {
            if (GUILayout.Button("Start Host"))
            {
                networkManager.StartHost();
            }
        }
        
        GUILayout.Space(10);
        
      
        if (GUILayout.Button("Stop Everything"))
        {
            networkManager.StopServer();
            networkManager.DisconnectClient();
        }
    }
}
