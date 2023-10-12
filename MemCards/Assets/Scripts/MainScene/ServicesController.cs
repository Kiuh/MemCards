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

public class ServicesController : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField lobbyName;

    [SerializeField]
    private LobbiesList lobbiesList;

    private async void Start()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            await UnityServices.InitializeAsync();

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    // Called by button create lobby
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

    // Called by Refresh lobbies
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
}
