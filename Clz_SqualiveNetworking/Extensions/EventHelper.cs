using SqualiveNetworking;
using UnityEngine;

public static class EventHelper
{
    public static void RegisterServerEvents()
    {
        NetworkServerEvent.ClientConnected += OnClientConnected;
        NetworkServerEvent.ClientDisconnected += OnClientDisconnected;
    }

    public static void UnregisterServerEvents()
    {
        NetworkServerEvent.ClientConnected -= OnClientConnected;
        NetworkServerEvent.ClientDisconnected -= OnClientDisconnected;
    }

    public static void RegisterClientEvents()
    {
        NetworkClientEvent.ClientDisconnected += OnClientDisconnected;
    }

    public static void UnregisterClientEvents()
    {
        NetworkClientEvent.ClientDisconnected -= OnClientDisconnected;
    }

    private static void OnClientConnected(ref ServerClientConnectedArgs args)
    {
        Debug.Log($"Client connected: {args.ClientID}");
     
     
    }

    private static void OnClientDisconnected(ref ServerClientDisconnectedArgs args)
    {
        Debug.Log($"Client disconnected: {args.ClientID}");
   
    }

    private static void OnClientDisconnected(ref ClientDisconnectedArgs args)
    {
        Debug.Log($"Client disconnected: {args.ClientID}");
    
    }
}
