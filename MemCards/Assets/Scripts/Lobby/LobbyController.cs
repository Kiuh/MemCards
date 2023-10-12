using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyController : NetworkBehaviour
{
    [SerializeField]
    private Button leaveLobby;

    [SerializeField]
    private Button setReady;

    [SerializeField]
    private Button startGame;

    [SerializeField]
    private TMP_Text mainText;

    [SerializeField]
    private GameObject beginLevel;

    private bool isLocalPlayerReady = false;
    private bool allClientsReady = false;
    private Dictionary<ulong, bool> playerReadyDictionary = new();

    private void Awake()
    {
        leaveLobby.onClick.AddListener(async () =>
        {
            await LobbyService.Instance.RemovePlayerAsync(
                SubNetworkManager.Singleton.JoinedLobby.Id,
                AuthenticationService.Instance.PlayerId
            );
            NetworkManager.Singleton.GetComponent<UnityTransport>().DisconnectLocalClient();
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("Main");
        });
        setReady.onClick.AddListener(() =>
        {
            isLocalPlayerReady = !isLocalPlayerReady;
            SetPlayerReadyServerRpc(isLocalPlayerReady);
        });
        startGame.onClick.AddListener(() =>
        {
            Destroy(LobbyPinger.Singleton.gameObject);
            StartGameServerRpc();
        });
    }

    public override void OnNetworkSpawn()
    {
        SetPlayerReadyServerRpc(false);
        base.OnNetworkSpawn();
    }

    private void Update()
    {
        string text = $"Lobby name: {SubNetworkManager.Singleton.JoinedLobby.Name}\n";

        foreach (KeyValuePair<ulong, bool> clientId in playerReadyDictionary)
        {
            text += $"Client id: {clientId.Key} -- {clientId.Value}\n";
        }

        if (IsHost)
        {
            text += "You are host.\n";
            if (allClientsReady)
            {
                text += "All clients ready, you can start game.\n";
                startGame.gameObject.SetActive(true);
            }
            else
            {
                text += "NOT All clients ready, you can't start game.\n";
                startGame.gameObject.SetActive(false);
            }
        }
        else
        {
            text += "You are regular client, press ready and wait.\n";
            text += $"You are {(isLocalPlayerReady ? "READY" : "NOT READY")}";
            startGame.gameObject.SetActive(false);
        }

        mainText.text = text;
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartGameServerRpc()
    {
        StartGameClientRpc();
        GameController.Singleton.StartCountDownServerRpc();
    }

    [ClientRpc]
    private void StartGameClientRpc()
    {
        beginLevel.SetActive(false);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerReadyServerRpc(bool isReady, ServerRpcParams rpcParams = default)
    {
        playerReadyDictionary[rpcParams.Receive.SenderClientId] = isReady;

        allClientsReady = true;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (!playerReadyDictionary.ContainsKey(clientId) || !playerReadyDictionary[clientId])
            {
                allClientsReady = false;
                break;
            }
        }
    }
}
