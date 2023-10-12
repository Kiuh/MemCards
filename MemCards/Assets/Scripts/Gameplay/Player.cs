using Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    [SerializeField]
    private CinemachineVirtualCamera virtualCamera;

    public override void OnNetworkSpawn()
    {
        if (!IsLocalPlayer)
        {
            virtualCamera.gameObject.SetActive(false);
        }
        base.OnNetworkSpawn();
    }
}
