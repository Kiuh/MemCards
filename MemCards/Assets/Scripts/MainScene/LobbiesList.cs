using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbiesList : MonoBehaviour
{
    [SerializeField]
    private LobbyRecord prefab;

    [SerializeField]
    private Transform container;

    public void RefreshList(List<Lobby> values)
    {
        while (container.childCount > 0)
        {
            Destroy(container.GetChild(0).gameObject);
        }

        foreach (Lobby lobby in values)
        {
            LobbyRecord record = Instantiate(prefab, container);
            record.SetContent(lobby);
        }
    }
}
