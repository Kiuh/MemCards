using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyPinger : MonoBehaviour
{
    public static LobbyPinger Singleton;

    private Lobby hostLobby;
    private float lobbyHeartBeatTimer = 0;

    private void Start()
    {
        DontDestroyOnLoad(this);
        if (Singleton == null)
        {
            Singleton = this;
        }
    }

    public void StartPingLobby(Lobby lobby)
    {
        hostLobby = lobby;
    }

    private void Update()
    {
        HandleLobbyHeartBeat();
    }

    private async void HandleLobbyHeartBeat()
    {
        if (hostLobby != null)
        {
            if (lobbyHeartBeatTimer < 0)
            {
                lobbyHeartBeatTimer = 15;
                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
            else
            {
                lobbyHeartBeatTimer -= Time.deltaTime;
            }
        }
    }
}
