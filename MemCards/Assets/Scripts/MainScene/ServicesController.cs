using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ServicesController : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField lobbyName;

    [SerializeField]
    private LobbiesList lobbiesList;

    [SerializeField]
    private Button createLobby;

    [SerializeField]
    private Button refreshLobbies;

    private void Awake()
    {
        createLobby.onClick.AddListener(CreateLobby);
        refreshLobbies.onClick.AddListener(RefreshLobbies);
    }

    private async void Start()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            await UnityServices.InitializeAsync();

#if UNITY_EDITOR
            if (ParrelSync.ClonesManager.IsClone())
            {
                // When using a ParrelSync clone, switch to a different authentication profile to force the clone
                // to sign in as a different anonymous user account.
                string customArgument = ParrelSync.ClonesManager.GetArgument();
                AuthenticationService.Instance.SwitchProfile($"Clone_{customArgument}_Profile");
            }
#endif

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    public async void CreateLobby()
    {
        try
        {
            LoadingTool.Singleton.ShowLoading("Creating lobby...");
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            RelayServerData relayServerData = new(allocation, "dtls");
            NetworkManager.Singleton
                .GetComponent<UnityTransport>()
                .SetRelayServerData(relayServerData);

            Dictionary<string, DataObject> data =
                new()
                {
                    { "RelayCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) }
                };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(
                lobbyName.text,
                2,
                new CreateLobbyOptions() { Data = data }
            );
            SubNetworkManager.Singleton.JoinedLobby = lobby;
            LobbyPinger.Singleton.StartPingLobby(lobby);

            _ = NetworkManager.Singleton.StartHost();
            _ = NetworkManager.Singleton.SceneManager.LoadScene("GamePlay", LoadSceneMode.Single);
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError(ex.Message);
        }
        finally
        {
            LoadingTool.Singleton.HideLoading();
        }
    }

    public async void RefreshLobbies()
    {
        try
        {
            QueryResponse list = await LobbyService.Instance.QueryLobbiesAsync(
                new QueryLobbiesOptions()
                {
                    Filters = new List<QueryFilter>()
                    {
                        new(QueryFilter.FieldOptions.AvailableSlots, "1", QueryFilter.OpOptions.EQ)
                    }
                }
            );
            lobbiesList.RefreshList(list.Results);
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    public void Exit()
    {
        Application.Quit();
    }
}
