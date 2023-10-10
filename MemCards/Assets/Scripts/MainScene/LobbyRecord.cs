using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyRecord : MonoBehaviour
{
    [SerializeField]
    private TMP_Text lobbyName;

    [SerializeField]
    private Button joinLobby;

    public void SetContent(Lobby lobby)
    {
        lobbyName.text = lobby.Name;
        joinLobby.onClick.AddListener(async () =>
        {
            LoadingTool.Singleton.ShowLoading("Joining lobby...");
            try
            {
                Lobby joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id);
                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(
                    joinedLobby.Data["RelayCode"].Value
                );
                SubNetworkManager.Singleton.JoinedLobby = joinedLobby;

                RelayServerData relayServerData = new(joinAllocation, "dtls");
                NetworkManager.Singleton
                    .GetComponent<UnityTransport>()
                    .SetRelayServerData(relayServerData);

                _ = NetworkManager.Singleton.StartClient();
                Destroy(LobbyPinger.Singleton.gameObject);
            }
            catch (LobbyServiceException ex)
            {
                Debug.LogError(ex.Message);
            }
            LoadingTool.Singleton.HideLoading();
        });
    }
}
