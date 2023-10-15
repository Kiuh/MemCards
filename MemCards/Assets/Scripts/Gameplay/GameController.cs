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
    private NetworkVariable<int> readyCounter = new(4);
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
                        readyCounter.Value++;
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
        if (
            !FindObjectsOfType<Player>()
                .Any(
                    x =>
                        x.GetComponent<NetworkObject>().OwnerClientId
                        == NetworkManager.Singleton.LocalClientId
                )
        )
        {
            SpawnPlayerServerRpc(NetworkManager.Singleton.LocalClientId);
        }
        SetPlayerRestartReadyServerRpc(false);
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
            .ForEach(x => x.SetLockInteraction(true));
        repeatView.gameObject.SetActive(true);
        repeatView.Ready.interactable = true;
        repeatView.WinLabel.text = winner + " Win";
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnPlayerServerRpc(ulong clientId)
    {
        PlayerSlot slot = FindObjectsOfType<PlayerSlot>().Where(x => !x.IsBusy).First();
        slot.GetComponent<NetworkObject>().ChangeOwnership(clientId);
        Player player = Instantiate(playerPrefab, slot.transform.position, slot.transform.rotation);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

        player.Deck.DeckConfig = slot.DeckConfig;
        player.Table.TableConfig = slot.TableConfig;
        player.DynamicPlayerConfig = slot.DynamicPlayerConfig;

        player.Deck.SpawnDeck(clientId);
        player.Deck.SetRandomCardValues();
        slot.IsBusy = true;
        InitPlayerClientRpc(clientId);
        if (FindObjectsOfType<Player>().Count() == 2)
        {
            SetPlayersEnemyClientRpc();
        }
    }

    [ClientRpc]
    private void InitPlayerClientRpc(ulong ownerPlayerId)
    {
        if (ownerPlayerId != NetworkManager.Singleton.LocalClientId)
        {
            return;
        }
        Player player = FindObjectsOfType<Player>()
            .FirstOrDefault(x => x.OwnerClientId == ownerPlayerId);
        PlayerSlot slot = FindObjectsOfType<PlayerSlot>()
            .FirstOrDefault(x => x.OwnerClientId == ownerPlayerId);
        if (player != null && slot != null)
        {
            player.Table.FillCardsPositions();
            player.Deck.FillCardsPositions();
            player.Deck.CollectOwnedCards();
        }
    }

    [ClientRpc]
    private void SetPlayersEnemyClientRpc()
    {
        Player[] players = FindObjectsOfType<Player>();
        players[0].EnemyPlayer = players[1];
        players[1].EnemyPlayer = players[0];
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
            allClientsReady = false;
            foreach (ulong key in playerRestartReadyDictionary.Keys.ToList())
            {
                playerRestartReadyDictionary[key] = false;
            }
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
        player.PreparePlayerToGameServerRpc();
    }

    [ClientRpc]
    private void StartGameClientRpc()
    {
        readyCounterText.gameObject.SetActive(false);
        FindObjectsOfType<Player>()
            .Where(x => x.IsLocalPlayer)
            .ToList()
            .ForEach(x =>
            {
                x.SetLockInteraction(false);
                x.BeginningGame();
            });
    }
}
