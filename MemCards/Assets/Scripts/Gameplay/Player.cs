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

    public void ResetCamera()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.lockState = CursorLockMode.None;
        CinemachinePOV pov = virtualCamera.GetCinemachineComponent<CinemachinePOV>();
        pov.m_HorizontalAxis.Value = 0f;
        pov.m_VerticalAxis.Value = 0f;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsLocalPlayer)
        {
            virtualCamera.gameObject.SetActive(false);
        }
        else
        {
            healthPoints.Value = MaxHealthPoints;
            shieldPoints.Value = 0;
            FindObjectOfType<PlayerView>().Player = this;
        }
        base.OnNetworkSpawn();
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
        }
    }
}
