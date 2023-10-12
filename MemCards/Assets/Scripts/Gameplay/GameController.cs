using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : NetworkBehaviour
{
    [SerializeField]
    private RepeatView repeatView;

    [SerializeField]
    private TMP_Text readyCounterText;
    private NetworkVariable<int> readyCounter = new(-1);
    private List<string> readyStrings = new() { "Prepare!", "Ready?", "GO!" };
    private float readyDeltaTime = 1f;
    private float timer = 0f;

    public static GameController Singleton;

    private Dictionary<ulong, bool> playerRestartReadyDictionary = new();
    private bool allClientsReady = false;

    private void Awake()
    {
        Singleton = this;
        repeatView.Leave.onClick.AddListener(async () =>
        {
            await LobbyService.Instance.RemovePlayerAsync(
                SubNetworkManager.Singleton.JoinedLobby.Id,
                AuthenticationService.Instance.PlayerId
            );
            NetworkManager.Singleton.GetComponent<UnityTransport>().DisconnectLocalClient();
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("Main");
        });
        repeatView.Ready.onClick.AddListener(() =>
        {
            SetPlayerRestartReadyServerRpc(true);
            repeatView.Ready.interactable = false;
        });
    }

    private void Update()
    {
        if (IsServer)
        {
            if (readyCounter.Value < 3 && readyCounter.Value > -1)
            {
                if (timer > 0f)
                {
                    timer -= Time.deltaTime;
                }
                else
                {
                    timer = readyDeltaTime;
                    readyCounter.Value++;
                    if (readyCounter.Value == 3)
                    {
                        StartGameClientRpc();
                    }
                }
            }
        }
        if (readyCounter.Value < 3 && readyCounter.Value > -1)
        {
            readyCounterText.text = readyStrings[readyCounter.Value];
        }
    }

    [SerializeField]
    private Player playerPrefab;

    public override void OnNetworkSpawn()
    {
        SpawnPlayerServerRpc();
        SetPlayerRestartReadyServerRpc(false);
        base.OnNetworkSpawn();
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlayerLoseServerRpc(string winner)
    {
        PlayerLoseClientRpc(winner);
    }

    [ClientRpc]
    private void PlayerLoseClientRpc(string winner)
    {
        FindObjectsOfType<Player>()
            .Where(x => x.IsLocalPlayer)
            .ToList()
            .ForEach(x => x.SetInteraction(false));
        repeatView.gameObject.SetActive(true);
        repeatView.WinLabel.text = winner + " Win";
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnPlayerServerRpc(ServerRpcParams rpcParams = default)
    {
        PlayerSlot slot = FindObjectsOfType<PlayerSlot>().Where(x => !x.IsBusy).First();
        Player player = Instantiate(playerPrefab, slot.transform.position, slot.transform.rotation);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(rpcParams.Receive.SenderClientId);
        slot.IsBusy = true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerRestartReadyServerRpc(bool isReady, ServerRpcParams rpcParams = default)
    {
        playerRestartReadyDictionary[rpcParams.Receive.SenderClientId] = isReady;

        allClientsReady = true;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (
                !playerRestartReadyDictionary.ContainsKey(clientId)
                || !playerRestartReadyDictionary[clientId]
            )
            {
                allClientsReady = false;
                break;
            }
        }
        if (allClientsReady)
        {
            StartCountDownServerRpc();
        }
    }

    [ServerRpc]
    public void StartCountDownServerRpc()
    {
        StartCountDownClientRpc();
        timer = readyDeltaTime;
        readyCounter.Value = 0;
    }

    [ClientRpc]
    public void StartCountDownClientRpc()
    {
        repeatView.gameObject.SetActive(false);
        repeatView.Ready.interactable = true;
        readyCounterText.gameObject.SetActive(true);
        Player player = FindObjectsOfType<Player>().Where(x => x.IsLocalPlayer).First();
        player.ResetCamera();
    }

    [ClientRpc]
    private void StartGameClientRpc()
    {
        readyCounterText.gameObject.SetActive(false);
        FindObjectsOfType<Player>()
            .Where(x => x.IsLocalPlayer)
            .ToList()
            .ForEach(x => x.SetInteraction(false));
        // TODO
    }
}
