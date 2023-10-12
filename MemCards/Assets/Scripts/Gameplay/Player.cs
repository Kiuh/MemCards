using Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    [SerializeField]
    private CinemachineVirtualCamera virtualCamera;

    [SerializeField]
    private NetworkVariable<float> maxHealthPoints;
    public float MaxHealthPoints => maxHealthPoints.Value;
    private NetworkVariable<float> healthPoints =
        new(0, writePerm: NetworkVariableWritePermission.Owner);
    public float HealthPoints
    {
        get => healthPoints.Value;
        set => healthPoints.Value = value;
    }

    [SerializeField]
    private NetworkVariable<float> maxShieldPoints;
    public float MaxShieldPoints => maxShieldPoints.Value;
    private NetworkVariable<float> shieldPoints =
        new(0, writePerm: NetworkVariableWritePermission.Owner);

    public float ShieldPoints
    {
        get => shieldPoints.Value;
        set => shieldPoints.Value = value;
    }

    [SerializeField]
    private NetworkVariable<float> shieldDecreasing;
    private bool lockInteraction = true;

    public void SetLockInteraction(bool value)
    {
        lockInteraction = value;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsLocalPlayer)
        {
            virtualCamera.gameObject.SetActive(false);
        }
        else
        {
            PreparePlayerToGame();
            FindObjectOfType<PlayerView>().Player = this;
        }
        base.OnNetworkSpawn();
    }

    public void PreparePlayerToGame()
    {
        healthPoints.Value = MaxHealthPoints;
        shieldPoints.Value = 0;
    }

    public void Suicide()
    {
        healthPoints.Value = 0;
    }

    private void Update()
    {
        if (IsLocalPlayer)
        {
            if (ShieldPoints > 0)
            {
                ShieldPoints -= Time.deltaTime * shieldDecreasing.Value;
            }
            else
            {
                ShieldPoints = 0;
            }
            if (!lockInteraction)
            {
                if (healthPoints.Value <= 0)
                {
                    lockInteraction = true;
                    GameController.Singleton.PlayerLoseServerRpc(IsHost ? "Player1" : "Player2");
                }
            }
        }
    }
}
