using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class GameController : NetworkBehaviour
{
    public static GameController Singleton;

    private void Awake()
    {
        Singleton = this;
    }

    [SerializeField]
    private Player playerPrefab;

    public override void OnNetworkSpawn()
    {
        SpawnPlayerServerRpc();
        base.OnNetworkSpawn();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnPlayerServerRpc(ServerRpcParams rpcParams = default)
    {
        PlayerSlot slot = FindObjectsOfType<PlayerSlot>().Where(x => !x.IsBusy).First();
        Player player = Instantiate(playerPrefab, slot.transform.position, slot.transform.rotation);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(rpcParams.Receive.SenderClientId);
        slot.IsBusy = true;
    }
}
