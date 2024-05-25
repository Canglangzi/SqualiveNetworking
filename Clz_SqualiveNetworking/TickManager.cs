using SqualiveNetworking;
using SqualiveNetworking.Tick;
using SqualiveNetworking.Utils;
using UnityEngine;

public class TickManager : MonoBehaviour
{
    public static TickManager Instance { get; private set; }
    public TickSystem TickSystem { get; private set; }

    public float tickRate = 50f;
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

    public void InitializeTickSystem()
    {
        TickSystem = new TickSystem(true, (uint)tickRate);
    }

    public void Tick(float deltaTime)
    {
        if (TickSystem.Update(ref deltaTime))
        {
            if (NetworkServer.Initialized)
            {
                NetworkServer.Tick(deltaTime);
            }

            if (NetworkClient.Initialized)
            {
                NetworkClient.Tick(deltaTime);
            }
        }
    }
}
