using Unity.Services.Lobbies.Models;
using UnityEngine;

public class SubNetworkManager : MonoBehaviour
{
    public static SubNetworkManager Singleton;

    private void Awake()
    {
        if (Singleton == null)
        {
            Singleton = this;
        }
    }

    public Lobby JoinedLobby;
}
